// Azure Maps Drawing Integration for Fencemark
// This provides the client-side map drawing functionality with Azure Maps
// Compatible with Azure Maps Web SDK v3.x
// Uses Azure AD authentication (no subscription key exposure)

let map = null;
let datasource = null;
let boundaryDatasource = null;
let currentJobId = null;
let currentTool = 'pan';
let drawnSegments = [];
let placedGates = [];
let currentDrawing = [];
let tokenProvider = null;
let isSubscriptionKeyMode = false;

// Initialize Azure Maps with Azure AD authentication or subscription key
// jobId: The job ID for this drawing session
// clientId: The Azure Maps account client ID (from Azure Portal)
// dotNetRef: A .NET object reference that provides getAzureMapsToken method
// tokenInfo: Optional initial token info from server (includes useSubscriptionKey flag)
window.initializeAzureMap = function (jobId, clientId, dotNetRef, tokenInfo) {
    currentJobId = jobId;
    tokenProvider = dotNetRef;

    // Check if we should use subscription key mode (for local development)
    if (tokenInfo && tokenInfo.useSubscriptionKey) {
        isSubscriptionKeyMode = true;
        console.log('Azure Maps: Using subscription key authentication (local development)');
    }

    // Validate client ID is provided (except for subscription key mode)
    if (!clientId && !isSubscriptionKeyMode) {
        console.error('Azure Maps client ID not configured. Please add AzureMaps:ClientId to appsettings.json');
        return;
    }

    if (!dotNetRef && !isSubscriptionKeyMode) {
        console.error('Token provider not provided. Azure AD authentication requires a token provider.');
        return;
    }

    try {
        // Build auth options based on mode
        let authOptions;
        if (isSubscriptionKeyMode && tokenInfo) {
            authOptions = {
                authType: 'subscriptionKey',
                subscriptionKey: tokenInfo.token
            };
        } else {
            authOptions = {
                authType: 'anonymous',
                clientId: clientId,
                getToken: async function (resolve, reject) {
                    try {
                        // Call back to Blazor to get the Azure Maps access token
                        const token = await tokenProvider.invokeMethodAsync('GetAzureMapsTokenAsync');
                        if (token) {
                            resolve(token);
                        } else {
                            reject(new Error('Failed to acquire Azure Maps token'));
                        }
                    } catch (error) {
                        console.error('Error acquiring Azure Maps token:', error);
                        reject(error);
                    }
                }
            };
        }

        // Initialize the map centered on Australia
        map = new atlas.Map('map', {
            center: [133.7751, -25.2744],
            zoom: 4,
            language: 'en-US',
            authOptions: authOptions,
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
    const lengthInMetres = calculateLineLength(currentDrawing);

    const segment = {
        type: 'Feature',
        geometry: {
            type: 'LineString',
            coordinates: [...currentDrawing]
        },
        properties: {
            type: 'fence',
            id: generateId(),
            lengthInMetres: lengthInMetres.toFixed(2)
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

// Load cadastral boundary from GeoJSON data
window.loadCadastralBoundary = function (geoJson) {
    if (!boundaryDatasource) {
        console.error('Boundary datasource not initialized');
        return false;
    }

    try {
        // Clear existing boundaries
        boundaryDatasource.clear();

        if (geoJson) {
            const feature = typeof geoJson === 'string' ? JSON.parse(geoJson) : geoJson;
            boundaryDatasource.add(feature);
            console.log('Cadastral boundary loaded');
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error loading cadastral boundary:', error);
        return false;
    }
};

// Clear cadastral boundary from map
window.clearCadastralBoundary = function () {
    if (boundaryDatasource) {
        boundaryDatasource.clear();
        console.log('Cadastral boundary cleared');
    }
};

// Toggle boundary layer visibility
window.toggleBoundaryLayer = function (visible) {
    const layers = map.layers.getLayers();
    layers.forEach(layer => {
        if (layer.getSource() === boundaryDatasource) {
            layer.setOptions({ visible: visible });
        }
    });
};

// Center map on coordinates and optionally zoom
window.centerMapOnCoordinates = function (lng, lat, zoom) {
    if (map) {
        const options = { center: [lng, lat] };
        if (zoom !== null && zoom !== undefined) {
            options.zoom = zoom;
        }
        map.setCamera(options);
    }
};

// Get current map center coordinates
window.getMapCenter = function () {
    if (map) {
        const center = map.getCamera().center;
        return JSON.stringify({ lng: center[0], lat: center[1] });
    }
    return null;
};

console.log('Azure Maps drawing module loaded (SDK v3 compatible)');
