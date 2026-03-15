using UnityEngine;

namespace R8EOX.Shared
{
    /// <summary>
    /// Runtime-safe unit conversion helpers shared between game code and editor tooling.
    /// No UnityEditor imports — safe to use at runtime.
    /// </summary>
    public static class UnitConversion
    {
        // Speed
        public static float MsToKmh(float ms) => ms * 3.6f;
        public static float KmhToMs(float kmh) => kmh / 3.6f;

        // Angle
        public static float RadToDeg(float rad) => rad * Mathf.Rad2Deg;
        public static float DegToRad(float deg) => deg * Mathf.Deg2Rad;

        // Spring rate
        public static float NmToNmm(float nm) => nm / 1000f;
        public static float NmmToNm(float nmm) => nmm * 1000f;

        // Force
        public static float NToKgf(float n) => n / 9.80665f;
        public static float KgfToN(float kgf) => kgf * 9.80665f;
    }
}
