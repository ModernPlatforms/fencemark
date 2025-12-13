using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for calculating pricing and generating quotes
/// </summary>
public interface IPricingService
{
    /// <summary>
    /// Generate a quote for a job
    /// </summary>
    Task<Quote> GenerateQuoteAsync(string jobId, string? pricingConfigId = null, CancellationToken ct = default);

    /// <summary>
    /// Recalculate an existing quote
    /// </summary>
    Task<Quote> RecalculateQuoteAsync(string quoteId, string? changeSummary = null, CancellationToken ct = default);

    /// <summary>
    /// Calculate bill of materials for a job
    /// </summary>
    Task<List<BillOfMaterialsItem>> CalculateBillOfMaterialsAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Get height tier multiplier for a given height
    /// </summary>
    decimal GetHeightMultiplier(List<HeightTier> tiers, decimal heightInFeet);
}

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;

    public PricingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Quote> GenerateQuoteAsync(string jobId, string? pricingConfigId = null, CancellationToken ct = default)
    {
        // Get the job with all related data
        var job = await _context.Jobs
            .Include(j => j.LineItems)
                .ThenInclude(li => li.FenceType)
                    .ThenInclude(ft => ft!.Components)
                        .ThenInclude(fc => fc.Component)
            .Include(j => j.LineItems)
                .ThenInclude(li => li.GateType)
                    .ThenInclude(gt => gt!.Components)
                        .ThenInclude(gc => gc.Component)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null)
        {
            throw new InvalidOperationException($"Job with ID {jobId} not found");
        }

        // Get pricing configuration
        PricingConfig? pricingConfig;
        if (!string.IsNullOrEmpty(pricingConfigId))
        {
            pricingConfig = await _context.PricingConfigs
                .Include(pc => pc.HeightTiers)
                .FirstOrDefaultAsync(pc => pc.Id == pricingConfigId && pc.OrganizationId == job.OrganizationId, ct);
        }
        else
        {
            // Get default pricing config for organization
            pricingConfig = await _context.PricingConfigs
                .Include(pc => pc.HeightTiers)
                .FirstOrDefaultAsync(pc => pc.OrganizationId == job.OrganizationId && pc.IsDefault, ct);
        }

        if (pricingConfig == null)
        {
            throw new InvalidOperationException($"No pricing configuration found for organization {job.OrganizationId}");
        }

        // Calculate BOM
        var bomItems = await CalculateBillOfMaterialsForJobAsync(job, pricingConfig, ct);

        // Calculate costs
        var materialsCost = bomItems.Where(b => b.Category != "Labor").Sum(b => b.TotalPrice);
        var laborCost = CalculateLaborCost(job.TotalLinearFeet, pricingConfig);
        var subtotal = materialsCost + laborCost;
        var contingencyAmount = subtotal * pricingConfig.ContingencyPercentage;
        var profitAmount = (subtotal + contingencyAmount) * pricingConfig.ProfitMarginPercentage;
        var totalAmount = subtotal + contingencyAmount + profitAmount;

        // Generate quote number
        var quoteNumber = await GenerateQuoteNumberAsync(job.OrganizationId, ct);

        // Create quote
        var quote = new Quote
        {
            JobId = job.Id,
            OrganizationId = job.OrganizationId,
            PricingConfigId = pricingConfig.Id,
            QuoteNumber = quoteNumber,
            MaterialsCost = materialsCost,
            LaborCost = laborCost,
            Subtotal = subtotal,
            ContingencyAmount = contingencyAmount,
            ProfitAmount = profitAmount,
            TotalAmount = totalAmount,
            TaxAmount = 0, // Tax calculation can be added later
            GrandTotal = totalAmount,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = QuoteStatus.Draft
        };

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync(ct);

        // Add BOM items to quote
        foreach (var bomItem in bomItems)
        {
            bomItem.QuoteId = quote.Id;
            _context.BillOfMaterialsItems.Add(bomItem);
        }

        // Create initial version
        var version = CreateQuoteVersion(quote, bomItems, pricingConfig, "Initial quote");
        _context.QuoteVersions.Add(version);

        await _context.SaveChangesAsync(ct);

        // Reload quote with all relationships
        return await _context.Quotes
            .Include(q => q.BillOfMaterials)
            .Include(q => q.Versions)
            .Include(q => q.Job)
            .Include(q => q.PricingConfig)
            .FirstAsync(q => q.Id == quote.Id, ct);
    }

    public async Task<Quote> RecalculateQuoteAsync(string quoteId, string? changeSummary = null, CancellationToken ct = default)
    {
        var quote = await _context.Quotes
            .Include(q => q.Job)
                .ThenInclude(j => j!.LineItems)
                    .ThenInclude(li => li.FenceType)
                        .ThenInclude(ft => ft!.Components)
                            .ThenInclude(fc => fc.Component)
            .Include(q => q.Job)
                .ThenInclude(j => j!.LineItems)
                    .ThenInclude(li => li.GateType)
                        .ThenInclude(gt => gt!.Components)
                            .ThenInclude(gc => gc.Component)
            .Include(q => q.PricingConfig)
                .ThenInclude(pc => pc!.HeightTiers)
            .Include(q => q.BillOfMaterials)
            .FirstOrDefaultAsync(q => q.Id == quoteId, ct);

        if (quote == null)
        {
            throw new InvalidOperationException($"Quote with ID {quoteId} not found");
        }

        if (quote.Job == null || quote.PricingConfig == null)
        {
            throw new InvalidOperationException("Quote is missing required job or pricing configuration");
        }

        // Remove old BOM items
        _context.BillOfMaterialsItems.RemoveRange(quote.BillOfMaterials);

        // Recalculate BOM
        var bomItems = await CalculateBillOfMaterialsForJobAsync(quote.Job, quote.PricingConfig, ct);

        // Recalculate costs
        var materialsCost = bomItems.Where(b => b.Category != "Labor").Sum(b => b.TotalPrice);
        var laborCost = CalculateLaborCost(quote.Job.TotalLinearFeet, quote.PricingConfig);
        var subtotal = materialsCost + laborCost;
        var contingencyAmount = subtotal * quote.PricingConfig.ContingencyPercentage;
        var profitAmount = (subtotal + contingencyAmount) * quote.PricingConfig.ProfitMarginPercentage;
        var totalAmount = subtotal + contingencyAmount + profitAmount;

        // Update quote
        quote.MaterialsCost = materialsCost;
        quote.LaborCost = laborCost;
        quote.Subtotal = subtotal;
        quote.ContingencyAmount = contingencyAmount;
        quote.ProfitAmount = profitAmount;
        quote.TotalAmount = totalAmount;
        quote.GrandTotal = totalAmount + quote.TaxAmount;
        quote.UpdatedAt = DateTime.UtcNow;
        quote.CurrentVersion++;
        quote.Status = QuoteStatus.Revised;

        // Add new BOM items
        foreach (var bomItem in bomItems)
        {
            bomItem.QuoteId = quote.Id;
            _context.BillOfMaterialsItems.Add(bomItem);
        }

        // Create new version
        var version = CreateQuoteVersion(quote, bomItems, quote.PricingConfig, changeSummary ?? "Quote recalculated");
        _context.QuoteVersions.Add(version);

        await _context.SaveChangesAsync(ct);

        return quote;
    }

    public async Task<List<BillOfMaterialsItem>> CalculateBillOfMaterialsAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _context.Jobs
            .Include(j => j.LineItems)
                .ThenInclude(li => li.FenceType)
                    .ThenInclude(ft => ft!.Components)
                        .ThenInclude(fc => fc.Component)
            .Include(j => j.LineItems)
                .ThenInclude(li => li.GateType)
                    .ThenInclude(gt => gt!.Components)
                        .ThenInclude(gc => gc.Component)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job == null)
        {
            throw new InvalidOperationException($"Job with ID {jobId} not found");
        }

        // Get default pricing config
        var pricingConfig = await _context.PricingConfigs
            .Include(pc => pc.HeightTiers)
            .FirstOrDefaultAsync(pc => pc.OrganizationId == job.OrganizationId && pc.IsDefault, ct);

        if (pricingConfig == null)
        {
            throw new InvalidOperationException($"No default pricing configuration found for organization {job.OrganizationId}");
        }

        return await CalculateBillOfMaterialsForJobAsync(job, pricingConfig, ct);
    }

    private async Task<List<BillOfMaterialsItem>> CalculateBillOfMaterialsForJobAsync(
        Job job, 
        PricingConfig pricingConfig, 
        CancellationToken ct = default)
    {
        var bomItems = new List<BillOfMaterialsItem>();
        int sortOrder = 0;

        // Group components by category
        var componentsByCategory = new Dictionary<string, List<(Component component, decimal quantity)>>();

        // Process fence line items
        foreach (var lineItem in job.LineItems.Where(li => li.ItemType == LineItemType.Fence && li.FenceType != null))
        {
            var fenceType = lineItem.FenceType!;
            var linearFeet = lineItem.Quantity;
            var heightMultiplier = GetHeightMultiplier(pricingConfig.HeightTiers.ToList(), fenceType.HeightInFeet);

            foreach (var fenceComponent in fenceType.Components)
            {
                if (fenceComponent.Component == null) continue;

                var component = fenceComponent.Component;
                var quantity = fenceComponent.QuantityPerLinearFoot * linearFeet;
                var category = component.Category;

                if (!componentsByCategory.ContainsKey(category))
                {
                    componentsByCategory[category] = new List<(Component, decimal)>();
                }

                componentsByCategory[category].Add((component, quantity));
            }
        }

        // Process gate line items
        foreach (var lineItem in job.LineItems.Where(li => li.ItemType == LineItemType.Gate && li.GateType != null))
        {
            var gateType = lineItem.GateType!;
            var gateCount = lineItem.Quantity;

            foreach (var gateComponent in gateType.Components)
            {
                if (gateComponent.Component == null) continue;

                var component = gateComponent.Component;
                var quantity = gateComponent.QuantityPerGate * gateCount;
                var category = component.Category;

                if (!componentsByCategory.ContainsKey(category))
                {
                    componentsByCategory[category] = new List<(Component, decimal)>();
                }

                componentsByCategory[category].Add((component, quantity));
            }
        }

        // Consolidate components by category and create BOM items
        foreach (var category in componentsByCategory.Keys.OrderBy(k => k))
        {
            var componentsInCategory = componentsByCategory[category]
                .GroupBy(c => c.component.Id)
                .Select(g => new
                {
                    Component = g.First().component,
                    TotalQuantity = g.Sum(x => x.quantity)
                });

            foreach (var item in componentsInCategory.OrderBy(c => c.Component.Name))
            {
                var component = item.Component;
                var quantity = item.TotalQuantity;
                var unitPrice = component.UnitPrice;
                var totalPrice = quantity * unitPrice;

                bomItems.Add(new BillOfMaterialsItem
                {
                    QuoteId = string.Empty, // Will be set later
                    ComponentId = component.Id,
                    Category = category,
                    Description = component.Name,
                    Sku = component.Sku,
                    Quantity = quantity,
                    UnitOfMeasure = component.UnitOfMeasure,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    SortOrder = sortOrder++
                });
            }
        }

        // Add labor as a line item
        var laborCost = CalculateLaborCost(job.TotalLinearFeet, pricingConfig);
        if (laborCost > 0)
        {
            bomItems.Add(new BillOfMaterialsItem
            {
                QuoteId = string.Empty,
                Category = "Labor",
                Description = $"Installation Labor ({job.TotalLinearFeet:N2} linear feet)",
                Quantity = 1,
                UnitOfMeasure = "Job",
                UnitPrice = laborCost,
                TotalPrice = laborCost,
                SortOrder = sortOrder++
            });
        }

        return bomItems;
    }

    private decimal CalculateLaborCost(decimal totalLinearFeet, PricingConfig pricingConfig)
    {
        // Convert feet to meters for calculation (1 foot = 0.3048 meters)
        var totalLinearMeters = totalLinearFeet * 0.3048m;
        var totalHours = totalLinearMeters * pricingConfig.HoursPerLinearMeter;
        return totalHours * pricingConfig.LaborRatePerHour;
    }

    public decimal GetHeightMultiplier(List<HeightTier> tiers, decimal heightInFeet)
    {
        if (tiers == null || tiers.Count == 0)
        {
            return 1.0m;
        }

        // Convert feet to meters for comparison (1 foot = 0.3048 meters)
        var heightInMeters = heightInFeet * 0.3048m;

        var applicableTier = tiers
            .Where(t => heightInMeters >= t.MinHeightInMeters && 
                       (t.MaxHeightInMeters == null || heightInMeters <= t.MaxHeightInMeters))
            .OrderBy(t => t.MinHeightInMeters)
            .FirstOrDefault();

        return applicableTier?.Multiplier ?? 1.0m;
    }

    private QuoteVersion CreateQuoteVersion(Quote quote, List<BillOfMaterialsItem> bomItems, PricingConfig pricingConfig, string changeSummary)
    {
        // Create simple DTOs for serialization to avoid circular references
        var bomSnapshot = bomItems.Select(b => new
        {
            b.Category,
            b.Description,
            b.Sku,
            b.Quantity,
            b.UnitOfMeasure,
            b.UnitPrice,
            b.TotalPrice,
            b.SortOrder
        });

        var version = new QuoteVersion
        {
            QuoteId = quote.Id,
            VersionNumber = quote.CurrentVersion,
            ChangeSummary = changeSummary,
            MaterialsCost = quote.MaterialsCost,
            LaborCost = quote.LaborCost,
            Subtotal = quote.Subtotal,
            ContingencyAmount = quote.ContingencyAmount,
            ProfitAmount = quote.ProfitAmount,
            TotalAmount = quote.TotalAmount,
            TaxAmount = quote.TaxAmount,
            GrandTotal = quote.GrandTotal,
            BomSnapshot = JsonSerializer.Serialize(bomSnapshot),
            PricingConfigSnapshot = JsonSerializer.Serialize(new
            {
                pricingConfig.Name,
                pricingConfig.LaborRatePerHour,
                pricingConfig.HoursPerLinearMeter,
                pricingConfig.ContingencyPercentage,
                pricingConfig.ProfitMarginPercentage,
                HeightTiers = pricingConfig.HeightTiers.Select(ht => new
                {
                    ht.MinHeightInMeters,
                    ht.MaxHeightInMeters,
                    ht.Multiplier,
                    ht.Description
                })
            }),
            CreatedAt = DateTime.UtcNow
        };

        return version;
    }

    private async Task<string> GenerateQuoteNumberAsync(string organizationId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"Q-{today:yyyyMMdd}";
        
        var existingQuotesCount = await _context.Quotes
            .Where(q => q.OrganizationId == organizationId && q.QuoteNumber.StartsWith(prefix))
            .CountAsync(ct);

        return $"{prefix}-{existingQuotesCount + 1:D4}";
    }
}
