/* 
* BSM Snap
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Extension
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Advanced vertex and object snapping
* - Flexible grid positioning
* - Precise object bounds calculation
* - Multi-criteria surface matching
* - Layer and tag filtering
* - Customizable snap settings
* - Object base offset detection
* - Mesh vertex alignment
* - Surface normal preservation
* - Configurable distance constraints
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace BSMTools
{
    public class BSMSnap
    {
        #region Structures
        public struct SnapResult
        {
            public bool success;             // Si un point de snap a été trouvé
            public Vector3 position;         // Position du point de snap
            public float distance;           // Distance au point de snap
            public Vector3 normal;           // Normale de la surface au point de snap
            public GameObject snapTarget;    // L'objet sur lequel on a snappé
        }

        public struct SnapSettings
        {
            public float maxDistance;        // Distance maximale de snap
            public bool useNormal;           // Si on veut récupérer la normale
            public bool useBaseOffset;       // Si on veut utiliser l'offset de base de l'objet
            public LayerMask layerMask;      // Layers à considérer pour le snap
            public string[] tags;            // Tags des objets à considérer
            public bool requireMeshCollider; // Si on veut uniquement snapper sur des MeshCollider
            public bool includeChildren;      // Si on veut inclure les enfants dans la recherche
            
            public static SnapSettings Default => new SnapSettings
            {
                maxDistance = 2.0f,
                useNormal = true,
                useBaseOffset = true,
                layerMask = -1,
                tags = null,
                requireMeshCollider = false,
                includeChildren = true
            };
        }

        public struct BoundsSettings
        {
            public bool includeInactive;     // Inclure les objets inactifs
            public bool includeChildren;     // Inclure les enfants
            public bool useColliders;        // Utiliser les colliders
            public bool useRenderers;        // Utiliser les renderers
            public Vector3 fallbackSize;     // Taille par défaut si rien n'est trouvé

            public static BoundsSettings Default => new BoundsSettings
            {
                includeInactive = false,
                includeChildren = true,
                useColliders = true,
                useRenderers = true,
                fallbackSize = Vector3.one * 0.5f
            };
        }
        #endregion

        #region Vertex Snapping
        public static SnapResult SnapToNearestVertex(Vector3 position, GameObject target, SnapSettings settings = default)
        {
            if (settings.Equals(default(SnapSettings)))
                settings = SnapSettings.Default;

            var result = new SnapResult
            {
                success = false,
                position = position,
                distance = float.MaxValue,
                normal = Vector3.up,
                snapTarget = null
            };

            // Si on exige un MeshCollider
            if (settings.requireMeshCollider)
            {
                var meshCollider = target.GetComponent<MeshCollider>();
                if (meshCollider == null || meshCollider.sharedMesh == null)
                    return result;

                return SnapToMeshVertices(position, meshCollider, settings);
            }

            // Sinon, on essaie avec tous les Mesh disponibles
            var meshFilters = settings.includeChildren ? 
                target.GetComponentsInChildren<MeshFilter>() : 
                new[] { target.GetComponent<MeshFilter>() };

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;
                if (!IsValidTarget(meshFilter.gameObject, settings)) continue;

                var snapResult = SnapToMeshVertices(position, meshFilter, settings);
                if (snapResult.success && snapResult.distance < result.distance)
                {
                    result = snapResult;
                    result.snapTarget = meshFilter.gameObject;
                }
            }

            return result;
        }

        public static SnapResult SnapToPoint(Vector3 position, Vector3 snapPoint, SnapSettings settings = default)
        {
            if (settings.Equals(default(SnapSettings)))
                settings = SnapSettings.Default;

            float distance = Vector3.Distance(position, snapPoint);
            bool success = distance <= settings.maxDistance;

            return new SnapResult
            {
                success = success,
                position = success ? snapPoint : position,
                distance = distance,
                normal = Vector3.up,
                snapTarget = null
            };
        }

        public static SnapResult SnapToGrid(Vector3 position, float gridSize, SnapSettings settings = default)
        {
            if (settings.Equals(default(SnapSettings)))
                settings = SnapSettings.Default;

            Vector3 snappedPosition = new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y / gridSize) * gridSize,
                Mathf.Round(position.z / gridSize) * gridSize
            );

            float distance = Vector3.Distance(position, snappedPosition);
            bool success = distance <= settings.maxDistance;

            return new SnapResult
            {
                success = success,
                position = success ? snappedPosition : position,
                distance = distance,
                normal = Vector3.up,
                snapTarget = null
            };
        }
        #endregion

        #region Object Bounds
        public static Bounds GetObjectBounds(GameObject obj, BoundsSettings settings = default)
        {
            if (settings.Equals(default(BoundsSettings)))
                settings = BoundsSettings.Default;

            Bounds bounds = new Bounds(obj.transform.position, settings.fallbackSize);
            bool boundsInitialized = false;

            // Colliders
            if (settings.useColliders)
            {
                var colliders = settings.includeChildren ? 
                    obj.GetComponentsInChildren<Collider>(settings.includeInactive) : 
                    new[] { obj.GetComponent<Collider>() };

                colliders = colliders.Where(c => c != null && !c.isTrigger).ToArray();

                foreach (var collider in colliders)
                {
                    if (!boundsInitialized)
                    {
                        bounds = collider.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }

            // Renderers
            if (settings.useRenderers && !boundsInitialized)
            {
                var renderers = settings.includeChildren ? 
                    obj.GetComponentsInChildren<Renderer>(settings.includeInactive) : 
                    new[] { obj.GetComponent<Renderer>() };

                renderers = renderers.Where(r => r != null).ToArray();

                foreach (var renderer in renderers)
                {
                    if (!boundsInitialized)
                    {
                        bounds = renderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return bounds;
        }

        public static Vector3 GetBaseOffset(GameObject obj, BoundsSettings settings = default)
        {
            var bounds = GetObjectBounds(obj, settings);
            return bounds.min - obj.transform.position;
        }
        #endregion

        #region Support Methods
        private static bool IsValidTarget(GameObject obj, SnapSettings settings)
        {
            // Vérification des layers
            if (settings.layerMask != -1 && ((1 << obj.layer) & settings.layerMask) == 0)
                return false;

            // Vérification des tags
            if (settings.tags != null && settings.tags.Length > 0)
                if (!settings.tags.Contains(obj.tag))
                    return false;

            return true;
        }

        private static SnapResult SnapToMeshVertices(Vector3 position, MeshFilter meshFilter, SnapSettings settings)
        {
            var result = new SnapResult
            {
                success = false,
                position = position,
                distance = float.MaxValue,
                normal = Vector3.up
            };

            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = settings.useNormal ? mesh.normals : null;

            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = meshFilter.transform.TransformPoint(vertex);
                float distance = Vector3.Distance(position, worldVertex);

                if (distance <= settings.maxDistance && distance < result.distance)
                {
                    result.success = true;
                    result.position = worldVertex;
                    result.distance = distance;
                    
                    if (settings.useNormal && normals != null)
                    {
                        int vertexIndex = System.Array.IndexOf(vertices, vertex);
                        if (vertexIndex >= 0)
                            result.normal = meshFilter.transform.TransformDirection(normals[vertexIndex]);
                    }
                }
            }

            return result;
        }

        private static SnapResult SnapToMeshVertices(Vector3 position, MeshCollider meshCollider, SnapSettings settings)
        {
            var result = new SnapResult
            {
                success = false,
                position = position,
                distance = float.MaxValue,
                normal = Vector3.up
            };

            Mesh mesh = meshCollider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = settings.useNormal ? mesh.normals : null;

            foreach (var vertex in vertices)
            {
                Vector3 worldVertex = meshCollider.transform.TransformPoint(vertex);
                float distance = Vector3.Distance(position, worldVertex);

                if (distance <= settings.maxDistance && distance < result.distance)
                {
                    result.success = true;
                    result.position = worldVertex;
                    result.distance = distance;

                    if (settings.useNormal && normals != null)
                    {
                        int vertexIndex = System.Array.IndexOf(vertices, vertex);
                        if (vertexIndex >= 0)
                            result.normal = meshCollider.transform.TransformDirection(normals[vertexIndex]);
                    }
                }
            }

            return result;
        }
        #endregion
    }
}