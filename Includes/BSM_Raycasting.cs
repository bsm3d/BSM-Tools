/* 
* BSM Raycasting
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Advanced multi-cast raycast techniques
* - Surface validation algorithms
* - Customizable raycast parameters
* - Collision and hit detection
* - Sophisticated physics querying
* - Flexible cast type support
* - Detailed hit information analysis
* - Debug visualization
* - Physics material and tag filtering
* - Precision casting methods
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BSMTools
{
    public class BSMRaycasting
    {
        #region Structures
        [System.Serializable]
        public class RaycastSettings
        {
            public LayerMask layerMask = -1;               // Layers à tester
            public float maxDistance = 100f;               // Distance maximale
            public float rayStartOffset = 0.1f;            // Offset du point de départ
            public float rayEndOffset = 0.1f;              // Offset du point d'arrivée
            public bool sphereCast = false;                // Utiliser SphereCast au lieu de Raycast
            public float sphereRadius = 0.1f;              // Rayon pour le SphereCast
            public bool boxCast = false;                   // Utiliser BoxCast au lieu de Raycast
            public Vector3 boxHalfExtents = Vector3.one * 0.1f; // Dimensions pour le BoxCast
            public bool capsuleCast = false;               // Utiliser CapsuleCast
            public float capsuleRadius = 0.1f;             // Rayon pour le CapsuleCast
            public float capsuleHeight = 1f;               // Hauteur pour le CapsuleCast
            public bool ignoreTriggers = true;             // Ignorer les triggers
            public bool ignoreOwnColliders = true;         // Ignorer ses propres colliders
            public bool debugDraw = true;                  // Dessiner les raycast en debug
            public float debugDrawDuration = 1f;           // Durée d'affichage du debug
            public Color debugColor = Color.green;         // Couleur du debug

            public static RaycastSettings Default => new RaycastSettings();
        }

        [System.Serializable]
        public class SurfaceSettings
        {
            public float minArea = 0.01f;                  // Surface minimale
            public float maxSlopeAngle = 45f;             // Angle maximum de pente
            public bool requireMeshCollider = false;       // Nécessite un MeshCollider
            public PhysicsMaterial[] validMaterials;        // Matériaux valides
            public string[] validTags;                     // Tags valides
            public bool checkSurfaceNoise = false;         // Vérifier la rugosité
            public float maxSurfaceNoise = 0.1f;          // Rugosité maximale

            public static SurfaceSettings Default => new SurfaceSettings();
        }

        public struct RaycastResult
        {
            public bool hasHit;                // A touché quelque chose
            public Vector3 hitPoint;           // Point d'impact
            public Vector3 hitNormal;          // Normale à l'impact
            public float hitDistance;          // Distance de l'impact
            public GameObject hitObject;        // Objet touché
            public Collider hitCollider;       // Collider touché
            public PhysicsMaterial hitMaterial; // Matériau physique
            public bool isValidSurface;        // La surface est-elle valide
            public string hitTag;              // Tag de l'objet touché
            public Vector2 hitTextureCoord;    // Coordonnées de texture
            public float surfaceSlope;         // Pente de la surface
            public float surfaceArea;          // Aire de la surface

            public static RaycastResult none => new RaycastResult { hasHit = false };
        }
        #endregion

        #region Public Methods
        // Lance un rayon avec les paramètres spécifiés et retourne le premier hit
        public static RaycastResult Cast(Vector3 origin, Vector3 direction, RaycastSettings settings = null)
        {
            settings = settings ?? new RaycastSettings();
            var result = new RaycastResult();

            // Ajuste l'origine et la direction avec les offsets
            Vector3 startPoint = origin + direction.normalized * settings.rayStartOffset;
            float adjustedDistance = settings.maxDistance + settings.rayStartOffset + settings.rayEndOffset;

            // Prépare les paramètres de hit
            RaycastHit hit;
            bool hasHit = false;

            // Choix du type de cast
            if (settings.sphereCast)
            {
                hasHit = Physics.SphereCast(startPoint, settings.sphereRadius, direction, out hit, adjustedDistance, 
                    settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }
            else if (settings.boxCast)
            {
                hasHit = Physics.BoxCast(startPoint, settings.boxHalfExtents, direction, out hit, Quaternion.identity, 
                    adjustedDistance, settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }
            else if (settings.capsuleCast)
            {
                Vector3 point1 = startPoint;
                Vector3 point2 = startPoint + Vector3.up * settings.capsuleHeight;
                hasHit = Physics.CapsuleCast(point1, point2, settings.capsuleRadius, direction, out hit, 
                    adjustedDistance, settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }
            else
            {
                hasHit = Physics.Raycast(startPoint, direction, out hit, adjustedDistance, 
                    settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }

            // Si pas de hit ou hit ignoré
            if (!hasHit || (settings.ignoreOwnColliders && hit.collider != null && 
                hit.collider.gameObject.GetInstanceID() == origin.GetHashCode()))
            {
                return RaycastResult.none;
            }

            // Remplit le résultat
            result.hasHit = true;
            result.hitPoint = hit.point;
            result.hitNormal = hit.normal;
            result.hitDistance = hit.distance - settings.rayStartOffset;
            result.hitObject = hit.collider.gameObject;
            result.hitCollider = hit.collider;
            result.hitMaterial = hit.collider.sharedMaterial;
            result.hitTag = hit.collider.tag;
            result.hitTextureCoord = hit.textureCoord;
            result.surfaceSlope = Vector3.Angle(hit.normal, Vector3.up);

            if (hit.collider is MeshCollider meshCollider)
            {
                result.surfaceArea = BSMDebug.CalculateSurfaceArea(meshCollider, hit.triangleIndex);
            }

            // Debug
            if (settings.debugDraw)
            {
                Color hitColor = result.hasHit ? settings.debugColor : Color.red;
                Debug.DrawLine(startPoint, result.hasHit ? result.hitPoint : startPoint + direction * adjustedDistance, 
                    hitColor, settings.debugDrawDuration);
                
                if (result.hasHit)
                {
                    Debug.DrawLine(result.hitPoint, result.hitPoint + result.hitNormal * 0.5f, 
                        Color.blue, settings.debugDrawDuration);
                }
            }

            return result;
        }

        // Lance un rayon et retourne tous les hits triés par distance
        public static List<RaycastResult> CastAll(Vector3 origin, Vector3 direction, RaycastSettings settings = null)
        {
            settings = settings ?? new RaycastSettings();
            var results = new List<RaycastResult>();

            // Ajuste l'origine et la direction
            Vector3 startPoint = origin + direction.normalized * settings.rayStartOffset;
            float adjustedDistance = settings.maxDistance + settings.rayStartOffset + settings.rayEndOffset;

            // Récupère tous les hits
            RaycastHit[] hits;
            if (settings.sphereCast)
            {
                hits = Physics.SphereCastAll(startPoint, settings.sphereRadius, direction, adjustedDistance, 
                    settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }
            else if (settings.boxCast)
            {
                hits = Physics.BoxCastAll(startPoint, settings.boxHalfExtents, direction, Quaternion.identity, 
                    adjustedDistance, settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }
            else
            {
                hits = Physics.RaycastAll(startPoint, direction, adjustedDistance, 
                    settings.layerMask, settings.ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);
            }

            // Convertit les hits en résultats
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                // Vérifie si le hit doit être ignoré
                if (settings.ignoreOwnColliders && hit.collider != null && 
                    hit.collider.gameObject.GetInstanceID() == origin.GetHashCode())
                    continue;

                var result = new RaycastResult
                {
                    hasHit = true,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    hitDistance = hit.distance - settings.rayStartOffset,
                    hitObject = hit.collider.gameObject,
                    hitCollider = hit.collider,
                    hitMaterial = hit.collider.sharedMaterial,
                    hitTag = hit.collider.tag,
                    hitTextureCoord = hit.textureCoord,
                    surfaceSlope = Vector3.Angle(hit.normal, Vector3.up)
                };

                if (hit.collider is MeshCollider meshCollider)
                {
                    result.surfaceArea = BSMDebug.CalculateSurfaceArea(meshCollider, hit.triangleIndex);
                }

                results.Add(result);
            }

            // Debug
            if (settings.debugDraw && results.Any())
            {
                for (int i = 0; i < results.Count; i++)
                {
                    Color hitColor = Color.Lerp(settings.debugColor, Color.red, (float)i / results.Count);
                    Debug.DrawLine(i == 0 ? startPoint : results[i - 1].hitPoint, 
                        results[i].hitPoint, hitColor, settings.debugDrawDuration);
                }
            }

            return results;
        }

        // Cast avec validation de surface
        public static RaycastResult ValidatedCast(Vector3 origin, Vector3 direction, 
            RaycastSettings raySettings = null, SurfaceSettings surfaceSettings = null)
        {
            raySettings = raySettings ?? new RaycastSettings();
            surfaceSettings = surfaceSettings ?? new SurfaceSettings();

            var result = Cast(origin, direction, raySettings);
            if (!result.hasHit) return result;

            // Validation de la surface
            result.isValidSurface = true;

            // Vérifie l'angle
            if (result.surfaceSlope > surfaceSettings.maxSlopeAngle)
            {
                result.isValidSurface = false;
                return result;
            }

            // Vérifie la surface minimale
            if (result.surfaceArea < surfaceSettings.minArea)
            {
                result.isValidSurface = false;
                return result;
            }

            // Vérifie le type de collider
            if (surfaceSettings.requireMeshCollider && !(result.hitCollider is MeshCollider))
            {
                result.isValidSurface = false;
                return result;
            }

            // Vérifie les matériaux
            if (surfaceSettings.validMaterials != null && surfaceSettings.validMaterials.Length > 0)
            {
                if (!surfaceSettings.validMaterials.Contains(result.hitMaterial))
                {
                    result.isValidSurface = false;
                    return result;
                }
            }

            // Vérifie les tags
            if (surfaceSettings.validTags != null && surfaceSettings.validTags.Length > 0)
            {
                if (!surfaceSettings.validTags.Contains(result.hitTag))
                {
                    result.isValidSurface = false;
                    return result;
                }
            }

            return result;
        }


        // Cast à partir des bounds d'un objet
        public static RaycastResult CastFromBounds(GameObject obj, Vector3 direction, RaycastSettings settings = null)
        {
            settings = settings ?? new RaycastSettings();
            
            Bounds bounds = BSMSnap.GetObjectBounds(obj);
            Vector3 origin = new Vector3(
                obj.transform.position.x,
                bounds.max.y,
                obj.transform.position.z
            );

            return Cast(origin, direction, settings);
        }
        #endregion

        #region Legacy Support
        public static bool ProcessWithRaycast(GameObject obj, Transform transform, Bounds bounds, 
            float raycastOffset, int floorLayer, float collisionPrecision, bool useBaseForCollision)
        {
            var settings = new RaycastSettings
            {
                layerMask = 1 << floorLayer,
                rayStartOffset = raycastOffset,
                debugDraw = true
            };

            var result = CastFromBounds(obj, Vector3.down, settings);
            if (!result.hasHit || !result.isValidSurface) return false;

            // Applique la position
            float heightOffset = useBaseForCollision ? 
                (bounds.min.y - transform.position.y) : 
                (bounds.center.y - transform.position.y);

            Vector3 newPosition = new Vector3(
                transform.position.x,
                result.hitPoint.y - heightOffset + collisionPrecision,
                transform.position.z
            );

            transform.position = newPosition;
            return true;
        }
        #endregion
    }
}