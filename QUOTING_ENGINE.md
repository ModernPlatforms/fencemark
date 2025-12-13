# Quoting and Estimation Engine

This document describes the quoting and estimation engine implemented in Fencemark.

## Overview

The quoting engine provides comprehensive pricing calculation, bill of materials generation, quote versioning, and export functionality for fence installation projects.

## Features

### 1. Pricing Configuration

Organizations can define custom pricing configurations with:

- **Labor Rates**: Hourly rate and hours per linear foot
- **Contingency**: Percentage buffer for unexpected costs
- **Profit Margin**: Target profit percentage
- **Height Tiers**: Price multipliers based on fence height (e.g., taller fences cost more)

Each organization can have multiple pricing configurations, with one set as the default.

#### Example Height Tiers

| Min Height | Max Height | Multiplier | Description |
|------------|------------|------------|-------------|
| 0 ft | 6 ft | 1.0x | Standard height |
| 6 ft | 8 ft | 1.25x | Tall fence surcharge (25% increase) |
| 8 ft+ | - | 1.5x | Extra tall fence (50% increase) |

### 2. Quote Generation

The system automatically generates quotes from jobs by:

1. **Calculating Materials**: 
   - Analyzes fence types and gate types in the job
   - Determines required components based on linear footage
   - Aggregates components by category (Posts, Rails, Panels, Gates, etc.)

2. **Calculating Labor**:
   - Uses the formula: `Total Hours = Linear Feet × Hours per Linear Foot`
   - Applies hourly labor rate
   - Example: 100 ft × 0.15 hours/ft × $50/hour = $750

3. **Applying Pricing Rules**:
   - Subtotal = Materials + Labor
   - Contingency = Subtotal × Contingency %
   - Profit = (Subtotal + Contingency) × Profit %
   - Total = Subtotal + Contingency + Profit
   - Grand Total = Total + Tax (if applicable)

### 3. Bill of Materials (BOM)

Each quote includes a detailed BOM that lists:

- **Component Description**: Name of the material or service
- **SKU**: Product code (if available)
- **Quantity**: Amount needed
- **Unit of Measure**: Each, Linear Foot, Board Foot, etc.
- **Unit Price**: Cost per unit
- **Total Price**: Quantity × Unit Price
- **Category**: Posts, Rails, Panels, Hardware, Gates, Labor, etc.

Components are automatically calculated based on:
- **Fence Components**: Quantity per linear foot (e.g., 1 post per 8 feet = 0.125 per foot)
- **Gate Components**: Quantity per gate (e.g., 2 hinges per gate)

### 4. Quote Versioning

Quotes support versioning for "what-if" scenarios:

- **Version History**: Every change creates a new version
- **Change Summaries**: Track why each version was created
- **Snapshots**: Each version captures the complete BOM and pricing config
- **Comparison**: Compare costs between versions

This allows contractors to:
- Present multiple options to customers
- Track pricing changes over time
- Revert to previous quotes if needed

### 5. Export Formats

Quotes can be exported in multiple formats:

#### HTML Export (Branded Quote)
- Professional, printable format
- Includes organization branding
- Customer information
- Detailed line-item BOM
- All pricing breakdowns
- Terms and conditions
- Can be converted to PDF using browser print

#### CSV Export (BOM)
- Spreadsheet-compatible format
- All line items with quantities and pricing
- Summary totals
- Can be imported into Excel, Google Sheets, or accounting software

## API Endpoints

### Pricing Configuration

```http
GET    /api/pricing-configs              # List all pricing configs
GET    /api/pricing-configs/{id}         # Get specific config
POST   /api/pricing-configs              # Create new config
PUT    /api/pricing-configs/{id}         # Update config
DELETE /api/pricing-configs/{id}         # Delete config
```

### Quote Management

```http
POST   /api/quotes/generate              # Generate quote from job
POST   /api/quotes/{id}/recalculate      # Recalculate existing quote
GET    /api/quotes                       # List all quotes
GET    /api/quotes/{id}                  # Get quote with BOM and versions
PUT    /api/quotes/{id}                  # Update quote status, terms, etc.
DELETE /api/quotes/{id}                  # Delete quote
```

### Bill of Materials

```http
GET    /api/jobs/{jobId}/bom             # Calculate BOM for a job
```

### Export

```http
GET    /api/quotes/{id}/export/html      # Export as HTML
GET    /api/quotes/{id}/export/csv       # Export BOM as CSV
```

## Usage Examples

### Creating a Pricing Configuration

```json
POST /api/pricing-configs
{
  "name": "Standard Pricing 2024",
  "description": "Default pricing for residential projects",
  "laborRatePerHour": 50.00,
  "hoursPerLinearFoot": 0.15,
  "contingencyPercentage": 0.10,
  "profitMarginPercentage": 0.20,
  "isDefault": true
}
```

### Generating a Quote

```json
POST /api/quotes/generate
{
  "jobId": "job-12345",
  "pricingConfigId": "config-67890"  // Optional, uses default if not specified
}
```

Response includes:
- Quote number (e.g., `Q-20251213-0001`)
- All cost breakdowns
- Complete BOM
- Initial version record

### Recalculating a Quote

When a job changes (e.g., customer wants more linear feet):

```json
POST /api/quotes/{id}/recalculate
{
  "changeSummary": "Increased fence length from 100ft to 150ft"
}
```

This creates a new version and updates all calculations.

### Exporting a Quote

```http
GET /api/quotes/{id}/export/html
```

Returns a complete HTML document that can be:
- Displayed in a browser
- Printed to PDF
- Sent to customers via email

## Data Models

### PricingConfig

```csharp
{
  "id": "string",
  "organizationId": "string",
  "name": "string",
  "laborRatePerHour": "decimal",
  "hoursPerLinearFoot": "decimal",
  "contingencyPercentage": "decimal",
  "profitMarginPercentage": "decimal",
  "isDefault": "boolean",
  "heightTiers": [...]
}
```

### Quote

```csharp
{
  "id": "string",
  "jobId": "string",
  "quoteNumber": "string",
  "currentVersion": "int",
  "status": "Draft|Sent|Accepted|Rejected|Expired|Revised",
  "materialsCost": "decimal",
  "laborCost": "decimal",
  "subtotal": "decimal",
  "contingencyAmount": "decimal",
  "profitAmount": "decimal",
  "totalAmount": "decimal",
  "taxAmount": "decimal",
  "grandTotal": "decimal",
  "validUntil": "datetime",
  "billOfMaterials": [...],
  "versions": [...]
}
```

### BillOfMaterialsItem

```csharp
{
  "id": "string",
  "category": "string",
  "description": "string",
  "sku": "string",
  "quantity": "decimal",
  "unitOfMeasure": "string",
  "unitPrice": "decimal",
  "totalPrice": "decimal",
  "sortOrder": "int"
}
```

## Testing

The implementation includes comprehensive unit tests:

### PricingServiceTests (9 tests)
- Quote generation with valid job
- Labor cost calculations
- Contingency and profit margin application
- BOM generation with all components
- Height tier multipliers
- Quote versioning
- Unique quote number generation
- Error handling

### QuoteExportServiceTests (13 tests)
- HTML export validation
- CSV export validation
- All BOM items included
- All totals included
- Terms and conditions
- Special character handling
- Data retrieval
- Error handling

Run tests:
```bash
dotnet test --filter-class "*PricingServiceTests"
dotnet test --filter-class "*QuoteExportServiceTests"
```

## Calculation Examples

### Example 1: Simple Fence Project

**Job Details:**
- 100 linear feet of 6ft privacy fence
- 1 single walk gate

**Pricing Config:**
- Labor: $50/hour, 0.15 hours per linear foot
- Contingency: 10%
- Profit: 20%

**Materials:**
- Posts: 13 @ $45 = $585
- Rails: 300 LF @ $2.50 = $750
- Panels: 13 @ $12.69 = $165
- Gate Hinges: 2 @ $12.50 = $25
- **Materials Total: $1,525**

**Labor:**
- Hours: 100 ft × 0.15 = 15 hours
- Cost: 15 × $50 = $750

**Pricing:**
- Subtotal: $1,525 + $750 = $2,275
- Contingency (10%): $227.50
- Subtotal with Contingency: $2,502.50
- Profit (20%): $500.50
- **Total: $3,003.00**

### Example 2: With Height Tier

Same project but with 8ft tall fence:

**Height Tier:** 8ft falls into "Tall Fence" tier with 1.25x multiplier

**Adjusted Materials:** $1,525 × 1.25 = $1,906.25
**Labor:** $750 (same)
**Subtotal:** $2,656.25
**Contingency:** $265.63
**Profit:** $584.38
**Total: $3,506.25**

## Security

All endpoints require authentication and enforce organization-level data isolation:
- Users can only access quotes for their organization
- Pricing configs are organization-specific
- All operations verify the user has access to the resources

## Future Enhancements

Potential improvements:
- [ ] PDF generation (currently HTML can be printed to PDF)
- [ ] Excel export with formatted spreadsheets
- [ ] Email delivery of quotes
- [ ] E-signature integration hooks
- [ ] Quote templates with custom branding
- [ ] Multi-currency support
- [ ] Discount and promotion codes
- [ ] Payment schedule templates
- [ ] Material vendor integration
- [ ] Real-time material pricing updates

## Related Documentation

- [IMPLEMENTATION.md](IMPLEMENTATION.md) - B2B User Onboarding
- [README.md](README.md) - General project information
- [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment guide
