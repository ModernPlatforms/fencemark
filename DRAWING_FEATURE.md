# Drawing Feature Documentation

## Overview

The Drawing Feature enables users to visually plan fence installations using Azure Maps with satellite imagery. Users can draw fence segments on property boundaries, place gates, and get accurate measurements - all with support for onsite verification and corrections.

## Features

### 1. Interactive Map Drawing
- **Satellite Imagery**: View properties with high-resolution satellite imagery from Azure Maps
- **Draw Fence Segments**: Click to add points and draw fence lines on the map
- **Place Gates**: Click on fence segments to add gate positions
- **Snapping**: Fence lines automatically snap to lot boundaries when close
- **Measurements**: Real-time calculation of fence segment lengths in both feet and meters

### 2. Data Models

#### FenceSegment
Represents a drawn fence line on the map:
- GeoJSON geometry (LineString format)
- Calculated length in feet and meters
- Fence type selection
- Snapping to boundary indicator
- Onsite verification support
- Notes and corrections

#### GatePosition
Represents a gate placed on a fence segment:
- GeoJSON location (Point format)
- Position along segment (0.0 to 1.0)
- Gate type selection
- Onsite verification support
- Notes

### 3. API Endpoints

#### Fence Segments
- `GET /api/fence-segments` - Get all fence segments for organization
- `GET /api/fence-segments/by-job/{jobId}` - Get segments for a specific job
- `GET /api/fence-segments/{id}` - Get a specific segment
- `POST /api/fence-segments` - Create a new segment
- `PUT /api/fence-segments/{id}` - Update a segment
- `DELETE /api/fence-segments/{id}` - Delete a segment

#### Gate Positions
- `GET /api/gate-positions` - Get all gate positions for organization
- `GET /api/gate-positions/by-segment/{segmentId}` - Get gates for a segment
- `GET /api/gate-positions/{id}` - Get a specific gate
- `POST /api/gate-positions` - Create a new gate
- `PUT /api/gate-positions/{id}` - Update a gate
- `DELETE /api/gate-positions/{id}` - Delete a gate

### 4. User Interface

#### Drawing Page (`/jobs/{jobId}/drawing`)
- **Toolbar**: Tools for pan, draw, and gate placement
- **Sidebar**: List of drawn segments and placed gates
- **Layer Controls**: Toggle satellite/map view and boundary overlays
- **Save Functionality**: Persist drawings to the database

#### Drawing Tools
- **Pan**: Navigate the map (default)
- **Draw**: Click to add points, double-click to finish segment
- **Gate**: Click on a segment to place a gate

### 5. Integration Points

#### Jobs
- Each job can have multiple fence segments
- Navigate from Jobs page to Drawing page via "Open Drawing" button
- Segments are scoped to the job

#### Parcels
- Segments can optionally be associated with specific parcels
- Supports multi-parcel jobs

#### Fence Types
- Each segment can be assigned a fence type
- Used for pricing calculations

#### Gate Types
- Each gate position can be assigned a gate type
- Used for pricing calculations

## Configuration

### Azure Maps Setup

1. **Get Azure Maps Subscription Key**:
   - Create an Azure Maps account in the Azure Portal
   - Copy the subscription key

2. **Configure in `appsettings.json`**:
```json
{
  "AzureMaps": {
    "SubscriptionKey": "YOUR_AZURE_MAPS_KEY",
    "TilesetId": "microsoft.imagery",
    "DefaultCenter": {
      "Longitude": 133.7751,
      "Latitude": -25.2744
    },
    "DefaultZoom": 4
  }
}
```

3. **Update JavaScript**: Replace the placeholder in `wwwroot/js/azure-maps.js`:
```javascript
const subscriptionKey = 'YOUR_AZURE_MAPS_SUBSCRIPTION_KEY';
```

### Australian Lot Boundaries

The feature is designed to support Australian lot boundaries from:
- NSW Spatial Services
- VIC DataVic
- QLD Spatial Data
- Other state/territory cadastral services

To enable lot boundary overlays:
1. Obtain cadastral data access from relevant state authority
2. Convert boundary data to GeoJSON format
3. Load into Azure Maps Data Service or serve via API
4. Update `loadLotBoundaries()` function in `azure-maps.js`

## Database Schema

### FenceSegments Table
- `Id` (string, PK)
- `OrganizationId` (string, FK)
- `JobId` (string, FK)
- `ParcelId` (string, FK, nullable)
- `Name` (string, nullable)
- `FenceTypeId` (string, FK, nullable)
- `GeoJsonGeometry` (string, required) - LineString
- `LengthInFeet` (decimal)
- `LengthInMeters` (decimal)
- `IsSnappedToBoundary` (bool)
- `Notes` (string, nullable)
- `IsVerifiedOnsite` (bool)
- `OnsiteVerifiedLengthInFeet` (decimal, nullable)
- `CreatedAt` (datetime)
- `UpdatedAt` (datetime)

### GatePositions Table
- `Id` (string, PK)
- `OrganizationId` (string, FK)
- `FenceSegmentId` (string, FK)
- `GateTypeId` (string, FK, nullable)
- `Name` (string, nullable)
- `GeoJsonLocation` (string, required) - Point
- `PositionAlongSegment` (decimal) - 0.0 to 1.0
- `Notes` (string, nullable)
- `IsVerifiedOnsite` (bool)
- `CreatedAt` (datetime)
- `UpdatedAt` (datetime)

## Workflow

### 1. Planning Phase
1. Create a job with customer details
2. Click "Open Drawing" on the job
3. Search for the property address (or navigate manually)
4. Toggle satellite view for better visibility
5. Draw fence segments by clicking points on the boundary
6. Double-click to finish each segment
7. Click on segments to place gates
8. Save the drawing

### 2. Onsite Verification
1. Open the drawing on a mobile device
2. Review drawn segments against actual property
3. Mark segments as verified
4. Add corrections for any measurement discrepancies
5. Update notes with observations
6. Save changes

### 3. Quoting
1. Drawing measurements automatically feed into pricing
2. Gate types and fence types determine material costs
3. Segment lengths determine labor costs
4. Generate accurate quotes based on verified measurements

## Future Enhancements

### Planned Features
- [ ] Offline mode for onsite work without internet
- [ ] Photo attachment to segments and gates
- [ ] 3D terrain visualization for slope calculations
- [ ] AR (Augmented Reality) visualization on mobile
- [ ] Auto-detection of property boundaries using AI
- [ ] Multi-user collaborative drawing
- [ ] Version history for drawings
- [ ] Export to PDF or CAD formats

### Australia-Specific Enhancements
- [ ] Integration with state cadastral services APIs
- [ ] Automatic property lookup by address
- [ ] Council boundary and zoning overlays
- [ ] Native vegetation and environmental overlays
- [ ] Support for rural properties with large boundaries
- [ ] Integration with Australian building codes

## Technical Notes

### GeoJSON Format
Fence segments use LineString geometry:
```json
{
  "type": "LineString",
  "coordinates": [
    [longitude1, latitude1],
    [longitude2, latitude2],
    [longitude3, latitude3]
  ]
}
```

Gate positions use Point geometry:
```json
{
  "type": "Point",
  "coordinates": [longitude, latitude]
}
```

### Distance Calculations
- Uses Haversine formula for accurate distance on Earth's surface
- Converts between meters and feet (1 meter = 3.28084 feet)
- Accounts for Earth's curvature for large properties

### Security
- All API endpoints require authentication
- Data is scoped to organization via RLS (Row Level Security)
- Azure Maps subscription key should be stored in Azure Key Vault
- Lot boundary data access follows state government regulations

## Support

For issues or questions about the drawing feature:
1. Check the console for JavaScript errors
2. Verify Azure Maps subscription key is valid
3. Ensure proper authentication
4. Check browser compatibility (Chrome, Edge, Safari recommended)

## Resources

- [Azure Maps Documentation](https://docs.microsoft.com/en-us/azure/azure-maps/)
- [GeoJSON Specification](https://geojson.org/)
- [NSW Spatial Services](https://www.spatial.nsw.gov.au/)
- [VIC DataVic](https://datashare.maps.vic.gov.au/)
