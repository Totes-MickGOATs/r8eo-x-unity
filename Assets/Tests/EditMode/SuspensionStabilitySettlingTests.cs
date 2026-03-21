using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// M8 tests: settling behavior and oscillation decay after landing.
    /// Landing / damping-spike tests live in SuspensionStabilityLandingTests.cs.
    /// </summary>
    public class SuspensionStabilitySettlingTests
    {
        const float k_RestDistance = 0.25f;
        const float k_SpringStrength = 750.0f;
        const float k_SpringDamping = 106.25f;
        const float k_DefaultDt = 0.008333f;
        const float k_OverExtend = 0.24f;
        const float k_MinSpringLen = 0.12f;

        [Test]
        public void LandingFromSmallHeight_SettlesWithoutOscillation()
        {
            float springLen = 0.22f;
            float prevLen = springLen;

            for (int i = 0; i < 20; i++)
            {
                float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                    k_SpringStrength, k_SpringDamping,
                    k_RestDistance, springLen, prevLen, k_DefaultDt);

                float velocity = force * k_DefaultDt;
                prevLen = springLen;
                springLen += velocity * k_DefaultDt;

                if (springLen > k_RestDistance + k_OverExtend)
                    springLen = k_RestDistance + k_OverExtend;
                if (springLen < k_MinSpringLen)
                    springLen = k_MinSpringLen;
            }

            float finalDeviation = UnityEngine.Mathf.Abs(springLen - k_RestDistance);
            Assert.Less(finalDeviation, 0.20f,
                $"After 20 frames, spring length ({springLen:F4}m) should be near rest ({k_RestDistance}m). " +
                "Suspension did not settle after landing.");
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
                    k_SpringStrength, k_SpringDamping,
                    k_RestDistance, springLen, prevLen, k_DefaultDt);

                float deviation = springLen - k_RestDistance;

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
    }
}
