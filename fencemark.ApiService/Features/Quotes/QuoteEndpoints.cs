using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Quotes;

public static class QuoteEndpoints
{
    public static IEndpointRouteBuilder MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes")
            .WithTags("Quotes")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateQuote)
            .WithName("GenerateQuote");

        group.MapPost("/{id}/recalculate", RecalculateQuote)
            .WithName("RecalculateQuote");

        group.MapGet("/", GetAllQuotes)
            .WithName("GetQuotes");

        group.MapGet("/{id}", GetQuoteById)
            .WithName("GetQuoteById");

        group.MapPut("/{id}", UpdateQuote)
            .WithName("UpdateQuote");

        group.MapDelete("/{id}", DeleteQuote)
            .WithName("DeleteQuote");

        group.MapGet("/{id}/export/html", ExportQuoteAsHtml)
            .WithName("ExportQuoteAsHtml")
            .WithTags("Export");

        group.MapGet("/{id}/export/csv", ExportBomAsCsv)
            .WithName("ExportBomAsCsv")
            .WithTags("Export");

        // BOM endpoint
        app.MapGet("/api/jobs/{jobId}/bom", GetJobBillOfMaterials)
            .RequireAuthorization()
            .WithName("GetJobBillOfMaterials")
            .WithTags("BOM");

        return app;
    }

    private static async Task<IResult> GenerateQuote(
        GenerateQuoteRequest request,
        IPricingService pricingService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        try
        {
            var quote = await pricingService.GenerateQuoteAsync(request.JobId, request.PricingConfigId, ct);
            return Results.Ok(quote);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RecalculateQuote(
        string id,
        RecalculateQuoteRequest request,
        IPricingService pricingService,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify quote belongs to user's organization
        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        if (quote == null)
            return Results.NotFound();

        try
        {
            var updatedQuote = await pricingService.RecalculateQuoteAsync(id, request.ChangeSummary, ct);
            return Results.Ok(updatedQuote);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetAllQuotes(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var quotes = await db.Quotes
            .Include(q => q.Job)
            .Where(q => q.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(quotes);
    }

    private static async Task<IResult> GetQuoteById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var quote = await db.Quotes
            .Include(q => q.Job)
            .Include(q => q.BillOfMaterials)
            .Include(q => q.Versions)
            .Include(q => q.PricingConfig)
            .FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        
        return quote != null ? Results.Ok(quote) : Results.NotFound();
    }

    private static async Task<IResult> UpdateQuote(
        string id,
        UpdateQuoteRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        if (quote == null)
            return Results.NotFound();

        quote.Status = request.Status;
        quote.ValidUntil = request.ValidUntil;
        quote.Terms = request.Terms;
        quote.Notes = request.Notes;
        quote.TaxAmount = request.TaxAmount;
        quote.GrandTotal = quote.TotalAmount + request.TaxAmount;
        quote.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(quote);
    }

    private static async Task<IResult> DeleteQuote(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        if (quote == null)
            return Results.NotFound();

        db.Quotes.Remove(quote);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetJobBillOfMaterials(
        string jobId,
        IPricingService pricingService,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify job belongs to user's organization
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == currentUser.OrganizationId, ct);
        if (job == null)
            return Results.NotFound();

        try
        {
            var bom = await pricingService.CalculateBillOfMaterialsAsync(jobId, ct);
            return Results.Ok(bom);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ExportQuoteAsHtml(
        string id,
        IQuoteExportService exportService,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify quote belongs to user's organization
        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        if (quote == null)
            return Results.NotFound();

        try
        {
            var html = await exportService.ExportQuoteAsHtmlAsync(id, ct);
            return Results.Content(html, "text/html");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ExportBomAsCsv(
        string id,
        IQuoteExportService exportService,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify quote belongs to user's organization
        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id && q.OrganizationId == currentUser.OrganizationId, ct);
        if (quote == null)
            return Results.NotFound();

        try
        {
            var csv = await exportService.ExportBomAsCsvAsync(id, ct);
            return Results.Text(csv, "text/csv");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

// Request/Response DTOs
record GenerateQuoteRequest(string JobId, string? PricingConfigId = null);
record RecalculateQuoteRequest(string? ChangeSummary = null);
record UpdateQuoteRequest(QuoteStatus Status, DateTime? ValidUntil, string? Terms, string? Notes, decimal TaxAmount);
