/*
* BSM Scatter
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Distribution algorithms
* - Noise field generation
* - Clustering systems
* - Ecological patterns
* - Performance optimization
* - Placement validation
* - Random sampling methods
* - Spatial partitioning
* - Point cloud analysis
* - Distribution management
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*
* https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
* https://www.wisdom.weizmann.ac.il/~ylipman/
* https://www.redblobgames.com/maps/terrain-from-noise/
*/


using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using System.Linq;

namespace BSMTools
{
    public static class BSMScatter
    {
        #region Structures
        public struct ScatterPoint
        {
            public Vector3 Position;
            public float Rotation;
            public float Scale;
            public float Density;
            public float Slope;  // pente ajoutée pour un meilleur alignement de la surface
            public Vector3 Normal;  // ajoutée pour un meilleur alignement de la surface

            public ScatterPoint(Vector3 pos, float rot = 0f, float scale = 1f, float density = 1f, float slope = 0f, Vector3? normal = null)
            {
                Position = pos;
                Rotation = rot;
                Scale = scale;
                Density = density;
                Slope = slope;
                Normal = normal ?? Vector3.up;
            }
        }

        public struct ScatterZone
        {
            public Vector3 Center; // Centre de la zone de dispersion
            public Vector3 Size; // Taille de la zone de dispersion
            public Vector3 Normal; // Normale de la zone de dispersion
            public float Slope; // Pente de la zone de dispersion
            public AnimationCurve SlopeFalloff;  // Courbe d'animation pour le falloff de la pente
            public float MinHeight;  // Hauteur minimale pour les contraintes de hauteur
            public float MaxHeight; // Hauteur maximale pour les contraintes de hauteur


            public ScatterZone(Vector3 center, Vector3 size, Vector3 normal, float slope, 
                AnimationCurve slopeFalloff = null,                 float minHeight = float.NegativeInfinity, 
                float maxHeight = float.PositiveInfinity)
            {
                Center = center;
                Size = size;
                Normal = normal;
                Slope = slope;
                SlopeFalloff = slopeFalloff ?? AnimationCurve.Linear(0, 1, 1, 0);
                MinHeight = minHeight;
                MaxHeight = maxHeight;
            }
        }

        public struct ScatterSettings
        {
            public float MinScale; // Échelle minimale
            public float MaxScale; // Échelle maximale
            public float MinRotation; // Rotation minimale
            public float MaxRotation; // Rotation maximale
            public bool AlignToNormal; // Aligner à la normale
            public float RandomRotationWeight; // Poids de la rotation aléatoire
            public AnimationCurve DensityFalloff; // Courbe d'animation pour le falloff de la densité
            public float JitterStrength; // Force du jitter
            public static ScatterSettings Default => new ScatterSettings
            {
                MinScale = 0.8f,
                MaxScale = 1.2f,
                MinRotation = 0f,
                MaxRotation = 360f,
                AlignToNormal = true,
                RandomRotationWeight = 0.3f,
                DensityFalloff = AnimationCurve.Linear(0, 1, 1, 0),
                JitterStrength = 0.5f
            };
        }
        #endregion

        #region Distribution Algorithms
        public static async Task<List<ScatterPoint>> GenerateTreeDistributionAsync(
            ScatterZone zone,
            float minDistance,
            ScatterSettings settings,
            float relaxation = 0.5f,
            int maxAttempts = 30)
        {
            return await Task.Run(() =>
            {
                List<ScatterPoint> points = new List<ScatterPoint>();
                var grid = new SpatialGrid(zone.Size, minDistance);
                
                var candidates = GeneratePoissonDiskCandidates(zone, minDistance, relaxation);
                
                foreach (var candidate in candidates)
                {
                    if (ValidatePosition(candidate, zone, grid, minDistance))
                    {
                        var point = CreateScatterPoint(candidate, zone, settings);
                        points.Add(point);
                        grid.Add(point);
                    }
                }
                
                return points;
            });
        }

        public static async Task<List<ScatterPoint>> GenerateFlowerDistributionAsync(
            ScatterZone zone,
            float density,
            ScatterSettings settings)
        {
            return await Task.Run(() =>
            {
                var points = new List<ScatterPoint>();
                var noiseMap = GenerateLayeredNoise(zone, density);
                
                int resolution = noiseMap.GetLength(0);
                float stepX = zone.Size.x / resolution;
                float stepZ = zone.Size.z / resolution;

                Parallel.For(0, resolution, x =>
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        if (noiseMap[x, z] > settings.DensityFalloff.Evaluate(0.5f))
                        {
                            Vector3 position = CalculatePointPosition(x, z, stepX, stepZ, zone, settings);
                            var point = CreateScatterPoint(position, zone, settings);
                            lock(points) points.Add(point);
                        }
                    }
                });

                return points;
            });
        }

        public static List<ScatterPoint> GenerateClusteredDistribution(
            ScatterZone zone,
            int clusterCount,
            int pointsPerCluster,
            float clusterRadius,
            ScatterSettings settings)
        {
            var points = new List<ScatterPoint>();
            var clusters = new List<Vector3>();
            
            // Générer les centres de cluster en utilisant la distribution de bruit bleu 'Blue Noise'
            var blueNoise = GenerateBlueNoise(zone, clusterCount);
            clusters.AddRange(blueNoise);
            
            // Générer des points dans les clusters en utilisant des distributions variées
            foreach (var cluster in clusters)
            {
                var clusterZone = new ScatterZone(
                    cluster,
                    new Vector3(clusterRadius * 2, zone.Size.y, clusterRadius * 2),
                    zone.Normal,
                    zone.Slope
                );

                var distribution = UnityEngine.Random.value;
                List<ScatterPoint> clusterPoints;

                if (distribution < 0.33f)
                {
                    // Distribution gaussienne
                    clusterPoints = GenerateGaussianCluster(clusterZone, pointsPerCluster, settings);
                }
                else if (distribution < 0.66f)
                {
                    // Distribution en anneau
                    clusterPoints = GenerateRingCluster(clusterZone, pointsPerCluster, settings);
                }
                else
                {
                    // Distribution en spirale
                    clusterPoints = GenerateSpiralCluster(clusterZone, pointsPerCluster, settings);
                }

                points.AddRange(clusterPoints);
            }

            return points;

        }
            
            // Distribution DLA
            public static async Task<List<ScatterPoint>> GenerateDLADistributionAsync(
                ScatterZone zone,
                int particleCount,
                ScatterSettings settings)
            {
                return await Task.Run(() =>
                {
                    var points = new List<ScatterPoint>();
                    var grid = new HashSet<Vector2Int>();
                    
                    // Ajouter la graine initiale
                    var seed = CreateScatterPoint(zone.Center, zone, settings);
                    points.Add(seed);
                    grid.Add(Vector2Int.FloorToInt(new Vector2(seed.Position.x, seed.Position.z)));
                    
                    int attempts = 0;
                    int maxAttempts = particleCount * 100;
                    
                    while (points.Count < particleCount && attempts < maxAttempts)
                    {
                        // Générer un marcheur aléatoire
                        var walker = GenerateRandomParticlePosition(zone);
                        bool stuck = false;
                        
                        // Marcher jusqu'à ce qu'il colle ou quitte les limites
                        while (!stuck && IsInZoneBounds(walker, zone))
                        {
                            // Vérifier les voisins
                            var gridPos = Vector2Int.FloorToInt(new Vector2(walker.x, walker.z));
                            if (HasNeighborInGrid(gridPos, grid))
                            {
                                var point = CreateScatterPoint(walker, zone, settings);
                                points.Add(point);
                                grid.Add(gridPos);
                                stuck = true;
                            }
                            else
                            {
                                // Marche aléatoire
                                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                                walker += new Vector3(
                                    Mathf.Cos(angle),
                                    0,
                                    Mathf.Sin(angle)
                                );
                            }
                        }
                        
                        attempts++;
                    }
                    
                    return points;
                });
            }

        // Distribution de tuiles de Wang (rochers, arbres, etc.)
        // https://graphics.uni-konstanz.de/publikationen/Cohen2003WangTilesImage/Cohen2003WangTilesImage.pdf
        public struct WangTile
                {
                    public int[] Edges; // Nord, Est, Sud, Ouest
                    public float[] Heights; // Valeurs de hauteur pour la tuile
                    public float[] Densities; // Valeurs de densité pour le placement des objets
                }

                public static List<ScatterPoint> GenerateGeologicalDistribution(
                    ScatterZone zone,
                    ScatterSettings settings)
                {
                    int gridSize = Mathf.CeilToInt(Mathf.Max(zone.Size.x, zone.Size.z) / 10f);
                    var tiles = GenerateWangTileSet();
                    var tileGrid = new WangTile[gridSize, gridSize];
                    var points = new List<ScatterPoint>();
                    
                    // Remplir la grille avec des tuiles compatibles
                    for (int x = 0; x < gridSize; x++)
                    {
                        for (int z = 0; z < gridSize; z++)
                        {
                            tileGrid[x, z] = GetCompatibleTile(tileGrid, x, z, tiles);
                            
                            // Générer des points en fonction de la densité de la tuile
                            var tile = tileGrid[x, z];
                            float tileSize = zone.Size.x / gridSize;
                            Vector3 tileCenter = new Vector3(
                                x * tileSize + zone.Center.x - zone.Size.x/2,
                                zone.Center.y,
                                z * tileSize + zone.Center.z - zone.Size.z/2
                            );
                            
                            // Ajouter des points en fonction de la densité de la tuile
                            for (int i = 0; i < tile.Densities.Length; i++)
                            {
                                if (UnityEngine.Random.value < tile.Densities[i])
                                {
                                    Vector3 offset = new Vector3(
                                        UnityEngine.Random.Range(0, tileSize),
                                        tile.Heights[i],
                                        UnityEngine.Random.Range(0, tileSize)
                                    );
                                    
                                    var point = CreateScatterPoint(
                                        tileCenter + offset,
                                        zone,
                                        settings
                                    );
                                    points.Add(point);
                                }
                            }
                        }
                    }
                    
                    return points;
                }

                        private static List<WangTile> GenerateWangTileSet()
        {
            var tiles = new List<WangTile>();
            
            // Créer un ensemble de tuiles avec différentes caractéristiques
            tiles.Add(new WangTile 
            { 
                Edges = new int[] { 1, 2, 3, 0 },  // Nord, Est, Sud, Ouest
                Heights = new float[] { 0.2f, 0.3f, 0.1f },
                Densities = new float[] { 0.7f, 0.5f, 0.3f }
            });

            tiles.Add(new WangTile 
            { 
                Edges = new int[] { 2, 1, 0, 3 },
                Heights = new float[] { 0.5f, 0.4f, 0.6f },
                Densities = new float[] { 0.6f, 0.8f, 0.4f }
            });

            tiles.Add(new WangTile 
            { 
                Edges = new int[] { 3, 0, 1, 2 },
                Heights = new float[] { 0.1f, 0.2f, 0.4f },
                Densities = new float[] { 0.5f, 0.6f, 0.7f }
            });

            tiles.Add(new WangTile 
            { 
                Edges = new int[] { 0, 3, 2, 1 },
                Heights = new float[] { 0.4f, 0.5f, 0.3f },
                Densities = new float[] { 0.8f, 0.4f, 0.5f }
            });

            return tiles;
        }

                private static WangTile GetCompatibleTile(
                    WangTile[,] grid,
                    int x,
                    int z,
                    List<WangTile> tiles)
                {
                    // Obtenir les contraintes des voisins
                    int? northEdge = z > 0 ? grid[x, z-1].Edges[2] : null;
                    int? westEdge = x > 0 ? grid[x-1, z].Edges[1] : null;
                    
                    // Filtrer les tuiles compatibles
                    var compatible = tiles.Where(t =>
                        (!northEdge.HasValue || t.Edges[0] == northEdge.Value) &&
                        (!westEdge.HasValue || t.Edges[3] == westEdge.Value)
                    ).ToList();
                    
                    // Retourner une tuile compatible aléatoire
                    return compatible[UnityEngine.Random.Range(0, compatible.Count)];
                }

            private static bool HasNeighborInGrid(Vector2Int pos, HashSet<Vector2Int> grid)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        var neighbor = pos + new Vector2Int(x, y);
                        if (grid.Contains(neighbor)) return true;
                    }
                }
                return false;
            }

        #endregion

        #region Helper Methods
        private static float[,] GenerateLayeredNoise(ScatterZone zone, float density)
        {
            int resolution = 128; // Résolution
            float[,] noiseMap = new float[resolution, resolution];
            // Plusieurs couches de bruit avec différentes fréquences et amplitudes
            float[] frequencies = { 1f, 2f, 4f, 8f };
            float[] amplitudes = { 0.5f, 0.25f, 0.125f, 0.0625f };
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float noise = 0f;
                    float scale = 1f / density;
                    
                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        float xCoord = (float)x / resolution * scale * frequencies[i];
                        float yCoord = (float)y / resolution * scale * frequencies[i];
                        
                        noise += Mathf.PerlinNoise(xCoord + UnityEngine.Random.value * 1000f, 
                                                 yCoord + UnityEngine.Random.value * 1000f) * amplitudes[i];
                    }
                    
                    // Ajouter du bruit de Worley pour des motifs plus naturels
                    noise = Mathf.Lerp(noise, GenerateWorleyNoise(x, y, resolution), 0.3f);
                    noiseMap[x, y] = noise;
                }
            }
            
            return noiseMap;
        }

        private static float GenerateWorleyNoise(int x, int y, int resolution)
        {
            var points = new List<Vector2>();
            int cellSize = resolution / 8;
            
            // Générer des points
            for (int i = 0; i < 16; i++)
            {
                points.Add(new Vector2(
                    UnityEngine.Random.Range(0, resolution),
                    UnityEngine.Random.Range(0, resolution)
                ));
            }
            
            // Trouver la distance au point le plus proche
            float minDist = float.MaxValue;
            Vector2 pos = new Vector2(x, y);
            
            foreach (var point in points)
            {
                float dist = Vector2.Distance(pos, point);
                minDist = Mathf.Min(minDist, dist);
            }
            
            return Mathf.Clamp01(minDist / (resolution * 0.1f));
        }

        private class SpatialGrid
        {
            private readonly float cellSize;
            private readonly Dictionary<Vector2Int, List<ScatterPoint>> grid;
            
            public SpatialGrid(Vector3 size, float minDistance)
            {
                cellSize = minDistance * 2f;
                grid = new Dictionary<Vector2Int, List<ScatterPoint>>();
            }
            
            public void Add(ScatterPoint point)
            {
                var cell = GetCell(point.Position);
                if (!grid.ContainsKey(cell))
                    grid[cell] = new List<ScatterPoint>();
                grid[cell].Add(point);
            }
            
            public bool CheckOverlap(Vector3 position, float minDistance)
            {
                var cell = GetCell(position);
                var cellsToCheck = GetNeighboringCells(cell);
                
                foreach (var checkCell in cellsToCheck)
                {
                    if (!grid.ContainsKey(checkCell)) continue;
                    
                    foreach (var point in grid[checkCell])
                    {
                        if (Vector3.Distance(position, point.Position) < minDistance)
                            return true;
                    }
                }
                
                return false;
            }
            
            private Vector2Int GetCell(Vector3 position)
            {
                return new Vector2Int(
                    Mathf.FloorToInt(position.x / cellSize),
                    Mathf.FloorToInt(position.z / cellSize)
                );
            }
            
            private IEnumerable<Vector2Int> GetNeighboringCells(Vector2Int cell)
            {
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        yield return new Vector2Int(cell.x + x, cell.y + y);
            }
        }

        private static ScatterPoint CreateScatterPoint(Vector3 position, ScatterZone zone, ScatterSettings settings)
        {
            // Sampling des données du terrain
            RaycastHit hit;
            Vector3 normal = zone.Normal;
            float slope = zone.Slope;
            
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit))
            {
                normal = hit.normal;
                slope = Vector3.Angle(normal, Vector3.up);
                position = hit.point;
            }
            
            // Calculer la rotation en fonction de la normale et des paramètres
            float baseRotation = UnityEngine.Random.Range(settings.MinRotation, settings.MaxRotation);
            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
            Quaternion randomRotation = Quaternion.Euler(0, baseRotation, 0);
            Quaternion finalRotation = Quaternion.Lerp(normalRotation, randomRotation, settings.RandomRotationWeight);
            
            return new ScatterPoint(
                position,
                finalRotation.eulerAngles.y,
                UnityEngine.Random.Range(settings.MinScale, settings.MaxScale),
                settings.DensityFalloff.Evaluate(UnityEngine.Random.value),
                slope,
                normal
            );
        }

        private static List<Vector3> GenerateBlueNoise(ScatterZone zone, int count)
        {
            // Echantillonnage rapide (enfin j'espère) de disque de Poisson pour le bruit bleu
            var points = new List<Vector3>();
            float cellSize = Mathf.Sqrt(zone.Size.x * zone.Size.z / (count * 2f));
            var grid = new Dictionary<Vector2Int, Vector3>();
            
            // Point initial
            var firstPoint = GenerateRandomParticlePosition(zone);
            points.Add(firstPoint);
            AddToGrid(firstPoint, cellSize, grid);
            
            // Générer les points restants
            var activePoints = new Queue<Vector3>();
            activePoints.Enqueue(firstPoint);
            
            while (activePoints.Count > 0 && points.Count < count)
            {
                var current = activePoints.Dequeue();
                
                for (int i = 0; i < 30; i++)
                {
                    var candidate = GenerateCandidatePoint(current, cellSize * 2f, zone);
                    
                    if (IsValidBlueNoisePoint(candidate, cellSize, grid, zone))
                    {
                        points.Add(candidate);
                        activePoints.Enqueue(candidate);
                        AddToGrid(candidate, cellSize, grid);
                        
                        if (points.Count >= count) break;
                    }
                }
            }
            
            return points;
        }

        private static void AddToGrid(Vector3 point, float cellSize, Dictionary<Vector2Int, Vector3> grid)
        {
            var cell = new Vector2Int(
                Mathf.FloorToInt(point.x / cellSize),
                Mathf.FloorToInt(point.z / cellSize)
            );
            grid[cell] = point;
        }

        private static bool IsValidBlueNoisePoint(Vector3 point, float cellSize, Dictionary<Vector2Int, Vector3> grid, ScatterZone zone)
        {
            if (!IsInZoneBounds(point, zone)) return false;
            
            var cell = new Vector2Int(
                Mathf.FloorToInt(point.x / cellSize),
                Mathf.FloorToInt(point.z / cellSize)
            );
            
            // Vérifier les cellules voisines
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    var checkCell = new Vector2Int(cell.x + x, cell.y + y);
                    if (grid.ContainsKey(checkCell))
                    {
                        var existing = grid[checkCell];
                        if (Vector3.Distance(point, existing) < cellSize)
                            return false;
                    }
                }
            }
            
            return true;
        }

        private static bool IsInZoneBounds(Vector3 point, ScatterZone zone)
        {
            return point.x >= zone.Center.x - zone.Size.x / 2f &&
                   point.x <= zone.Center.x + zone.Size.x / 2f &&
                   point.z >= zone.Center.z - zone.Size.z / 2f &&
                   point.z <= zone.Center.z + zone.Size.z / 2f;
        }

        #region Advanced Distribution Patterns
        private static List<ScatterPoint> GenerateGaussianCluster(
            ScatterZone zone,
            int count,
            ScatterSettings settings)
        {
            var points = new List<ScatterPoint>();
            float radius = Mathf.Min(zone.Size.x, zone.Size.z) / 2f;
            
            for (int i = 0; i < count; i++)
            {
                // Transformation de Box-Muller pour la distribution gaussienne
                float u1 = UnityEngine.Random.value;
                float u2 = UnityEngine.Random.value;
                
                float radius2D = radius * Mathf.Sqrt(-2f * Mathf.Log(u1));
                float theta = 2f * Mathf.PI * u2;
                
                Vector3 offset = new Vector3(
                    radius2D * Mathf.Cos(theta),
                    0f,
                    radius2D * Mathf.Sin(theta)
                );
                
                Vector3 position = zone.Center + offset;
                if (IsInZoneBounds(position, zone))
                {
                    points.Add(CreateScatterPoint(position, zone, settings));
                }
            }
            
            return points;
        }

        private static List<ScatterPoint> GenerateRingCluster(
            ScatterZone zone,
            int count,
            ScatterSettings settings)
        {
            var points = new List<ScatterPoint>();
            float radius = Mathf.Min(zone.Size.x, zone.Size.z) / 2f;
            float ringWidth = radius * 0.2f;
            
            for (int i = 0; i < count; i++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                float r = radius + UnityEngine.Random.Range(-ringWidth, ringWidth);
                
                Vector3 offset = new Vector3(
                    r * Mathf.Cos(angle),
                    0f,
                    r * Mathf.Sin(angle)
                );
                
                Vector3 position = zone.Center + offset;
                if (IsInZoneBounds(position, zone))
                {
                    points.Add(CreateScatterPoint(position, zone, settings));
                }
            }
            
            return points;
        }

        private static List<ScatterPoint> GenerateSpiralCluster(
            ScatterZone zone,
            int count,
            ScatterSettings settings)
        {
            var points = new List<ScatterPoint>();
            float radius = Mathf.Min(zone.Size.x, zone.Size.z) / 2f;
            float spiralTightness = 0.5f;
            
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float angle = t * Mathf.PI * 8f; // 4 rotations
                float r = t * radius;
                
                Vector3 offset = new Vector3(
                    r * Mathf.Cos(angle),
                    0f,
                    r * Mathf.Sin(angle)
                );
                
                // Ajouter un peu de Random à la spirale
                offset += new Vector3(
                    UnityEngine.Random.Range(-spiralTightness, spiralTightness),
                    0f,
                    UnityEngine.Random.Range(-spiralTightness, spiralTightness)
                );
                
                Vector3 position = zone.Center + offset;
                if (IsInZoneBounds(position, zone))
                {
                    points.Add(CreateScatterPoint(position, zone, settings));
                }
            }
            
            return points;
        }
        #endregion

        #region Performance Optimization
        public static async Task<List<ScatterPoint>> OptimizeDistributionAsync(
            List<ScatterPoint> points,
            ScatterZone zone,
            float minDistance,
            float targetDensity,
            ScatterSettings settings)
        {
            return await Task.Run(() =>
            {
                var optimized = new List<ScatterPoint>(points);
                var spatialGrid = new SpatialGrid(zone.Size, minDistance);
                
                // Supprimer les points qui se chevauchent
                optimized = RemoveOverlappingPointsOptimized(optimized, spatialGrid, minDistance);
                
                // Remplir les lacunes en utilisant l'échantillonnage de bruit bleu
                if (CalculateActualDensity(optimized, zone) < targetDensity)
                {
                    var gapFiller = GenerateBlueNoiseGapFiller(zone, optimized, targetDensity, minDistance, settings);
                    optimized.AddRange(gapFiller);
                }
                
                return optimized;
            });
        }

        private static List<ScatterPoint> RemoveOverlappingPointsOptimized(
            List<ScatterPoint> points,
            SpatialGrid grid,
            float minDistance)
        {
            var result = new List<ScatterPoint>();
            
            // Trier les points par densité pour garder les plus importants
            var sortedPoints = points.OrderByDescending(p => p.Density).ToList();
            
            foreach (var point in sortedPoints)
            {
                if (!grid.CheckOverlap(point.Position, minDistance))
                {
                    result.Add(point);
                    grid.Add(point);
                }
            }
            
            return result;
        }

        private static List<ScatterPoint> GenerateBlueNoiseGapFiller(
            ScatterZone zone,
            List<ScatterPoint> existingPoints,
            float targetDensity,
            float minDistance,
            ScatterSettings settings)
        {
            var fillerPoints = new List<ScatterPoint>();
            var grid = new SpatialGrid(zone.Size, minDistance);
            
            // Ajouter les points existants à la grille
            foreach (var point in existingPoints)
            {
                grid.Add(point);
            }
            
            // Calculer le nombre de points nécessaires
            float area = zone.Size.x * zone.Size.z;
            int targetCount = Mathf.CeilToInt(area * targetDensity);
            int pointsNeeded = targetCount - existingPoints.Count;
            
            if (pointsNeeded <= 0) return fillerPoints;
            
            var blueNoise = GenerateBlueNoise(zone, pointsNeeded * 2);
            
            foreach (var candidate in blueNoise)
            {
                if (fillerPoints.Count >= pointsNeeded) break;
                
                if (!grid.CheckOverlap(candidate, minDistance))
                {
                    var point = CreateScatterPoint(candidate, zone, settings);
                    fillerPoints.Add(point);
                    grid.Add(point);
                }
            }
            
            return fillerPoints;
        }
        #endregion

        #region Distribution Analysis
        public static float CalculateClusteringIndex(List<ScatterPoint> points)
        {
            if (points.Count < 2) return 0f;
            
            float totalRipleyK = 0f;
            float maxDistance = float.NegativeInfinity;
            
            // Trouver la distance maximale entre deux points sans importance
            foreach (var p1 in points)
            {
                foreach (var p2 in points)
                {
                    maxDistance = Mathf.Max(maxDistance, Vector3.Distance(p1.Position, p2.Position));
                }
            }
            
            // Calculer la fonction K de Ripley à différentes échelles
            float[] scales = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            foreach (float scale in scales)
            {
                float radius = maxDistance * scale;
                float k = CalculateRipleysK(points, radius);
                totalRipleyK += k;
            }
            
            return totalRipleyK / scales.Length;
        }

        private static float CalculateRipleysK(List<ScatterPoint> points, float radius)
        {
            float area = CalculatePointCloudArea(points);
            float lambda = points.Count / area;
            float k = 0f;
            
            foreach (var p1 in points)
            {
                int count = 0;
                foreach (var p2 in points)
                {
                    if (p1.Position != p2.Position && 
                        Vector3.Distance(p1.Position, p2.Position) <= radius)
                    {
                        count++;
                    }
                }
                k += count;
            }
            
            k /= (points.Count * lambda);
            return k;
        }

        private static float CalculatePointCloudArea(List<ScatterPoint> points)
        {
            if (points.Count < 2) return 0f;
            
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
            
            foreach (var point in points)
            {
                minX = Mathf.Min(minX, point.Position.x);
                maxX = Mathf.Max(maxX, point.Position.x);
                minZ = Mathf.Min(minZ, point.Position.z);
                maxZ = Mathf.Max(maxZ, point.Position.z);
            }
            
                            return (maxX - minX) * (maxZ - minZ);
        }
        #endregion

        #region Missing Helper Methods
        private static List<Vector3> GeneratePoissonDiskCandidates(ScatterZone zone, float minDistance, float relaxation)
        {
            var candidates = new List<Vector3>();
            float cellSize = minDistance / Mathf.Sqrt(2);
            
            int gridWidth = Mathf.CeilToInt(zone.Size.x / cellSize);
            int gridHeight = Mathf.CeilToInt(zone.Size.z / cellSize);
            
            // Générer le point de départ
            candidates.Add(new Vector3(
                zone.Center.x,
                zone.Center.y,
                zone.Center.z
            ));
            
            // Traiter ceux qui sont actifs
            var activePoints = new Queue<Vector3>();
            activePoints.Enqueue(candidates[0]);
            
            while (activePoints.Count > 0)
            {
                var current = activePoints.Dequeue();
                
                for (int i = 0; i < 30; i++)
                {
                    var candidate = GenerateCandidatePoint(current, minDistance * (1f + relaxation), zone);
                    
                    if (ValidatePosition(candidate, zone, null, minDistance))
                    {
                        candidates.Add(candidate);
                        activePoints.Enqueue(candidate);
                    }
                }
            }
            
            return candidates;
        }

        private static bool ValidatePosition(Vector3 position, ScatterZone zone, SpatialGrid grid, float minDistance)
        {
            // Vérifier les limites de la zone
            if (!IsInZoneBounds(position, zone))
                return false;
                
            // Vérifier les contraintes de hauteur
            if (position.y < zone.MinHeight || position.y > zone.MaxHeight)
                return false;
                
            // Vérifier les contraintes de pente si nous avons des données de terrain
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit))
            {
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                float slopeInfluence = zone.SlopeFalloff.Evaluate(slope / 90f);
                if (slopeInfluence < 0.01f)
                    return false;
            }
            
            // Vérifier la grille spatiale (si elle fournie)
            if (grid != null && grid.CheckOverlap(position, minDistance))
                return false;
                
            return true;
        }

        private static Vector3 CalculatePointPosition(int x, int z, float stepX, float stepZ, ScatterZone zone, ScatterSettings settings)
        {
            float jitterX = UnityEngine.Random.Range(-stepX, stepX) * settings.JitterStrength;
            float jitterZ = UnityEngine.Random.Range(-stepZ, stepZ) * settings.JitterStrength;
            
            return new Vector3(
                x * stepX + jitterX,
                0,
                z * stepZ + jitterZ
            ) + zone.Center - zone.Size * 0.5f;
        }

        private static Vector3 GenerateRandomParticlePosition(ScatterZone zone)
        {
            return new Vector3(
                UnityEngine.Random.Range(zone.Center.x - zone.Size.x/2, zone.Center.x + zone.Size.x/2),
                zone.Center.y,
                UnityEngine.Random.Range(zone.Center.z - zone.Size.z/2, zone.Center.z + zone.Size.z/2)
            );
        }

        private static Vector3 GenerateCandidatePoint(Vector3 center, float radius, ScatterZone zone)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float r = radius;
            
            return new Vector3(
                center.x + Mathf.Cos(angle) * r,
                center.y,
                center.z + Mathf.Sin(angle) * r
            );
        }

        public static float CalculateActualDensity(List<ScatterPoint> points, ScatterZone zone)
        {
            float area = zone.Size.x * zone.Size.z;
            return points.Count / area;
        }
        #endregion
    }
    #endregion
}