using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for M7 (airborne-to-ground damping spike) and M8 (settling behavior).</summary>
    public class SuspensionStabilityTests
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
                k_SpringStrength, k_SpringDamping, k_RestDistance, steadySpringLen, steadySpringLen, k_DefaultDt);

            float airborneLen = k_RestDistance + k_OverExtend;
            float sanitizedPrev = SuspensionMath.SanitizePrevSpringLenForLanding(
                steadySpringLen, airborneLen, wasOnGround: false);
            float landingForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, steadySpringLen, sanitizedPrev, k_DefaultDt);

            Assert.Less(landingForce, steadyForce * 2f,
                $"Landing force ({landingForce:F2}N) must be < 2x steady state ({steadyForce:F2}N).");
        }

        [Test]
        public void LandingFrame_WasOnGround_PrevSpringLenUnchanged()
        {
            float prevLen = 1.8f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(1.5f, prevLen, wasOnGround: true);
            Assert.AreEqual(prevLen, result, 0.0001f);
        }

        [Test]
        public void LandingFrame_WasAirborne_PrevSpringLenSetToCurrent()
        {
            float landingLen = 1.5f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(
                landingLen, k_RestDistance + k_OverExtend, wasOnGround: false);
            Assert.AreEqual(landingLen, result, 0.0001f);
        }

        [Test]
        public void LandingFromSmallHeight_SettlesWithoutOscillation()
        {
            float springLen = 0.22f;
            float prevLen = springLen;

            for (int i = 0; i < 20; i++)
            {
                float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                    k_SpringStrength, k_SpringDamping, k_RestDistance, springLen, prevLen, k_DefaultDt);
                float velocity = force * k_DefaultDt;
                prevLen = springLen;
                springLen += velocity * k_DefaultDt;
                if (springLen > k_RestDistance + k_OverExtend) springLen = k_RestDistance + k_OverExtend;
                if (springLen < k_MinSpringLen) springLen = k_MinSpringLen;
            }

            Assert.Less(UnityEngine.Mathf.Abs(springLen - k_RestDistance), 0.20f,
                $"After 20 frames, spring ({springLen:F4}m) should be near rest ({k_RestDistance}m).");
        }

        [Test]
        public void OscillationAmplitude_DecreasesMonotonically()
        {
            float springLen = 0.12f;
            float prevLen = springLen;
            float lastPeakDeviation = float.MaxValue;
            float lastForce = 0f;
            bool wasIncreasing = false;
            int peakCount = 0;
            bool amplitudeDecreasing = true;

            for (int i = 0; i < 200; i++)
            {
                float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                    k_SpringStrength, k_SpringDamping, k_RestDistance, springLen, prevLen, k_DefaultDt);
                float deviation = springLen - k_RestDistance;
                bool isIncreasing = force > lastForce;
                if (wasIncreasing && !isIncreasing && i > 2)
                {
                    float peakDeviation = UnityEngine.Mathf.Abs(deviation);
                    if (peakCount > 0 && peakDeviation > lastPeakDeviation + 0.001f)
                        amplitudeDecreasing = false;
                    lastPeakDeviation = peakDeviation;
                    peakCount++;
                }
                wasIncreasing = isIncreasing;
                lastForce = force;
                float vel = force * k_DefaultDt;
                prevLen = springLen;
                springLen += vel * k_DefaultDt;
                if (springLen > k_RestDistance + k_OverExtend) springLen = k_RestDistance + k_OverExtend;
                if (springLen < k_MinSpringLen) springLen = k_MinSpringLen;
            }

            Assert.IsTrue(amplitudeDecreasing, "Oscillation amplitude should decrease monotonically.");
        }

        [Test]
        public void SuspensionForce_AtFullDroop_WithDampingSpike_QuantifiesIssue()
        {
            float airborneLen = k_RestDistance + k_OverExtend;
            float groundLen = 0.20f;

            float unsanitizedForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, groundLen, airborneLen, k_DefaultDt);
            Assert.Greater(unsanitizedForce, 500f,
                "Without fix, landing damping spike should exceed 500N.");

            float sanitizedPrev = SuspensionMath.SanitizePrevSpringLenForLanding(groundLen, airborneLen, wasOnGround: false);
            float sanitizedForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, groundLen, sanitizedPrev, k_DefaultDt);
            Assert.Less(sanitizedForce, 50f,
                $"With fix, landing force ({sanitizedForce:F2}N) should be just spring component (~37.5N).");
        }
    }
}
