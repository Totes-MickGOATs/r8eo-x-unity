using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for M7 (airborne-to-ground damping spike) and M8 (settling behavior).
    /// Validates that suspension forces remain stable during landing transitions
    /// and that oscillations decay monotonically over time.
    /// </summary>
    public class SuspensionStabilityTests
    {
        // ---- Constants matching production defaults (1/1 full scale) ----
        const float k_RestDistance = 0.80f;
        const float k_SpringStrength = 187.5f;
        const float k_SpringDamping = 10.625f;
        const float k_MaxSpringForce = 500f;
        const float k_DefaultDt = 0.008333f; // 120 Hz
        const float k_OverExtend = 0.32f;
        const float k_WheelRadius = 0.664f;
        const float k_MinSpringLen = 0.128f;


        // ---- M7: Airborne-to-ground damping spike ----

        [Test]
        public void LandingFrame_DampingForce_DoesNotExceedTwoTimesSteadyState()
        {
            // Steady state: spring at rest on flat ground, typical compression ~0.60m
            float steadySpringLen = 0.60f;
            float steadyForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, steadySpringLen, steadySpringLen, k_DefaultDt);

            // Simulate landing: prevSpringLen is full droop (airborne), current is ground contact
            float airborneLen = k_RestDistance + k_OverExtend; // 1.12m full droop
            float landingSpringLen = steadySpringLen;

            // WITHOUT the fix, this would produce a huge damping spike:
            // damping = 10.625 * (1.12 - 0.60) / 0.008333 = ~661 N
            // WITH the fix (SanitizePrevSpringLenForLanding), prevSpringLen = springLen,
            // so damping = 0 and force = spring component only.
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
            // When already on ground (wasOnGround=true), prev should pass through unchanged
            float prevLen = 0.72f;
            float curLen = 0.60f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(
                curLen, prevLen, wasOnGround: true);

            Assert.AreEqual(prevLen, result, 0.0001f,
                "When already grounded, prevSpringLen should not be modified.");
        }

        [Test]
        public void LandingFrame_WasAirborne_PrevSpringLenSetToCurrent()
        {
            // When transitioning from airborne (wasOnGround=false), prev should equal current
            // to zero out damping on the first ground frame
            float airborneLen = k_RestDistance + k_OverExtend;
            float landingLen = 0.60f;
            float result = SuspensionMath.SanitizePrevSpringLenForLanding(
                landingLen, airborneLen, wasOnGround: false);

            Assert.AreEqual(landingLen, result, 0.0001f,
                "On first ground frame after airborne, prevSpringLen should equal current springLen.");
        }

        [Test]
        public void LandingFromSmallHeight_SettlesWithoutOscillation()
        {
            // Simulate multiple frames after landing from a small height
            // The suspension should settle, not bounce
            float springLen = 0.48f; // Slightly compressed from landing
            float prevLen = springLen; // Sanitized by landing fix

            // Simulate 20 frames of suspension settling
            for (int i = 0; i < 20; i++)
            {
                float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                    k_SpringStrength, k_SpringDamping,
                    k_RestDistance, springLen, prevLen, k_DefaultDt);

                // Simple Euler integration: spring pushes chassis up, extending the spring
                // velocity = force / mass * dt (assume 1kg equivalent per wheel)
                float velocity = force * k_DefaultDt;
                prevLen = springLen;
                springLen += velocity * k_DefaultDt;

                // Clamp to valid range
                if (springLen > k_RestDistance + k_OverExtend)
                    springLen = k_RestDistance + k_OverExtend;
                if (springLen < k_MinSpringLen)
                    springLen = k_MinSpringLen;
            }

            // After 20 frames, spring should be near rest distance (not oscillating wildly)
            float finalDeviation = UnityEngine.Mathf.Abs(springLen - k_RestDistance);
            Assert.Less(finalDeviation, 0.20f,
                $"After 20 frames, spring length ({springLen:F4}m) should be near rest ({k_RestDistance}m). " +
                "Suspension did not settle after landing.");
        }


        // ---- M8: Verify settling behavior (oscillation decay) ----

        [Test]
        public void OscillationAmplitude_DecreasesMonotonically()
        {
            // Simulate suspension from a compressed state and verify oscillation decays
            float springLen = 0.40f; // Start compressed
            float prevLen = springLen;

            // Track peaks and troughs to measure amplitude
            float lastPeakDeviation = float.MaxValue;
            float lastForce = 0f;
            bool wasIncreasing = false;
            int peakCount = 0;
            bool amplitudeDecreasing = true;

            // Simulate 200 frames (~1.67 seconds at 120 Hz)
            for (int i = 0; i < 200; i++)
            {
                float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                    k_SpringStrength, k_SpringDamping,
                    k_RestDistance, springLen, prevLen, k_DefaultDt);

                float deviation = springLen - k_RestDistance;

                // Detect peaks (sign changes in velocity of deviation)
                bool isIncreasing = force > lastForce;
                if (wasIncreasing && !isIncreasing && i > 2)
                {
                    float peakDeviation = UnityEngine.Mathf.Abs(deviation);
                    if (peakCount > 0 && peakDeviation > lastPeakDeviation + 0.001f)
                    {
                        amplitudeDecreasing = false;
                    }
                    lastPeakDeviation = peakDeviation;
                    peakCount++;
                }
                wasIncreasing = isIncreasing;
                lastForce = force;

                // Simple Euler integration
                float velocity = force * k_DefaultDt;
                prevLen = springLen;
                springLen += velocity * k_DefaultDt;

                if (springLen > k_RestDistance + k_OverExtend)
                    springLen = k_RestDistance + k_OverExtend;
                if (springLen < k_MinSpringLen)
                    springLen = k_MinSpringLen;
            }

            Assert.IsTrue(amplitudeDecreasing,
                "Oscillation amplitude should decrease monotonically (damping ratio is adequate).");
        }

        [Test]
        public void SuspensionForce_AtFullDroop_WithDampingSpike_QuantifiesIssue()
        {
            // Document the exact magnitude of the M7 bug
            // Before fix: landing from airborne with prevSpringLen = 1.12m, springLen = 0.60m
            // produces damping = 10.625 * (1.12 - 0.60) / 0.008333 = ~661 N
            float airborneLen = k_RestDistance + k_OverExtend; // 1.12
            float groundLen = 0.60f;

            float unsanitizedForce = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, groundLen, airborneLen, k_DefaultDt);

            // The unsanitized force includes the damping spike
            // Spring: 187.5 * (0.80 - 0.60) = 37.5
            // Damping: 10.625 * (1.12 - 0.60) / 0.008333 = ~661
            // Total: ~699 N
            Assert.Greater(unsanitizedForce, 500f,
                "Without fix, landing damping spike should exceed 500N (confirming the bug exists).");

            // With sanitization, force should be just the spring component
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
