/* 
* BSM Math
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Unit conversions
* - Area and volume calculations
* - Advanced interpolation techniques
* - Geometric distance computations
* - Rotation and transformation utilities
* - Statistical probability functions
* - Easing curve implementations
* - Random generation methods
* - Precision mapping algorithms
* - Comprehensive mathematical utilities
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using System;

namespace BSMTools
{
    public static class BSMMath
    {

        #region Conversion et Unités
        public static float DegreesToRadians(float degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        public static float RadiansToDegrees(float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        public static float KilometersPerHourToMetersPerSecond(float kmh)
        {
            return kmh * 1000f / 3600f;
        }

        public static float MetersPerSecondToKilometersPerHour(float ms)
        {
            return ms * 3600f / 1000f;
        }

        public static float MeterToInches(float meters)
        {
            return meters * 39.3701f;
        }

        public static float InchesToMeter(float inches)
        {
            return inches / 39.3701f;
        }

        public static float CelsiusToFahrenheit(float celsius)
        {
            return (celsius * 9f / 5f) + 32f;
        }

        public static float FahrenheitToCelsius(float fahrenheit)
        {
            return (fahrenheit - 32f) * 5f / 9f;
        }
        #endregion

        #region Calculs de Surface et Volume
        public static float CalculateRectangleArea(float length, float width)
        {
            return length * width;
        }

        public static float CalculateCircleArea(float radius)
        {
            return Mathf.PI * radius * radius;
        }

        public static float CalculateTriangleArea(float baseLength, float height)
        {
            return 0.5f * baseLength * height;
        }

        public static float CalculateCubeVolume(float sideLength)
        {
            return sideLength * sideLength * sideLength;
        }

        public static float CalculateCylinderVolume(float radius, float height)
        {
            return Mathf.PI * radius * radius * height;
        }

        public static float CalculateSphereVolume(float radius)
        {
            return (4f / 3f) * Mathf.PI * radius * radius * radius;
        }

        public static float SquareMetersToSquareFeet(float squareMeters)
        {
            return squareMeters * 10.7639f;
        }

        public static float SquareFeetToSquareMeters(float squareFeet)
        {
            return squareFeet / 10.7639f;
        }

        public static float CubicMetersToGallons(float cubicMeters)
        {
            return cubicMeters * 264.172f;
        }

        public static float GallonsToCubicMeters(float gallons)
        {
            return gallons / 264.172f;
        }
        #endregion

        #region Interpolation et Calculs
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }

        public static Vector3 SLerp(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Slerp(a, b, Mathf.Clamp01(t));
        }

        public static float CalculateProgress(float current, float min, float max)
        {
            return Mathf.Clamp01((current - min) / (max - min));
        }

        public static float Map(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float fromRange = fromMax - fromMin;
            float toRange = toMax - toMin;
            float scaledValue = (value - fromMin) / fromRange;
            return toMin + (scaledValue * toRange);
        }
        #endregion

        #region Géométrie et Positions
        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        public static bool IsInViewAngle(Vector3 forward, Vector3 direction, float maxAngle)
        {
            float angle = Vector3.Angle(forward, direction);
            return angle <= maxAngle;
        }

        public static Vector3 CalculateCenterPoint(params Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (Vector3 point in points)
            {
                center += point;
            }
            return center / points.Length;
        }

        public static float PointToLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            Vector3 pointToStart = point - lineStart;
            float t = Vector3.Dot(pointToStart, lineDirection) / lineDirection.sqrMagnitude;
            Vector3 closestPoint = lineStart + (t * lineDirection);
            return Vector3.Distance(point, closestPoint);
        }
        #endregion

        #region Transformations et Rotations
        public static Vector3 ClampMagnitude(Vector3 vector, float maxMagnitude)
        {
            return Vector3.ClampMagnitude(vector, maxMagnitude);
        }

        public static Quaternion LookRotation(Vector3 direction, Vector3 upwards = default)
        {
            return upwards == default 
                ? Quaternion.LookRotation(direction) 
                : Quaternion.LookRotation(direction, upwards);
        }

        public static Quaternion LerpRotation(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.Lerp(a, b, Mathf.Clamp01(t));
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            Vector3 dir = point - pivot;
            dir = Quaternion.Euler(angles) * dir;
            return dir + pivot;
        }
        #endregion

        #region Statistiques et Probabilités
        public static float RandomRange(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public static int RandomRange(int min, int max)
        {
            return UnityEngine.Random.Range(min, max + 1);
        }

        public static bool RandomChance(float probabilityPercent)
        {
            return UnityEngine.Random.value <= (probabilityPercent / 100f);
        }

        public static float Round(float value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        public static float Gaussian(float mean, float stdDev)
        {
            float u1 = 1f - UnityEngine.Random.value;
            float u2 = 1f - UnityEngine.Random.value;
            float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * 
                Mathf.Sin(2f * Mathf.PI * u2);
            return mean + (stdDev * randStdNormal);
        }
        #endregion

        #region Validation et Comparaison
        public static bool Approximately(float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        public static bool IsBetween(float value, float min, float max, bool inclusive = true)
        {
            return inclusive 
                ? value >= min && value <= max 
                : value > min && value < max;
        }
        #endregion

        #region Courbes et Easing
        public static float EaseInQuad(float t)
        {
            return t * t;
        }

        public static float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }

        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f 
                ? 2 * t * t 
                : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        public static float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }
        #endregion
    }
}