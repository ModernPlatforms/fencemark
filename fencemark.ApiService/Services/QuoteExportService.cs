using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for exporting quotes in various formats
/// </summary>
public interface IQuoteExportService
{
    /// <summary>
    /// Export quote as HTML
    /// </summary>
    Task<string> ExportQuoteAsHtmlAsync(string quoteId, CancellationToken ct = default);

    /// <summary>
    /// Export BOM as CSV
    /// </summary>
    Task<string> ExportBomAsCsvAsync(string quoteId, CancellationToken ct = default);

    /// <summary>
    /// Get quote data for export
    /// </summary>
    Task<QuoteExportData?> GetQuoteExportDataAsync(string quoteId, CancellationToken ct = default);
}

public class QuoteExportService : IQuoteExportService
{
    private readonly ApplicationDbContext _context;

    public QuoteExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> ExportQuoteAsHtmlAsync(string quoteId, CancellationToken ct = default)
    {
        var data = await GetQuoteExportDataAsync(quoteId, ct);
        if (data == null)
        {
            throw new InvalidOperationException($"Quote with ID {quoteId} not found");
        }

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <title>Quote " + data.QuoteNumber + "</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: Arial, sans-serif; max-width: 800px; margin: 40px auto; padding: 20px; }");
        html.AppendLine("        .header { border-bottom: 3px solid #333; padding-bottom: 20px; margin-bottom: 30px; }");
        html.AppendLine("        .header h1 { margin: 0; color: #333; }");
        html.AppendLine("        .info-section { margin-bottom: 30px; }");
        html.AppendLine("        .info-section h2 { color: #555; border-bottom: 1px solid #ddd; padding-bottom: 5px; }");
        html.AppendLine("        .info-grid { display: grid; grid-template-columns: 150px 1fr; gap: 10px; }");
        html.AppendLine("        .info-label { font-weight: bold; color: #666; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th { background-color: #f4f4f4; text-align: left; padding: 12px; border: 1px solid #ddd; }");
        html.AppendLine("        td { padding: 10px; border: 1px solid #ddd; }");
        html.AppendLine("        .category-header { background-color: #e8e8e8; font-weight: bold; }");
        html.AppendLine("        .totals { margin-top: 30px; float: right; width: 300px; }");
        html.AppendLine("        .total-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #eee; }");
        html.AppendLine("        .grand-total { font-size: 1.2em; font-weight: bold; border-top: 2px solid #333; padding-top: 10px; }");
        html.AppendLine("        .footer { margin-top: 50px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 0.9em; color: #666; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Header
        html.AppendLine("    <div class=\"header\">");
        html.AppendLine($"        <h1>{data.OrganizationName}</h1>");
        html.AppendLine($"        <h2>Quote #{data.QuoteNumber}</h2>");
        html.AppendLine("    </div>");

        // Quote Info
        html.AppendLine("    <div class=\"info-section\">");
        html.AppendLine("        <h2>Quote Information</h2>");
        html.AppendLine("        <div class=\"info-grid\">");
        html.AppendLine($"            <div class=\"info-label\">Quote Date:</div><div>{data.CreatedAt:MMMM dd, yyyy}</div>");
        html.AppendLine($"            <div class=\"info-label\">Valid Until:</div><div>{data.ValidUntil:MMMM dd, yyyy}</div>");
        html.AppendLine($"            <div class=\"info-label\">Status:</div><div>{data.Status}</div>");
        html.AppendLine($"            <div class=\"info-label\">Version:</div><div>{data.Version}</div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");

        // Customer Info
        html.AppendLine("    <div class=\"info-section\">");
        html.AppendLine("        <h2>Customer Information</h2>");
        html.AppendLine("        <div class=\"info-grid\">");
        html.AppendLine($"            <div class=\"info-label\">Name:</div><div>{data.CustomerName}</div>");
        if (!string.IsNullOrEmpty(data.CustomerEmail))
            html.AppendLine($"            <div class=\"info-label\">Email:</div><div>{data.CustomerEmail}</div>");
        if (!string.IsNullOrEmpty(data.CustomerPhone))
            html.AppendLine($"            <div class=\"info-label\">Phone:</div><div>{data.CustomerPhone}</div>");
        if (!string.IsNullOrEmpty(data.InstallationAddress))
            html.AppendLine($"            <div class=\"info-label\">Address:</div><div>{data.InstallationAddress}</div>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");

        // Bill of Materials
        html.AppendLine("    <div class=\"info-section\">");
        html.AppendLine("        <h2>Bill of Materials</h2>");
        html.AppendLine("        <table>");
        html.AppendLine("            <thead>");
        html.AppendLine("                <tr>");
        html.AppendLine("                    <th>Description</th>");
        html.AppendLine("                    <th>SKU</th>");
        html.AppendLine("                    <th style=\"text-align: right;\">Quantity</th>");
        html.AppendLine("                    <th style=\"text-align: right;\">Unit Price</th>");
        html.AppendLine("                    <th style=\"text-align: right;\">Total</th>");
        html.AppendLine("                </tr>");
        html.AppendLine("            </thead>");
        html.AppendLine("            <tbody>");

        var currentCategory = "";
        foreach (var item in data.BomItems.OrderBy(b => b.Category).ThenBy(b => b.SortOrder))
        {
            if (currentCategory != item.Category)
            {
                currentCategory = item.Category;
                html.AppendLine($"                <tr class=\"category-header\"><td colspan=\"5\">{currentCategory}</td></tr>");
            }

            html.AppendLine("                <tr>");
            html.AppendLine($"                    <td>{item.Description}</td>");
            html.AppendLine($"                    <td>{item.Sku ?? "-"}</td>");
            html.AppendLine($"                    <td style=\"text-align: right;\">{item.Quantity:N2} {item.UnitOfMeasure}</td>");
            html.AppendLine($"                    <td style=\"text-align: right;\">${item.UnitPrice:N2}</td>");
            html.AppendLine($"                    <td style=\"text-align: right;\">${item.TotalPrice:N2}</td>");
            html.AppendLine("                </tr>");
        }

        html.AppendLine("            </tbody>");
        html.AppendLine("        </table>");
        html.AppendLine("    </div>");

        // Totals
        html.AppendLine("    <div class=\"totals\">");
        html.AppendLine("        <div class=\"total-row\">");
        html.AppendLine($"            <span>Materials:</span><span>${data.MaterialsCost:N2}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"total-row\">");
        html.AppendLine($"            <span>Labor:</span><span>${data.LaborCost:N2}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"total-row\">");
        html.AppendLine($"            <span>Subtotal:</span><span>${data.Subtotal:N2}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"total-row\">");
        html.AppendLine($"            <span>Contingency:</span><span>${data.ContingencyAmount:N2}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("        <div class=\"total-row\">");
        html.AppendLine($"            <span>Profit:</span><span>${data.ProfitAmount:N2}</span>");
        html.AppendLine("        </div>");
        if (data.TaxAmount > 0)
        {
            html.AppendLine("        <div class=\"total-row\">");
            html.AppendLine($"            <span>Tax:</span><span>${data.TaxAmount:N2}</span>");
            html.AppendLine("        </div>");
        }
        html.AppendLine("        <div class=\"total-row grand-total\">");
        html.AppendLine($"            <span>Grand Total:</span><span>${data.GrandTotal:N2}</span>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        html.AppendLine("    <div style=\"clear: both;\"></div>");

        // Terms and Notes
        if (!string.IsNullOrEmpty(data.Terms) || !string.IsNullOrEmpty(data.Notes))
        {
            html.AppendLine("    <div class=\"info-section\">");
            if (!string.IsNullOrEmpty(data.Terms))
            {
                html.AppendLine("        <h2>Terms and Conditions</h2>");
                html.AppendLine($"        <p>{data.Terms.Replace("\n", "<br>")}</p>");
            }
            if (!string.IsNullOrEmpty(data.Notes))
            {
                html.AppendLine("        <h2>Notes</h2>");
                html.AppendLine($"        <p>{data.Notes.Replace("\n", "<br>")}</p>");
            }
            html.AppendLine("    </div>");
        }

        // Footer
        html.AppendLine("    <div class=\"footer\">");
        html.AppendLine($"        <p>This quote is valid until {data.ValidUntil:MMMM dd, yyyy}.</p>");
        html.AppendLine($"        <p>Generated on {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC</p>");
        html.AppendLine("    </div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> ExportBomAsCsvAsync(string quoteId, CancellationToken ct = default)
    {
        var data = await GetQuoteExportDataAsync(quoteId, ct);
        if (data == null)
        {
            throw new InvalidOperationException($"Quote with ID {quoteId} not found");
        }

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Category,Description,SKU,Quantity,Unit of Measure,Unit Price,Total Price");

        // Data rows
        foreach (var item in data.BomItems.OrderBy(b => b.Category).ThenBy(b => b.SortOrder))
        {
            csv.AppendLine($"{EscapeCsv(item.Category)},{EscapeCsv(item.Description)},{EscapeCsv(item.Sku ?? "")},{item.Quantity:F2},{EscapeCsv(item.UnitOfMeasure)},{item.UnitPrice:F2},{item.TotalPrice:F2}");
        }

        // Summary
        csv.AppendLine();
        csv.AppendLine($"Materials Cost,,,,,,{data.MaterialsCost:F2}");
        csv.AppendLine($"Labor Cost,,,,,,{data.LaborCost:F2}");
        csv.AppendLine($"Subtotal,,,,,,{data.Subtotal:F2}");
        csv.AppendLine($"Contingency,,,,,,{data.ContingencyAmount:F2}");
        csv.AppendLine($"Profit,,,,,,{data.ProfitAmount:F2}");
        if (data.TaxAmount > 0)
        {
            csv.AppendLine($"Tax,,,,,,{data.TaxAmount:F2}");
        }
        csv.AppendLine($"Grand Total,,,,,,{data.GrandTotal:F2}");

        return csv.ToString();
    }

    public async Task<QuoteExportData?> GetQuoteExportDataAsync(string quoteId, CancellationToken ct = default)
    {
        var quote = await _context.Quotes
            .Include(q => q.Job)
            .Include(q => q.Organization)
            .Include(q => q.BillOfMaterials)
            .FirstOrDefaultAsync(q => q.Id == quoteId, ct);

        if (quote == null)
        {
            return null;
        }

        return new QuoteExportData
        {
            QuoteId = quote.Id,
            QuoteNumber = quote.QuoteNumber,
            OrganizationName = quote.Organization?.Name ?? "Unknown",
            CustomerName = quote.Job?.CustomerName ?? "Unknown",
            CustomerEmail = quote.Job?.CustomerEmail,
            CustomerPhone = quote.Job?.CustomerPhone,
            InstallationAddress = quote.Job?.InstallationAddress,
            Status = quote.Status.ToString(),
            Version = quote.CurrentVersion,
            MaterialsCost = quote.MaterialsCost,
            LaborCost = quote.LaborCost,
            Subtotal = quote.Subtotal,
            ContingencyAmount = quote.ContingencyAmount,
            ProfitAmount = quote.ProfitAmount,
            TotalAmount = quote.TotalAmount,
            TaxAmount = quote.TaxAmount,
            GrandTotal = quote.GrandTotal,
            ValidUntil = quote.ValidUntil ?? DateTime.UtcNow.AddDays(30),
            CreatedAt = quote.CreatedAt,
            Terms = quote.Terms,
            Notes = quote.Notes,
            BomItems = quote.BillOfMaterials.ToList()
        };
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public class QuoteExportData
{
    public string QuoteId { get; set; } = string.Empty;
    public string QuoteNumber { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? InstallationAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ContingencyAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Terms { get; set; }
    public string? Notes { get; set; }
    public List<BillOfMaterialsItem> BomItems { get; set; } = new();
}
