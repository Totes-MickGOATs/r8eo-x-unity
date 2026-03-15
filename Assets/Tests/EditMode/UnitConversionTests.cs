using NUnit.Framework;
using R8EOX.Shared;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitConversion helpers.
    /// Verifies round-trip fidelity and boundary values for all conversion pairs.
    /// </summary>
    public class UnitConversionTests
    {
        const float k_Tolerance = 0.0001f;

        // ---- Speed: m/s <-> km/h ----

        [Test]
        public void MsToKmh_KnownValue_ReturnsCorrectKmh()
        {
            // 10 m/s = 36 km/h
            Assert.AreEqual(36f, UnitConversion.MsToKmh(10f), k_Tolerance);
        }

        [Test]
        public void KmhToMs_KnownValue_ReturnsCorrectMs()
        {
            // 36 km/h = 10 m/s
            Assert.AreEqual(10f, UnitConversion.KmhToMs(36f), k_Tolerance);
        }

        [Test]
        public void MsToKmh_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, UnitConversion.MsToKmh(0f), k_Tolerance);
        }

        [Test]
        public void KmhToMs_RoundTrip_ReturnsOriginal()
        {
            float original = 27f;
            Assert.AreEqual(original, UnitConversion.KmhToMs(UnitConversion.MsToKmh(original)), k_Tolerance);
        }

        // ---- Angle: rad <-> deg ----

        [Test]
        public void RadToDeg_HalfPi_Returns90()
        {
            Assert.AreEqual(90f, UnitConversion.RadToDeg(UnityEngine.Mathf.PI * 0.5f), 0.001f);
        }

        [Test]
        public void DegToRad_90_ReturnsHalfPi()
        {
            Assert.AreEqual(UnityEngine.Mathf.PI * 0.5f, UnitConversion.DegToRad(90f), 0.001f);
        }

        [Test]
        public void RadToDeg_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, UnitConversion.RadToDeg(0f), k_Tolerance);
        }

        [Test]
        public void DegToRad_RoundTrip_ReturnsOriginal()
        {
            float original = 0.5f;
            Assert.AreEqual(original, UnitConversion.DegToRad(UnitConversion.RadToDeg(original)), k_Tolerance);
        }

        // ---- Spring rate: N/m <-> N/mm ----

        [Test]
        public void NmToNmm_KnownValue_ReturnsCorrect()
        {
            // 75 N/m = 0.075 N/mm
            Assert.AreEqual(0.075f, UnitConversion.NmToNmm(75f), k_Tolerance);
        }

        [Test]
        public void NmmToNm_KnownValue_ReturnsCorrect()
        {
            // 0.075 N/mm = 75 N/m
            Assert.AreEqual(75f, UnitConversion.NmmToNm(0.075f), k_Tolerance);
        }

        [Test]
        public void NmToNmm_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, UnitConversion.NmToNmm(0f), k_Tolerance);
        }

        [Test]
        public void NmmToNm_RoundTrip_ReturnsOriginal()
        {
            float original = 75f;
            Assert.AreEqual(original, UnitConversion.NmmToNm(UnitConversion.NmToNmm(original)), k_Tolerance);
        }

        // ---- Force: N <-> kgf ----

        [Test]
        public void NToKgf_KnownValue_ReturnsCorrect()
        {
            // 9.80665 N = 1 kgf
            Assert.AreEqual(1f, UnitConversion.NToKgf(9.80665f), k_Tolerance);
        }

        [Test]
        public void KgfToN_KnownValue_ReturnsCorrect()
        {
            // 1 kgf = 9.80665 N
            Assert.AreEqual(9.80665f, UnitConversion.KgfToN(1f), k_Tolerance);
        }

        [Test]
        public void NToKgf_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, UnitConversion.NToKgf(0f), k_Tolerance);
        }

        [Test]
        public void KgfToN_RoundTrip_ReturnsOriginal()
        {
            float original = 26f;
            Assert.AreEqual(original, UnitConversion.KgfToN(UnitConversion.NToKgf(original)), k_Tolerance);
        }
    }
}
