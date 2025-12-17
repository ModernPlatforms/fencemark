// Azure Maps Drawing Integration for Fencemark
// This provides the client-side map drawing functionality with Azure Maps

let map = null;
let datasource = null;
let boundaryDatasource = null;
let currentJobId = null;
let currentTool = 'pan';
let drawnSegments = [];
let placedGates = [];
let currentDrawing = [];

// Initialize Azure Maps
window.initializeAzureMap = function (jobId) {
    currentJobId = jobId;
    
    // IMPORTANT: Replace this placeholder with your actual Azure Maps subscription key
    // Get your key from: https://portal.azure.com -> Azure Maps -> Authentication
    // For production, load this from server-side configuration via an API endpoint
    // to prevent exposing the key in client-side code
    const subscriptionKey = 'YOUR_AZURE_MAPS_SUBSCRIPTION_KEY';

    try {
        // Initialize the map centered on Australia
        map = new atlas.Map('map', {
            center: [133.7751, -25.2744],
            zoom: 4,
            language: 'en-US',
            authOptions: {
                authType: 'subscriptionKey',
                subscriptionKey: subscriptionKey
            },
            style: 'satellite_road_labels'
        });

        map.events.add('ready', onMapReady);
    } catch (error) {
        console.error('Error initializing Azure Maps:', error);
    }
};

function onMapReady() {
    // Create data sources
    datasource = new atlas.source.DataSource();
    map.sources.add(datasource);

    boundaryDatasource = new atlas.source.DataSource();
    map.sources.add(boundaryDatasource);

    // Add rendering layers
    addMapLayers();

    // Setup drawing interaction
    setupDrawingInteraction();

    console.log('Azure Maps ready for job:', currentJobId);
}

function addMapLayers() {
    // Boundary layer
    map.layers.add(new atlas.layer.LineLayer(boundaryDatasource, null, {
        strokeColor: '#FFA500',
        strokeWidth: 2,
        strokeDashArray: [5, 5]
    }));

    // Fence segment layer
    map.layers.add(new atlas.layer.LineLayer(datasource, null, {
        strokeColor: '#0066CC',
        strokeWidth: 3,
        filter: ['==', ['get', 'type'], 'fence']
    }));

    // Gate marker layer
    map.layers.add(new atlas.layer.SymbolLayer(datasource, null, {
        iconOptions: {
            image: 'marker-blue',
            size: 1.2
        },
        textOptions: {
            textField: ['get', 'name'],
            offset: [0, 1.5]
        },
        filter: ['==', ['get', 'type'], 'gate']
    }));
}

function setupDrawingInteraction() {
    map.events.add('click', function (e) {
        if (currentTool === 'draw') {
            currentDrawing.push(e.position);
            // Visual feedback for current drawing
            updateDrawingPreview();
        } else if (currentTool === 'gate') {
            placeGate(e.position);
        }
    });

    map.events.add('dblclick', function () {
        if (currentTool === 'draw' && currentDrawing.length > 1) {
            finishFenceSegment();
        }
    });
}

function updateDrawingPreview() {
    // Show temporary preview of current drawing
    if (currentDrawing.length > 1) {
        const previewLine = {
            type: 'Feature',
            geometry: {
                type: 'LineString',
                coordinates: currentDrawing
            },
            properties: {
                type: 'preview'
            }
        };
        // Add visual preview (simplified)
    }
}

function finishFenceSegment() {
    const lengthInMeters = calculateLineLength(currentDrawing);
    const lengthInFeet = lengthInMeters * 3.28084;
    
    const segment = {
        type: 'Feature',
        geometry: {
            type: 'LineString',
            coordinates: [...currentDrawing]
        },
        properties: {
            type: 'fence',
            id: generateId(),
            lengthInFeet: lengthInFeet.toFixed(2),
            lengthInMeters: lengthInMeters.toFixed(2)
        }
    };
    
    datasource.add(segment);
    drawnSegments.push(segment);
    currentDrawing = [];
    
    console.log('Fence segment created:', segment.properties);
}

function placeGate(position) {
    const gate = {
        type: 'Feature',
        geometry: {
            type: 'Point',
            coordinates: position
        },
        properties: {
            type: 'gate',
            id: generateId(),
            name: 'Gate'
        }
    };
    
    datasource.add(gate);
    placedGates.push(gate);
    console.log('Gate placed:', gate.properties);
}

function calculateLineLength(coordinates) {
    let total = 0;
    for (let i = 0; i < coordinates.length - 1; i++) {
        total += calculateDistance(coordinates[i], coordinates[i + 1]);
    }
    return total;
}

function calculateDistance(coord1, coord2) {
    const R = 6371000;
    const lat1 = coord1[1] * Math.PI / 180;
    const lat2 = coord2[1] * Math.PI / 180;
    const deltaLat = (coord2[1] - coord1[1]) * Math.PI / 180;
    const deltaLon = (coord2[0] - coord1[0]) * Math.PI / 180;

    const a = Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
              Math.cos(lat1) * Math.cos(lat2) *
              Math.sin(deltaLon / 2) * Math.sin(deltaLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return R * c;
}

function generateId() {
    return 'id_' + Date.now() + '_' + Math.random().toString(36).substring(2, 11);
}

// Public API functions
window.setDrawingTool = function (tool) {
    currentTool = tool;
    currentDrawing = [];
};

window.toggleMapLayer = function (layerType, visible) {
    if (layerType === 'satellite') {
        map.setStyle({ style: visible ? 'satellite_road_labels' : 'road' });
    }
};

window.removeSegmentFromMap = function (segmentId) {
    const segmentToRemove = drawnSegments.find(s => s.properties.id === segmentId);
    if (segmentToRemove && datasource) {
        datasource.remove(segmentToRemove);
    }
    drawnSegments = drawnSegments.filter(s => s.properties.id !== segmentId);
};

window.removeGateFromMap = function (gateId) {
    const gateToRemove = placedGates.find(g => g.properties.id === gateId);
    if (gateToRemove && datasource) {
        datasource.remove(gateToRemove);
    }
    placedGates = placedGates.filter(g => g.properties.id !== gateId);
};

window.getMapData = function () {
    return JSON.stringify({
        segments: drawnSegments,
        gates: placedGates,
        jobId: currentJobId
    });
};

console.log('Azure Maps drawing module loaded');
