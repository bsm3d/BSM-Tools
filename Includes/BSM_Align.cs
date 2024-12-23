/* 
* BSM Align
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Object alignment to surfaces
* - Normal and target-based rotation
* - Multi-object alignment support
* - Flexible rotation settings
* - Raycast alignment techniques
* - Customizable transformation rules
* - Preservation of object properties
* - Random rotation generation
* - Advanced transformation blending
* - Layer and distance-aware positioning
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
    public class BSMAlign
    {
        #region Structures
        public struct AlignSettings
        {
            public bool preserveScale;        // Garde l'échelle d'origine
            public bool preservePosition;     // Garde la position d'origine
            public bool preserveRotation;     // Garde des axes de rotation spécifiques
            public Vector3 lockedAxes;        // Axes de rotation à verrouiller (1 = verrouillé, 0 = libre)
            public float blendWeight;         // Pour les alignements progressifs (0-1)
            public float maxAngle;            // Angle maximum d'alignement
            public Vector3 upVector;          // Vecteur haut personnalisé
            public LayerMask raycastMask;     // Layers pour les raycasts
            public float raycastDistance;     // Distance maximale des raycasts

            public static AlignSettings Default => new AlignSettings
            {
                preserveScale = true,
                preservePosition = false,
                preserveRotation = false,
                lockedAxes = Vector3.zero,
                blendWeight = 1f,
                maxAngle = 180f,
                upVector = Vector3.up,
                raycastMask = -1,
                raycastDistance = 100f
            };
        }

        public struct AlignResult
        {
            public bool success;           // Si l'alignement a réussi
            public Quaternion rotation;    // Rotation finale
            public Vector3 position;       // Position finale (si modifiée)
            public float angle;            // Angle d'alignement appliqué
            public Vector3 alignAxis;      // Axe d'alignement utilisé
        }
        #endregion

        #region Public Methods
        /// Aligne un objet sur une normale
        public static AlignResult AlignToNormal(Transform transform, Vector3 normal, AlignSettings settings = default)
        {
            if (settings.Equals(default(AlignSettings)))
                settings = AlignSettings.Default;

            var result = new AlignResult
            {
                success = true,
                position = transform.position,
                alignAxis = normal
            };

            Vector3 currentUp = settings.preserveRotation ? GetLockedRotation(transform.up, settings.lockedAxes) : settings.upVector;
            
            // Calcul de la rotation
            float angle = Vector3.Angle(currentUp, normal);
            if (angle > settings.maxAngle)
            {
                result.success = false;
                result.angle = angle;
                return result;
            }

            Quaternion targetRotation = Quaternion.FromToRotation(currentUp, normal);
            result.rotation = Quaternion.Lerp(transform.rotation, targetRotation * transform.rotation, settings.blendWeight);
            result.angle = angle;

            // Application si demandé
            if (!settings.preserveRotation)
            {
                transform.rotation = result.rotation;
            }

            return result;
        }

        /// Aligne un objet sur une surface en utilisant un raycast
        public static AlignResult AlignToSurface(Transform transform, AlignSettings settings = default)
        {
            if (settings.Equals(default(AlignSettings)))
                settings = AlignSettings.Default;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, -settings.upVector, out hit, settings.raycastDistance, settings.raycastMask))
            {
                return AlignToNormal(transform, hit.normal, settings);
            }

            return new AlignResult { success = false, rotation = transform.rotation, position = transform.position };
        }

        /// Aligne un objet sur un autre objet
        public static AlignResult AlignToTarget(Transform transform, Transform target, AlignSettings settings = default)
        {
            if (settings.Equals(default(AlignSettings)))
                settings = AlignSettings.Default;

            var result = new AlignResult
            {
                success = true,
                position = settings.preservePosition ? transform.position : target.position,
                rotation = CalculateTargetRotation(transform, target, settings)
            };

            // Vérification de l'angle maximum
            float angle = Quaternion.Angle(transform.rotation, result.rotation);
            if (angle > settings.maxAngle)
            {
                result.success = false;
                result.angle = angle;
                return result;
            }

            // Application de la rotation avec blend
            result.rotation = Quaternion.Lerp(transform.rotation, result.rotation, settings.blendWeight);
            result.angle = angle;

            if (!settings.preserveRotation)
            {
                transform.rotation = result.rotation;
            }

            if (!settings.preservePosition)
            {
                transform.position = result.position;
            }

            return result;
        }

        /// Aligne plusieurs objets entre eux
        public static AlignResult AlignMultiple(IEnumerable<Transform> transforms, AlignSettings settings = default)
        {
            if (settings.Equals(default(AlignSettings)))
                settings = AlignSettings.Default;

            if (!transforms.Any())
                return new AlignResult { success = false };

            // Calcul de la moyenne des rotations
            Vector3 averageUp = Vector3.zero;
            Vector3 averageForward = Vector3.zero;
            Vector3 averagePosition = Vector3.zero;
            int count = 0;

            foreach (var t in transforms)
            {
                if (t == null) continue;
                averageUp += t.up;
                averageForward += t.forward;
                averagePosition += t.position;
                count++;
            }

            if (count == 0)
                return new AlignResult { success = false };

            averageUp /= count;
            averageForward /= count;
            averagePosition /= count;

            // Création de la rotation moyenne
            Quaternion averageRotation = Quaternion.LookRotation(averageForward, averageUp);

            var result = new AlignResult
            {
                success = true,
                rotation = averageRotation,
                position = averagePosition,
                alignAxis = averageUp
            };

            // Application aux objets
            foreach (var t in transforms)
            {
                if (t == null) continue;

                if (!settings.preservePosition)
                    t.position = Vector3.Lerp(t.position, averagePosition, settings.blendWeight);

                if (!settings.preserveRotation)
                    t.rotation = Quaternion.Lerp(t.rotation, averageRotation, settings.blendWeight);
            }

            return result;
        }

        /// Applique une rotation aléatoire autour d'un axe
        public static AlignResult RandomRotation(Transform transform, Vector3 axis, Vector2 angleRange, AlignSettings settings = default)
        {
            if (settings.Equals(default(AlignSettings)))
                settings = AlignSettings.Default;

            float randomAngle = Random.Range(angleRange.x, angleRange.y);
            Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, axis);

            var result = new AlignResult
            {
                success = true,
                rotation = randomRotation * transform.rotation,
                position = transform.position,
                angle = randomAngle,
                alignAxis = axis
            };

            if (!settings.preserveRotation)
            {
                transform.rotation = result.rotation;
            }

            return result;
        }
        #endregion

        #region Legacy Support Methods
        // Méthode de compatibilité pour BSM_Drop
        public static void ApplyTransformations(GameObject obj, bool alignToNormal, bool alignOnY, bool useRandomRotation, Vector2 rotationRange, int layerMask)
        {
            var settings = new AlignSettings
            {
                preservePosition = true,
                preserveScale = true,
                lockedAxes = alignOnY ? new Vector3(0, 1, 0) : Vector3.zero,
                raycastMask = layerMask,
                upVector = Vector3.up
            };

            if (alignToNormal)
            {
                AlignToSurface(obj.transform, settings);
            }

            if (useRandomRotation)
            {
                RandomRotation(obj.transform, Vector3.up, rotationRange);
            }
        }
        #endregion

        #region Support Methods
        private static Vector3 GetLockedRotation(Vector3 direction, Vector3 lockedAxes)
        {
            return new Vector3(
                lockedAxes.x > 0 ? 0 : direction.x,
                lockedAxes.y > 0 ? 0 : direction.y,
                lockedAxes.z > 0 ? 0 : direction.z
            ).normalized;
        }

        private static Quaternion CalculateTargetRotation(Transform source, Transform target, AlignSettings settings)
        {
            if (settings.preserveRotation)
                return source.rotation;

            Quaternion baseRotation = target.rotation;
            
            if (settings.lockedAxes != Vector3.zero)
            {
                Vector3 sourceUp = GetLockedRotation(source.up, settings.lockedAxes);
                Vector3 targetUp = GetLockedRotation(target.up, settings.lockedAxes);
                return Quaternion.FromToRotation(sourceUp, targetUp) * source.rotation;
            }

            return baseRotation;
        }
        #endregion
    }
}