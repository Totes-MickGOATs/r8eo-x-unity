using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// M7 tests: airborne-to-ground damping spike (landing frame behavior).
    /// Settling/oscillation tests live in SuspensionStabilitySettlingTests.cs.
    /// </summary>
    public class SuspensionStabilityLandingTests
    {
        const float k_RestDistance = 0.25f;
        const float k_SpringStrength = 750.0f;
        const float k_SpringDamping = 106.25f;
        const float k_DefaultDt = 0.008333f;
        const float k_OverExtend = 0.24f;
        const float k_MinSpringLen = 0.12f;

        [Test]
        public void LandingFrame_DampingForce_DoesNotExceedTwoTimesSteadyState()
        {
            float steadySpringLen = 0.20f;
            float steadyForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, steadySpringLen, steadySpringLen, k_DefaultDt);

            float airborneLen = k_RestDistance + k_OverExtend;
            float landingSpringLen = steadySpringLen;

            float sanitizedPrev = SuspensionMath.SanitizePrevSpringLenForLanding(
                landingSpringLen, airborneLen, wasOnGround: false);
            float landingForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, landingSpringLen, sanitizedPrev, k_DefaultDt);

            Assert.Less(landingForce, steadyForce * 2f,
                $"Landing force ({landingForce:F2}N) must be < 2x steady state ({steadyForce:F2}N). " +
                "Damping spike on airborne-to-ground transition was not suppressed.");
        }

        [Test]
        public void LandingFrame_WasOnGround_PrevSpringLenUnchanged()
        {
            float prevLen = 1.8f;
            float curLen = 1.5f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(
                curLen, prevLen, wasOnGround: true);

            Assert.AreEqual(prevLen, result, 0.0001f,
                "When already grounded, prevSpringLen should not be modified.");
        }

        [Test]
        public void LandingFrame_WasAirborne_PrevSpringLenSetToCurrent()
        {
            float airborneLen = k_RestDistance + k_OverExtend;
            float landingLen = 1.5f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(
                landingLen, airborneLen, wasOnGround: false);

            Assert.AreEqual(landingLen, result, 0.0001f,
                "On first ground frame after airborne, prevSpringLen should equal current springLen.");
        }

        [Test]
        public void SuspensionForce_AtFullDroop_WithDampingSpike_QuantifiesIssue()
        {
            float airborneLen = k_RestDistance + k_OverExtend;
            float groundLen = 0.20f;

            float unsanitizedForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, groundLen, airborneLen, k_DefaultDt);

            Assert.Greater(unsanitizedForce, 500f,
                "Without fix, landing damping spike should exceed 500N (confirming the bug exists).");

            float sanitizedPrev = SuspensionMath.SanitizePrevSpringLenForLanding(
                groundLen, airborneLen, wasOnGround: false);
            float sanitizedForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, groundLen, sanitizedPrev, k_DefaultDt);

            Assert.Less(sanitizedForce, 50f,
                $"With fix, landing force ({sanitizedForce:F2}N) should be just spring component (~37.5N).");
        }
    }
}
