# Pricing Catalog and Data Management

This document describes the core data schema and pricing catalog features implemented in Fencemark.

## Overview

Fencemark provides a comprehensive system for managing fence installation data, including:
- Property and land parcel tracking
- Blueprint and drawing management
- Flexible unit system support (Imperial/Metric)
- Tax region configuration
- Discount rules and promotional codes
- Complete pricing catalog

## Data Models

### Organization Settings

Organizations can now configure:
- **Unit System**: Choose between Imperial (feet/inches) or Metric (meters/centimeters)
- **Default Tax Region**: Set a default tax region for automatic tax calculation

```csharp
public class Organization
{
    public string Id { get; set; }
    public string Name { get; set; }
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Metric;  // Defaults to Metric
    public string? DefaultTaxRegionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum UnitSystem
{
    Imperial,  // Feet, inches (US/UK)
    Metric     // Meters, millimeters (Australia, most of world)
}
```

### Parcels (Properties)

Track property and land parcels where fencing will be installed:

```csharp
public class Parcel
{
    public string Id { get; set; }
    public string OrganizationId { get; set; }
    public string JobId { get; set; }
    public string Name { get; set; }  // e.g., "Front Yard", "Lot 5"
    public string? Address { get; set; }
    public string? ParcelNumber { get; set; }  // Assessor's parcel number
    public decimal? TotalArea { get; set; }
    public string? AreaUnit { get; set; }  // "sqft" or "sqm"
    public string? Coordinates { get; set; }  // GPS coordinates as JSON
    public string? Notes { get; set; }
    public ICollection<Drawing> Drawings { get; set; }
}
```

### Drawings

Manage blueprints, site plans, and other project documents:

```csharp
public class Drawing
{
    public string Id { get; set; }
    public string OrganizationId { get; set; }
    public string? JobId { get; set; }  // Optional - can be standalone
    public string? ParcelId { get; set; }  // Optional
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? DrawingType { get; set; }  // "Site Plan", "Blueprint", "Survey", "Photo"
    public string FileName { get; set; }
    public string FilePath { get; set; }  // Storage path or URL
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int Version { get; set; }
}
```

### Tax Regions

Configure tax rates for different jurisdictions:

```csharp
public class TaxRegion
{
    public string Id { get; set; }
    public string OrganizationId { get; set; }
    public string Name { get; set; }  // e.g., "California", "New South Wales"
    public string? Code { get; set; }  // e.g., "CA", "NSW"
    public decimal TaxRate { get; set; }  // e.g., 0.0875 for 8.75%
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}
```

### Discount Rules

Create flexible discount rules with various conditions:

```csharp
public class DiscountRule
{
    public string Id { get; set; }
    public string OrganizationId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }  // Percentage, FixedAmount, PerLinearFoot
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public decimal? MinimumLinearFeet { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; }
    public string? PromoCode { get; set; }
}

public enum DiscountType
{
    Percentage,      // e.g., 10% off
    FixedAmount,     // e.g., $100 off
    PerLinearFoot    // e.g., $0.50 off per foot
}
```

### Quote Enhancements

Quotes now support tax regions and discounts:

```csharp
public class Quote
{
    // ... existing properties ...
    public string? TaxRegionId { get; set; }
    public string? DiscountRuleId { get; set; }
    public decimal DiscountAmount { get; set; }
    public TaxRegion? TaxRegion { get; set; }
    public DiscountRule? DiscountRule { get; set; }
}
```

## API Endpoints

### Pricing Configurations

```http
GET    /api/pricing-configs              # List all pricing configs
GET    /api/pricing-configs/{id}         # Get specific config
POST   /api/pricing-configs              # Create new config
PUT    /api/pricing-configs/{id}         # Update config
DELETE /api/pricing-configs/{id}         # Delete config
```

### Tax Regions

```http
GET    /api/tax-regions                  # List all tax regions
GET    /api/tax-regions/{id}             # Get specific region
POST   /api/tax-regions                  # Create new region
PUT    /api/tax-regions/{id}             # Update region
DELETE /api/tax-regions/{id}             # Delete region
```

### Discount Rules

```http
GET    /api/discounts                    # List all discount rules
GET    /api/discounts/{id}               # Get specific discount
POST   /api/discounts                    # Create new discount
PUT    /api/discounts/{id}               # Update discount
DELETE /api/discounts/{id}               # Delete discount
POST   /api/discounts/validate-promo    # Validate a promo code
```

### Parcels

```http
GET    /api/parcels                      # List all parcels
GET    /api/parcels/by-job/{jobId}       # Get parcels for a job
GET    /api/parcels/{id}                 # Get specific parcel
POST   /api/parcels                      # Create new parcel
PUT    /api/parcels/{id}                 # Update parcel
DELETE /api/parcels/{id}                 # Delete parcel
```

### Drawings

```http
GET    /api/drawings                     # List all drawings
GET    /api/drawings/by-job/{jobId}      # Get drawings for a job
GET    /api/drawings/by-parcel/{parcelId}# Get drawings for a parcel
GET    /api/drawings/{id}                # Get specific drawing
POST   /api/drawings                     # Create new drawing
PUT    /api/drawings/{id}                # Update drawing metadata
DELETE /api/drawings/{id}                # Delete drawing
```

### Sample Data

```http
POST   /api/organizations/seed-sample-data  # Seed sample catalog data
```

## Sample Data

The seed data service provides a complete starter catalog with Australian/metric measurements:

### Components
- **Posts**: 150x150mm Treated Post ($65.00 each)
- **Rails**: 90x45mm Treated Rail ($12.50/linear metre)
- **Panels**: 1800mm Privacy Panel ($85.00 each)
- **Gate Hardware**: Heavy Duty Hinges ($35.00/pair), Gate Latch ($25.00 each)

### Fence Types
- **1800mm Privacy Fence**: Treated Pine, $115.00/linear metre
- **1200mm Picket Fence**: Timber, $95.00/linear metre
- **2100mm Privacy Fence**: Treated Pine, $145.00/linear metre

### Gate Types
- **Single Walk Gate**: 900mm x 1800mm, $385.00
- **Double Driveway Gate**: 3000mm x 1800mm, $1,250.00

### Pricing Configuration
- **Standard Pricing 2024**
  - Labor Rate: $85.00/hour
  - Hours per Linear Metre: 0.5
  - Contingency: 10%
  - Profit Margin: 20%
  - Height Tiers:
    - Up to 1800mm: 1.0x multiplier (standard)
    - 1800mm-2100mm: 1.25x multiplier (25% increase)
    - Over 2100mm: 1.5x multiplier (50% increase)

### Tax Regions
- **New South Wales**: 10% GST (default)
- **Victoria**: 10% GST
- **Queensland**: 10% GST

### Discount Rules
- **Volume Discount**: 10% off for orders over 150 linear metres
- **Early Bird Special**: $750 off for bookings 60+ days in advance (promo code: EARLY2024)

## Usage Examples

### Creating a Tax Region

```json
POST /api/tax-regions
{
  "name": "Western Australia",
  "code": "WA",
  "taxRate": 0.10,
  "description": "Australian GST",
  "isDefault": false
}
```

### Creating a Discount Rule

```json
POST /api/discounts
{
  "name": "Summer Special",
  "description": "15% off all fencing during summer months",
  "discountType": "Percentage",
  "discountValue": 0.15,
  "minimumOrderValue": 1000,
  "validFrom": "2024-06-01T00:00:00Z",
  "validUntil": "2024-08-31T23:59:59Z",
  "isActive": true,
  "promoCode": "SUMMER2024"
}
```

### Validating a Promo Code

```json
POST /api/discounts/validate-promo
{
  "promoCode": "SUMMER2024",
  "orderValue": 2500,
  "linearFeet": 150
}
```

Response (if valid):
```json
{
  "id": "...",
  "name": "Summer Special",
  "description": "15% off all fencing during summer months",
  "discountType": "Percentage",
  "discountValue": 0.15,
  ...
}
```

Response (if invalid):
```json
{
  "error": "Invalid promo code"
}
```

### Creating a Parcel

```json
POST /api/parcels
{
  "jobId": "job-123",
  "name": "Front Yard",
  "address": "123 Main St, Anytown, CA 12345",
  "parcelNumber": "APN-123-456-789",
  "totalArea": 5000,
  "areaUnit": "sqft",
  "coordinates": "{\"lat\": 37.7749, \"lng\": -122.4194}",
  "notes": "Level terrain, easy access"
}
```

### Creating a Drawing

```json
POST /api/drawings
{
  "jobId": "job-123",
  "parcelId": "parcel-456",
  "name": "Site Plan - Front Yard",
  "description": "Initial site survey showing property boundaries",
  "drawingType": "Site Plan",
  "fileName": "site-plan-front.pdf",
  "filePath": "/drawings/org-789/site-plan-front.pdf",
  "mimeType": "application/pdf",
  "fileSize": 2048576,
  "version": 1
}
```

### Seeding Sample Data

```http
POST /api/organizations/seed-sample-data
```

Response:
```json
{
  "success": true,
  "message": "Sample data seeded successfully"
}
```

## Unit System Support

Organizations can choose their preferred unit system:

- **Imperial** (default): Measurements in feet, inches
- **Metric**: Measurements in meters, centimeters

The unit system affects:
- Fence height specifications
- Linear measurements for pricing
- Area calculations for parcels
- Display in UI components

## Tax Calculation

Taxes are automatically calculated based on:
1. Quote's selected tax region
2. Organization's default tax region (if no region specified)
3. No tax if no region is configured

Formula:
```
Tax Amount = Total Amount × Tax Rate
Grand Total = Total Amount + Tax Amount
```

## Discount Application

Discounts can be applied:
1. Manually by selecting a discount rule
2. Automatically via promo code validation

Validation checks:
- Active status
- Valid date range
- Minimum order value
- Minimum linear footage
- Unique promo code

Calculation order:
```
Subtotal = Materials + Labor
Contingency = Subtotal × Contingency %
Profit = (Subtotal + Contingency) × Profit %
Total = Subtotal + Contingency + Profit
Discount = Calculate based on discount type
Discounted Total = Total - Discount
Tax = Discounted Total × Tax Rate
Grand Total = Discounted Total + Tax
```

## Security

All endpoints require authentication and enforce organization-level data isolation:
- Users can only access data for their organization
- Pricing configs, tax regions, and discounts are organization-specific
- Parcels and drawings are scoped to jobs within the organization
- All operations verify the user has access to the resources

## Future Enhancements

Potential improvements:
- [ ] Multi-currency support
- [ ] Tiered discount rules (buy more, save more)
- [ ] Time-based pricing (seasonal rates)
- [ ] Material vendor integration
- [ ] Real-time material pricing updates
- [ ] File upload/storage for drawings
- [ ] Drawing version comparison
- [ ] Parcel boundary mapping integration
- [ ] GPS location tracking for jobs

## Related Documentation

- [QUOTING_ENGINE.md](QUOTING_ENGINE.md) - Quote generation and export
- [IMPLEMENTATION.md](IMPLEMENTATION.md) - B2B User Onboarding
- [README.md](README.md) - General project information
