using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// EditMode tests validating that RaycastWheel uses a SphereCast with a well-typed
    /// radius constant for terrain anti-snag.
    ///
    /// The constant k_SphereCastRadius is exposed via a public static property
    /// RaycastWheel.SphereCastRadius for testability without reflection.
    ///
    /// Reference: feat/terrain-anti-snag — SphereCast replaces Raycast in RaycastWheel
    /// </summary>
    public class SphereCastTests
    {
        // ---- Expected value range for full-scale tire contact patch ----

        /// <summary>Minimum acceptable sphere cast radius (m).</summary>
        const float k_MinRadius = 0.10f;
        /// <summary>Maximum acceptable sphere cast radius (m).</summary>
        const float k_MaxRadius = 0.20f;

        [Test]
        public void RaycastWheel_SphereCastRadius_IsAccessibleAsPublicProperty()
        {
            // This test will fail to compile if RaycastWheel does not expose
            // a public static property named SphereCastRadius.
            // That compile failure is the RED state for this test.
            float radius = RaycastWheel.SphereCastRadius;

            // Value must be within the expected range for a full-scale tire contact patch
            Assert.GreaterOrEqual(radius, k_MinRadius,
                $"SphereCastRadius ({radius}m) must be >= {k_MinRadius}m " +
                "(too small: would behave like a point raycast, no snag benefit).");

            Assert.LessOrEqual(radius, k_MaxRadius,
                $"SphereCastRadius ({radius}m) must be <= {k_MaxRadius}m " +
                "(too large: would incorrectly contact geometry above the wheel).");
        }

        [Test]
        public void RaycastWheel_SphereCastRadius_IsPositive()
        {
            float radius = RaycastWheel.SphereCastRadius;

            Assert.Greater(radius, 0f,
                "SphereCastRadius must be positive. Zero or negative values disable the anti-snag effect.");
        }
    }
}
