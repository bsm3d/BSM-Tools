BSM Tools Complete Guide
Version 1.0 for Unity 2023+ HDRP
Created by Benoît Saint-Moulin

INTRODUCTION
===========
The BSM Tools suite provides a comprehensive set of utilities designed to enhance your Unity development workflow. Each tool is crafted to solve specific challenges while maintaining high performance and ease of use.

ALIGNMENT SYSTEM (BSMAlign)
==========================
BSMAlign specializes in precise object positioning and rotation, particularly useful for level design and runtime object placement.

Core Features:
- Surface alignment with normal detection
- Multiple object alignment
- Random rotation with constraints
- Advanced axis locking system

Let's explore how to use these features effectively:

Basic Surface Alignment:
```csharp
// The simplest form - align an object to the surface below it
var result = BSMAlign.AlignToSurface(transform);

// Check if alignment was successful
if (result.success) {
    Debug.Log($"Object aligned at angle: {result.angle}");
}
```

Advanced Alignment with Custom Settings:
```csharp
// Create settings to control alignment behavior
var settings = new BSMAlign.AlignSettings {
    preserveScale = true,         // Keep the object's original scale
    preservePosition = false,     // Allow position adjustments
    lockedAxes = Vector3.up,     // Lock Y-axis rotation
    maxAngle = 45f,              // Maximum alignment angle
    blendWeight = 0.8f,          // Smooth transition blend
    raycastMask = LayerMask.GetMask("Ground", "Platforms")
};

// Align to a specific normal
var result = BSMAlign.AlignToNormal(transform, surfaceNormal, settings);
```

ATMOSPHERIC EFFECTS (BSM_Atmosphere)
=================================
BSM_Atmosphere creates dynamic sky and atmospheric effects, perfect for creating immersive environments.

Core Features:
- Dynamic sky gradient generation
- Time-based atmospheric haze
- Customizable color schemes
- Editor window visualization

Implementation Examples:

Basic Sky Setup:
```csharp
// Initialize the atmosphere system
var atmosphere = new BSM_Atmosphere();

// Draw basic sky gradient in your editor window
void OnGUI() {
    atmosphere.DrawSkyGradient(position);
}
```

Dynamic Atmospheric Effects:
```csharp
// Add atmospheric haze that changes over time
void Update() {
    float currentTime = Time.time;
    atmosphere.DrawAtmosphericHaze(windowRect, currentTime);
}
```

DEBUG VISUALIZATION (BSMDebug)
===========================
BSMDebug provides powerful visual debugging tools essential for development and testing.

Core Features:
- Point and vector visualization
- Surface contact display
- Bounds and zone rendering
- Customizable debug settings

Usage Examples:

Basic Debug Visualization:
```csharp
// Draw a simple debug point
BSMDebug.DrawPoint(transform.position, "Object Position");

// Visualize a direction vector
BSMDebug.DrawVector(transform.position, transform.forward * 2f, "Forward");
```

Advanced Debug Visualization:
```csharp
// Create custom debug settings
var settings = new BSMDebug.DebugSettings {
    duration = 5f,               // How long to display
    persistent = false,          // Temporary display
    primaryColor = Color.blue,   // Main visualization color
    secondaryColor = Color.yellow,
    size = 0.2f,                // Visual element size
    showLabels = true,          // Display text labels
    labelOffset = 0.2f          // Label positioning
};

// Display complex debug information
BSMDebug.DrawNormal(hitPoint, surfaceNormal, "Surface Normal", settings);
BSMDebug.DrawSnapZone(transform.position, 2f, "Snap Range", settings);
```

MATHEMATICAL UTILITIES (BSMMath)
=============================
BSMMath provides a comprehensive set of mathematical functions for various calculations and transformations.

Core Features:
- Unit conversions
- Geometric calculations
- Interpolation functions
- Statistical utilities

Common Applications:

Basic Calculations:
```csharp
// Convert units
float radians = BSMMath.DegreesToRadians(45f);
float kmh = BSMMath.MetersPerSecondToKilometersPerHour(10f);

// Calculate areas and volumes
float triangleArea = BSMMath.CalculateTriangleArea(2f, 3f);
float sphereVolume = BSMMath.CalculateSphereVolume(1f);
```

Advanced Mathematics:
```csharp
// Advanced interpolation
float progress = BSMMath.CalculateProgress(currentValue, minValue, maxValue);
float mapped = BSMMath.Map(value, 0f, 1f, -1f, 1f);

// Easing functions for smooth animations
float easedValue = BSMMath.EaseInOutQuad(time);

// Statistical calculations
float gaussianValue = BSMMath.Gaussian(0f, 1f);  // Mean and standard deviation
```

PHYSICS SIMULATION (BSMPhysics)
============================
BSMPhysics provides advanced physics simulation capabilities for precise object placement and movement.

Core Features:
- Physics-based positioning
- Surface validation
- Kinematic movement simulation
- Detailed collision analysis

Implementation Examples:

Basic Physics Simulation:
```csharp
// Simple physics simulation
var result = BSMPhysics.SimulatePhysics(gameObject);
if (result.success) {
    Debug.Log($"Simulation completed in {result.timeUsed} seconds");
}
```

Advanced Physics Configuration:
```csharp
// Configure detailed physics settings
var settings = new BSMPhysics.PhysicsSettings {
    maxStepSize = 0.05f,        // Simulation precision
    maxIterations = 200,        // Iteration limit
    useGravity = true,
    collisionMode = CollisionDetectionMode.Continuous,
    
    // Surface validation settings
    surfaceSettings = new BSMRaycasting.SurfaceSettings {
        maxSlopeAngle = 30f,
        minArea = 0.05f,
        requireMeshCollider = true
    }
};

// Run detailed simulation
var result = BSMPhysics.SimulatePhysics(gameObject, settings);
```

RAYCASTING SYSTEM (BSMRaycasting)
==============================
BSMRaycasting extends Unity's built-in raycasting with advanced features and surface validation.

Core Features:
- Enhanced raycast types (Sphere, Box, Capsule)
- Surface validation
- Detailed hit information
- Visual debugging

Usage Examples:

Basic Raycasting:
```csharp
// Simple raycast with hit detection
var result = BSMRaycasting.Cast(transform.position, Vector3.down);
if (result.hasHit) {
    Debug.Log($"Hit surface at {result.hitPoint} with normal {result.hitNormal}");
}
```

Advanced Surface Detection:
```csharp
// Configure detailed raycast settings
var raySettings = new BSMRaycasting.RaycastSettings {
    sphereCast = true,           // Use sphere cast
    sphereRadius = 0.5f,         // Sphere size
    layerMask = LayerMask.GetMask("Ground"),
    debugDraw = true            // Show visual debug
};

// Set up surface validation
var surfaceSettings = new BSMRaycasting.SurfaceSettings {
    minArea = 0.1f,             // Minimum surface size
    maxSlopeAngle = 30f,        // Maximum slope angle
    validTags = new[] { "Ground", "Platform" }
};

// Perform validated cast
var result = BSMRaycasting.ValidatedCast(
    transform.position, 
    Vector3.down,
    raySettings,
    surfaceSettings
);
```

SNAP SYSTEM (BSMSnap)
===================
BSMSnap provides precise object positioning capabilities, particularly useful for level design and object placement.

Core Features:
- Vertex snapping
- Grid alignment
- Surface snapping
- Bounds calculation

Basic Implementation:
```csharp
// Snap to nearest vertex
var result = BSMSnap.SnapToNearestVertex(transform.position, targetObject);
if (result.success) {
    transform.position = result.position;
}

// Snap to grid
var gridResult = BSMSnap.SnapToGrid(transform.position, 1f);
```

Advanced Snapping:
```csharp
// Configure detailed snap settings
var settings = new BSMSnap.SnapSettings {
    maxDistance = 5f,           // Maximum snap distance
    useNormal = true,          // Consider surface normals
    useBaseOffset = true,      // Use object's base for placement
    layerMask = LayerMask.GetMask("Ground"),
    tags = new[] { "Snappable" }
};

// Perform precise snap with validation
var result = BSMSnap.SnapToNearestVertex(transform.position, targetObject, settings);
```

TEXTURE GENERATION (BSM_Textures)
==============================
BSM_Textures provides comprehensive tools for generating and managing procedural textures.

Core Features:
- Multiple noise algorithms
- Normal map generation
- Texture set management
- High-quality export options

Basic Texture Generation:
```csharp
// Create a simple Perlin noise texture
var texture = BSM_Textures.CreatePerlinNoiseTexture(512, 512);

// Generate a mask texture
var mask = BSM_Textures.CreateMaskTexture(512, 512, threshold: 0.5f);
```

Advanced Texture Creation:
```csharp
// Generate complex procedural texture
var primaryTexture = BSM_Textures.CreateTexture(
    BSM_Textures.TextureType.FractionalBrownian,
    512, 512,
    octaves: 4,
    persistence: 0.5f,
    lacunarity: 2f
);

// Create supporting textures
var normalMap = BSM_Textures.CreateNormalMapFromTexture(primaryTexture);
var maskTexture = BSM_Textures.CreateMaskTexture(512, 512, 0.5f);

// Export complete texture set
var exportResult = BSM_Textures.ExportTextureSet(
    primaryTexture,
    normalMap,
    maskTexture,
    null,  // Optional coat map
    "CustomTexture",
    BSM_Textures.TextureExportFormat.EXR,
    highQuality: true
);
```

BEST PRACTICES AND TIPS
=====================
1. Start with basic implementations and gradually add complexity as needed.
2. Use debug visualization during development to understand tool behavior.
3. Combine different BSM tools for more complex workflows.
4. Always check return values and success flags.
5. Use appropriate settings for your specific use case.

For complete documentation and updates, visit: https://www.bsm3d.com
Technical support available at: bsm@bsm3d.com