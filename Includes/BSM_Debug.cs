/* 
* BSM Debug
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Advanced debug visualization
* - 3D gizmo rendering
* - Point and vector drawing
* - Surface contact visualization
* - Raycast debug support
* - Customizable debug settings
* - Label and annotation system
* - Bounds and zone rendering
* - Normal direction visualization
* - Dynamic visualization management
* - Scene debug insights
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BSMTools
{
    public class BSMDebug
    {
        #region Structures
        public struct DebugSettings
        {
            public float duration;          // Durée d'affichage
            public bool persistent;         // Si la visualisation doit persister
            public Color primaryColor;      // Couleur principale
            public Color secondaryColor;    // Couleur secondaire
            public float size;              // Taille des gizmos
            public GUIStyle labelStyle;     // Style des labels
            public bool showLabels;         // Afficher les labels
            public float labelOffset;       // Décalage des labels
            public int drawOrder;           // Ordre de dessin (pour la superposition)

            public static DebugSettings Default => new DebugSettings
            {
                duration = 0f,
                persistent = false,
                primaryColor = Color.green,
                secondaryColor = Color.yellow,
                size = 0.1f,
                showLabels = true,
                labelOffset = 0.2f,
                drawOrder = 0
            };
        }

        public struct VisualizationState
        {
            public bool isActive;
            public float startTime;
            public float endTime;
            public System.Action drawAction;
        }
        #endregion

        #region Private Fields
        private static readonly Dictionary<string, VisualizationState> activeVisualizations = new Dictionary<string, VisualizationState>();
        private const float MIN_SIZE = 0.001f;
        private static GUIStyle defaultLabelStyle;
        #endregion

        #region Point and Vector Visualization
        public static void DrawPoint(Vector3 position, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Handles.color = settings.primaryColor;
                Handles.DrawWireCube(position, Vector3.one * settings.size);

                if (settings.showLabels && !string.IsNullOrEmpty(label))
                {
                    DrawLabel(position + Vector3.up * settings.labelOffset, label, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }

        public static void DrawVector(Vector3 start, Vector3 direction, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Handles.color = settings.primaryColor;
                Handles.DrawLine(start, start + direction);
                
                // Arrow head
                Vector3 endPoint = start + direction;
                Handles.ConeHandleCap(
                    0,
                    endPoint,
                    Quaternion.LookRotation(direction),
                    settings.size,
                    EventType.Repaint
                );

                if (settings.showLabels && !string.IsNullOrEmpty(label))
                {
                    Vector3 labelPos = start + direction * 0.5f + Vector3.up * settings.labelOffset;
                    DrawLabel(labelPos, label, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }

        public static void DrawNormal(Vector3 position, Vector3 normal, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Handles.color = settings.primaryColor;
                Handles.DrawLine(position, position + normal * settings.size * 2);
                
                Handles.DrawWireDisc(position + normal * settings.size * 2, normal, settings.size * 0.5f);
                
                if (settings.showLabels)
                {
                    string angleText = label;
                    if (string.IsNullOrEmpty(angleText))
                    {
                        float angle = Vector3.Angle(normal, Vector3.up);
                        angleText = $"{angle:F1}°";
                    }
                    DrawLabel(position + normal * settings.size * 2.2f, angleText, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }
        #endregion

        #region Surface Visualization
        public static void DrawContact(RaycastHit hit, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Handles.color = settings.primaryColor;
                Handles.DrawWireCube(hit.point, Vector3.one * settings.size);
                
                Handles.color = settings.secondaryColor;
                Handles.DrawLine(hit.point, hit.point + hit.normal * settings.size);
                
                Handles.ConeHandleCap(
                    0,
                    hit.point + hit.normal * settings.size,
                    Quaternion.LookRotation(hit.normal),
                    settings.size * 0.5f,
                    EventType.Repaint
                );

                if (settings.showLabels && !string.IsNullOrEmpty(label))
                {
                    DrawLabel(hit.point + Vector3.up * settings.labelOffset, label, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }

        public static void DrawSnapZone(Vector3 center, float radius, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Color snapColor = settings.primaryColor;
                snapColor.a *= 0.3f;
                Handles.color = snapColor;

                Handles.DrawWireDisc(center, Vector3.up, radius);
                Handles.DrawWireDisc(center, Vector3.right, radius);
                Handles.DrawWireDisc(center, Vector3.forward, radius);

                if (settings.showLabels && !string.IsNullOrEmpty(label))
                {
                    DrawLabel(center + Vector3.up * settings.labelOffset, label, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }

        public static void DrawBounds(Bounds bounds, string label = "", DebugSettings settings = default)
        {
            if (settings.Equals(default(DebugSettings)))
                settings = DebugSettings.Default;

            var id = System.Guid.NewGuid().ToString();
            System.Action drawAction = () =>
            {
                #if UNITY_EDITOR
                Handles.color = settings.primaryColor;
                DrawWireBounds(bounds);

                if (settings.showLabels && !string.IsNullOrEmpty(label))
                {
                    DrawLabel(bounds.center + Vector3.up * settings.labelOffset, label, settings);
                }
                #endif
            };

            RegisterVisualization(id, drawAction, settings);
        }
        #endregion

        #region Legacy Support
        // Méthode de compatibilité pour BSM_Drop
        public static void DrawDebugVisuals(RaycastHit hit, Vector3 position, float raycastOffset, 
            float snapDistance, bool showSurfaceNormals, bool showContactPoints, bool showSnapZone)
        {
            var defaultSettings = DebugSettings.Default;
            
            // Ligne de base du raycast
            DrawVector(position + Vector3.up * raycastOffset, Vector3.down * raycastOffset);
            
            if (showSurfaceNormals)
            {
                DrawNormal(hit.point, hit.normal);
            }

            if (showContactPoints)
            {
                // Pour la compatibilité avec l'ancien format
                var meshCollider = hit.collider as MeshCollider;
                string areaLabel = meshCollider != null ? 
                    $"Area: {CalculateSurfaceArea(meshCollider, hit.triangleIndex):F3}" : "";
                DrawContact(hit, areaLabel);
            }

            if (showSnapZone)
            {
                DrawSnapZone(hit.point, snapDistance);
            }
        }

        // Pour la compatibilité avec l'ancien code
        public static float CalculateSurfaceArea(MeshCollider meshCollider, int triangleIndex)
        {
            if (meshCollider == null || meshCollider.sharedMesh == null) return 0f;

            Mesh mesh = meshCollider.sharedMesh;
            if (triangleIndex < 0 || triangleIndex * 3 + 2 >= mesh.triangles.Length)
                return 0f;

            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            Vector3 v1 = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3]]);
            Vector3 v2 = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3 + 1]]);
            Vector3 v3 = meshCollider.transform.TransformPoint(vertices[triangles[triangleIndex * 3 + 2]]);

            return Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
        }
        #endregion

        #region Support Methods
        private static void RegisterVisualization(string id, System.Action drawAction, DebugSettings settings)
        {
            float endTime = settings.persistent ? float.MaxValue : Time.realtimeSinceStartup + settings.duration;
            
            var state = new VisualizationState
            {
                isActive = true,
                startTime = Time.realtimeSinceStartup,
                endTime = endTime,
                drawAction = drawAction
            };

            activeVisualizations[id] = state;
            
            EditorApplication.update += () =>
            {
                if (Time.realtimeSinceStartup >= endTime)
                {
                    activeVisualizations.Remove(id);
                }
            };
        }

        private static void DrawLabel(Vector3 position, string text, DebugSettings settings)
        {
            #if UNITY_EDITOR
            if (defaultLabelStyle == null)
            {
                defaultLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12
                };
            }

            GUIStyle style = settings.labelStyle ?? defaultLabelStyle;
            Handles.Label(position, text, style);
            #endif
        }

        private static void DrawWireBounds(Bounds bounds)
        {
            #if UNITY_EDITOR
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            Vector3[] points = new Vector3[8];
            
            // Les 8 coins du cube
            points[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            points[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            points[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            points[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            points[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            points[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            points[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            points[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            // Les 12 lignes
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(points[i], points[(i + 1) % 4]);            // Base
                Handles.DrawLine(points[i + 4], points[((i + 1) % 4) + 4]); // Top
                Handles.DrawLine(points[i], points[i + 4]);                  // Liens
            }
            #endif
        }
        #endregion
    }
}