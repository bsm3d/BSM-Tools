# BSM Tools Includes Guide
Version 1.0 for Unity 2023+ HDRP
Created by BSM3D

INTRODUCTION
===========
The BSM Tools suite provides a comprehensive set of Includes / utilities designed to enhance your Unity development workflow. Each tool is crafted to solve specific challenges while maintaining high performance and ease of use.

Let me introduce each file in the BSM Tools ./includes/:

**BSM_Align.cs** serves as a sophisticated alignment tool for Unity objects. It handles everything from simple surface alignment to complex multi-object transformations. The tool provides precise control over how objects align with surfaces, including features like normal-based rotation, flexible axis locking, and blend weight controls for smooth transitions.

**BSM_Debug.cs** is a comprehensive debugging visualization system. It enhances Unity's built-in debugging capabilities by providing advanced tools for visualizing physics interactions, surface contacts, and spatial relationships. The tool includes features for drawing custom gizmos, annotating scenes with labels, and visualizing complex geometric relationships in 3D space.

**BSM_Math.cs** acts as a mathematical utility library that extends Unity's built-in math capabilities. It provides a wide range of functions from basic unit conversions to advanced geometric calculations. The library includes specialized functions for handling areas, volumes, interpolation, and statistical calculations, making it invaluable for complex mathematical operations in game development.

**BSM_Noise_Textures.cs** is a powerful procedural texture generation system. It enables the creation of various texture types using different noise algorithms, including Perlin noise, fractional Brownian motion, and Voronoi patterns. The tool can generate complete texture sets including normal maps, mask textures, and coat maps, with support for multiple export formats.

**BSM_Physics.cs** offers advanced physics simulation capabilities beyond Unity's standard physics system. It provides detailed control over physics interactions, including surface validation, iterative physics casting, and sophisticated collision detection. The tool is particularly useful for precise object placement and movement that requires physical accuracy.

**BSM_Raycasting.cs** presents an enhanced raycasting system with sophisticated surface detection and validation features. It supports multiple cast types (ray, sphere, box, capsule) and provides detailed hit information including surface properties, material information, and precise geometric data. The tool is essential for complex environmental interactions and precise object placement.

**BSM_Scatter.cs** implements a comprehensive procedural distribution system for natural object placement. It handles various distribution patterns including Poisson disk, DLA (Diffusion-Limited Aggregation), and Wang tiles with customizable parameters and constraints. The tool includes advanced features for generating ecological patterns, handling terrain-aware placement, and optimizing large-scale distributions, making it invaluable for environment creation and natural landscape generation tasks.

**BSM_Snap.cs** implements a comprehensive snapping system for precise object positioning. It handles vertex snapping, grid snapping, and surface snapping with customizable rules and constraints. The tool includes advanced features for calculating object bounds and handling complex mesh-based snapping scenarios, making it invaluable for level design and object placement tasks.

These tools work together to provide a robust framework for handling common game development challenges in Unity, with a focus on precision, flexibility, and ease of use.


ALIGNMENT SYSTEM (BSM Align)
============================
BSM Align specializes in precise object positioning and rotation, particularly useful for level design and runtime object placement.

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

DEBUG VISUALIZATION (BSM Debug)
===============================
BSM Debug provides powerful visual debugging tools essential for development and testing.

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

MATHEMATICAL UTILITIES (BSM Math)
=================================
BSM Math provides a comprehensive set of mathematical functions for various calculations and transformations.

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

PHYSICS SIMULATION (BSM Physics)
================================
BSM Physics provides advanced physics simulation capabilities for precise object placement and movement.

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

RAY CASTING SYSTEM (BSM Raycasting)
====================================
BSM Raycasting extends Unity's built-in raycasting with advanced features and surface validation.

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

**SCATTER SYSTEM (BSM Scatter)**
============================
BSM Scatter provides advanced procedural distribution capabilities, particularly useful for environment creation and natural landscape generation.

Core Features:
- Natural distribution algorithms (Poisson disk, DLA, Wang tiles)
- Terrain-aware placement
- Density and clustering control
- Performance optimization

Basic Implementation:
```csharp
// The simplest form - distribute objects in a zone
var zone = new BSMScatter.ScatterZone(transform.position, new Vector3(10, 0, 10), Vector3.up, 0f);
var result = await BSMScatter.GenerateTreeDistributionAsync(zone, 2f, ScatterSettings.Default);

// Apply the distribution to objects
foreach(var point in result) {
    Instantiate(prefab, point.Position, Quaternion.Euler(0, point.Rotation, 0));
}
```

Advanced Implementation:
```csharp
// Create settings to control distribution behavior
var settings = new BSMScatter.ScatterSettings {
    minScale = 0.8f,              // Minimum scale variation
    maxScale = 1.2f,              // Maximum scale variation
    alignToNormal = true,         // Align to surface normal
    randomRotationWeight = 0.3f,  // Random rotation influence
    densityFalloff = AnimationCurve.Linear(0, 1, 1, 0),
    jitterStrength = 0.5f         // Position randomization
};

// Create a zone with height and slope constraints
var zone = new BSMScatter.ScatterZone(
    center: transform.position,
    size: new Vector3(20, 0, 20),
    normal: Vector3.up,
    slope: 45f,
    slopeFalloff: AnimationCurve.EaseInOut(0, 1, 1, 0),
    minHeight: 0f,
    maxHeight: 10f
);

// Generate clustered distribution
var result = BSMScatter.GenerateClusteredDistribution(zone, 5, 10, 2f, settings);
```

SNAP SYSTEM (BSM Snap)
=======================
BSM Snap provides precise object positioning capabilities, particularly useful for level design and object placement.

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

TEXTURE GENERATION (BSM Textures)
==================================
BSM Textures provides comprehensive tools for generating and managing procedural textures.

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
3. Combine different BSM tools Includes for more complex workflows.
4. Always check return values and success flags.
5. Use appropriate settings for your specific use case.


