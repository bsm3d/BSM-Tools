/* 
* BSM Physics
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Advanced physics simulation
* - Kinematic object positioning
* - Surface validation techniques
* - Iterative physics casting
* - Detailed simulation results
* - Customizable physics settings
* - Raycast-based movement
* - Collision detection
* - Surface analysis
* - Configurable physics parameters
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using UnityEditor;

namespace BSMTools
{
    public class BSMPhysics
    {
        #region Structures
        public struct PhysicsSettings
        {
            public float maxStepSize;           // Taille maximale du pas
            public int maxIterations;           // Nombre maximum d'itérations
            public float maxDistance;           // Distance maximale de détection
            public LayerMask layerMask;         // Layers à considérer
            public float solverIterations;      // Nombre d'itérations du solveur physique
            public bool useGravity;             // Utiliser la gravité
            public CollisionDetectionMode collisionMode; // Mode de détection des collisions
            public bool debug;                  // Afficher les debug visuals
            public float timeout;               // Temps maximum de simulation

            public BSMRaycasting.SurfaceSettings surfaceSettings; // Paramètres de validation de surface

            public static PhysicsSettings Default => new PhysicsSettings
            {
                maxStepSize = 0.1f,
                maxIterations = 100,
                maxDistance = 100f,
                layerMask = -1,
                solverIterations = 25,
                useGravity = false,
                collisionMode = CollisionDetectionMode.Continuous,
                debug = false,
                timeout = 5f,
                surfaceSettings = BSMRaycasting.SurfaceSettings.Default
            };
        }

        public struct SimulationResult
        {
            public bool success;                // Si la simulation a réussi
            public Vector3 finalPosition;       // Position finale
            public Vector3 hitNormal;           // Normale au point d'impact
            public float distance;              // Distance parcourue
            public int iterationsUsed;          // Nombre d'itérations utilisées
            public float timeUsed;              // Temps de simulation utilisé
            public GameObject hitObject;        // Objet touché
            
            // Informations détaillées sur la surface
            public bool isValidSurface;         // La surface est-elle valide selon les critères
            public float surfaceSlope;          // Angle de la pente
            public float surfaceArea;           // Aire de la surface
            public PhysicsMaterial surfaceMaterial; // Matériau de la surface
        }
        #endregion

        #region Public Methods
        public static SimulationResult SimulatePhysics(GameObject obj, PhysicsSettings settings = default)
        {
            if (settings.Equals(default(PhysicsSettings)))
                settings = PhysicsSettings.Default;

            var result = new SimulationResult
            {
                success = false,
                finalPosition = obj.transform.position,
                iterationsUsed = 0,
                timeUsed = 0f,
                isValidSurface = false
            };

            bool wasKinematic = false;
            bool hadRigidbody = false;
            Rigidbody rb = null;
            Vector3 originalPosition = obj.transform.position;
            float startTime = Time.realtimeSinceStartup;

            try
            {
                // Configuration du Rigidbody
                if (obj.TryGetComponent(out rb))
                {
                    hadRigidbody = true;
                    wasKinematic = rb.isKinematic;
                }
                else
                {
                    rb = obj.AddComponent<Rigidbody>();
                }

                ConfigureRigidbody(rb, settings);
                int defaultSolverIterations = Physics.defaultSolverIterations;
                Physics.defaultSolverIterations = (int)settings.solverIterations;

                // Simulation
                int safetyCounter = 0;

                while (safetyCounter < settings.maxIterations)
                {
                    if (Time.realtimeSinceStartup - startTime > settings.timeout)
                    {
                        result.success = false;
                        result.timeUsed = settings.timeout;
                        return result;
                    }

                    // Utiliser ValidatedCast pour une validation de surface plus robuste
                    var raySettings = new BSMRaycasting.RaycastSettings
                    {
                        layerMask = settings.layerMask,
                        maxDistance = settings.maxDistance,
                        rayStartOffset = settings.maxStepSize,
                        ignoreTriggers = true
                    };

                    var raycastResult = BSMRaycasting.ValidatedCast(
                        obj.transform.position + Vector3.up * settings.maxStepSize, 
                        Vector3.down, 
                        raySettings, 
                        settings.surfaceSettings
                    );

                    if (raycastResult.hasHit)
                    {
                        if (raycastResult.hitObject != null && 
                            raycastResult.hitObject != obj && 
                            !raycastResult.hitObject.transform.IsChildOf(obj.transform))
                        {
                            result.success = true;
                            result.finalPosition = raycastResult.hitPoint;
                            result.hitNormal = raycastResult.hitNormal;
                            result.distance = Vector3.Distance(originalPosition, raycastResult.hitPoint);
                            result.hitObject = raycastResult.hitObject;
                            
                            // Informations détaillées sur la surface
                            result.isValidSurface = raycastResult.isValidSurface;
                            result.surfaceSlope = raycastResult.surfaceSlope;
                            result.surfaceArea = raycastResult.surfaceArea;
                            result.surfaceMaterial = raycastResult.hitMaterial;
                            break;
                        }
                    }

                    obj.transform.position += Vector3.down * settings.maxStepSize;
                    safetyCounter++;
                    result.iterationsUsed = safetyCounter;
                }

                result.timeUsed = Time.realtimeSinceStartup - startTime;
                Physics.defaultSolverIterations = defaultSolverIterations;

                if (!result.success)
                {
                    Debug.LogWarning($"PhysX: No valid surface found under {obj.name}");
                }

                return result;
            }
            finally
            {
                if (rb != null)
                {
                    if (!hadRigidbody)
                    {
                        Object.DestroyImmediate(rb);
                    }
                    else
                    {
                        RestoreRigidbody(rb, wasKinematic);
                    }
                }
            }
        }

        public static SimulationResult SimulateKinematic(GameObject obj, Vector3 direction, float distance, PhysicsSettings settings = default)
        {
            if (settings.Equals(default(PhysicsSettings)))
                settings = PhysicsSettings.Default;

            var result = new SimulationResult
            {
                success = false,
                finalPosition = obj.transform.position,
                iterationsUsed = 0,
                timeUsed = 0f,
                isValidSurface = false
            };

            bool wasKinematic = false;
            bool hadRigidbody = false;
            Rigidbody rb = null;
            float startTime = Time.realtimeSinceStartup;

            try
            {
                // Configuration du Rigidbody
                if (obj.TryGetComponent(out rb))
                {
                    hadRigidbody = true;
                    wasKinematic = rb.isKinematic;
                }
                else
                {
                    rb = obj.AddComponent<Rigidbody>();
                }

                rb.isKinematic = true;
                ConfigureRigidbody(rb, settings);

                // Simulation
                Vector3 targetPosition = obj.transform.position + direction.normalized * distance;
                Vector3 movement = direction.normalized * settings.maxStepSize;
                int steps = Mathf.CeilToInt(distance / settings.maxStepSize);
                steps = Mathf.Min(steps, settings.maxIterations);

                for (int i = 0; i < steps; i++)
                {
                    if (Time.realtimeSinceStartup - startTime > settings.timeout)
                    {
                        result.success = false;
                        result.timeUsed = settings.timeout;
                        return result;
                    }

                    rb.MovePosition(rb.position + movement);
                    result.iterationsUsed++;

                    var raySettings = new BSMRaycasting.RaycastSettings
                    {
                        layerMask = settings.layerMask,
                        maxDistance = settings.maxStepSize,
                        ignoreTriggers = true
                    };

                    var raycastResult = BSMRaycasting.ValidatedCast(
                        rb.position, 
                        direction.normalized, 
                        raySettings, 
                        settings.surfaceSettings
                    );

                    if (raycastResult.hasHit)
                    {
                        result.success = true;
                        result.finalPosition = raycastResult.hitPoint;
                        result.hitNormal = raycastResult.hitNormal;
                        result.distance = Vector3.Distance(obj.transform.position, raycastResult.hitPoint);
                        result.hitObject = raycastResult.hitObject;
                        
                        // Informations détaillées sur la surface
                        result.isValidSurface = raycastResult.isValidSurface;
                        result.surfaceSlope = raycastResult.surfaceSlope;
                        result.surfaceArea = raycastResult.surfaceArea;
                        result.surfaceMaterial = raycastResult.hitMaterial;
                        break;
                    }
                }

                result.timeUsed = Time.realtimeSinceStartup - startTime;
                return result;
            }
            finally
            {
                if (rb != null)
                {
                    if (!hadRigidbody)
                    {
                        Object.DestroyImmediate(rb);
                    }
                    else
                    {
                        RestoreRigidbody(rb, wasKinematic);
                    }
                }
            }
        }
        #endregion

        #region Legacy Support
        public static bool ProcessWithPhysX(GameObject obj, float maxStepSize, int maxIterations, float maxDistance, int floorLayer)
        {
            var settings = new PhysicsSettings
            {
                maxStepSize = maxStepSize,
                maxIterations = maxIterations,
                maxDistance = maxDistance,
                layerMask = 1 << floorLayer
            };

            var result = SimulatePhysics(obj, settings);
            return result.success;
        }
        #endregion

        #region Support Methods
        private static void ConfigureRigidbody(Rigidbody rb, PhysicsSettings settings)
        {
            rb.isKinematic = !settings.useGravity;
            rb.useGravity = settings.useGravity;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = settings.collisionMode;
        }

        private static void RestoreRigidbody(Rigidbody rb, bool wasKinematic)
        {
            rb.isKinematic = wasKinematic;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        private static bool CheckCollision(GameObject obj, LayerMask layerMask, out RaycastHit hitInfo)
        {
            var collider = obj.GetComponent<Collider>();
            hitInfo = new RaycastHit(); // Initialize the out parameter

            if (collider == null)
            {
                return false;
            }

            return Physics.ComputePenetration(
                collider, obj.transform.position, obj.transform.rotation,
                collider, obj.transform.position + Vector3.down * 0.1f, obj.transform.rotation,
                out Vector3 direction, out float distance
            );
        }
        #endregion
    }
}