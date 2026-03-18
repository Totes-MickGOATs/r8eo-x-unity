using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for AirborneDetector frame-debounce logic.
    /// </summary>
    public class AirborneDetectorTests
    {
        [Test]
        public void Update_OnGround_ReturnsFalse()
        {
            var det = new AirborneDetector();
            bool result = det.Update(anyWheelOnGround: true);
            Assert.IsFalse(result);
        }

        [Test]
        public void Update_FourConsecutiveOffGround_ReturnsFalse()
        {
            var det = new AirborneDetector();
            bool result = false;
            for (int i = 0; i < 4; i++)
                result = det.Update(anyWheelOnGround: false);
            Assert.IsFalse(result, "Should not be airborne before threshold frame");
        }

        [Test]
        public void Update_FiveConsecutiveOffGround_ReturnsTrue()
        {
            var det = new AirborneDetector();
            bool result = false;
            for (int i = 0; i < 5; i++)
                result = det.Update(anyWheelOnGround: false);
            Assert.IsTrue(result, "Should be airborne at threshold");
        }

        [Test]
        public void Update_GroundContactAfterAirborne_ReturnsFalse()
        {
            var det = new AirborneDetector();
            for (int i = 0; i < 5; i++) det.Update(anyWheelOnGround: false);
            Assert.IsTrue(det.Update(anyWheelOnGround: false));

            bool grounded = det.Update(anyWheelOnGround: true);
            Assert.IsFalse(grounded, "Immediate ground contact should clear airborne");
        }

        [Test]
        public void Reset_AfterAirborne_ReturnsFalse()
        {
            var det = new AirborneDetector();
            for (int i = 0; i < 5; i++) det.Update(anyWheelOnGround: false);
            det.Reset();

            // Need 5 more frames to become airborne again
            bool result = false;
            for (int i = 0; i < 4; i++)
                result = det.Update(anyWheelOnGround: false);
            Assert.IsFalse(result, "Reset should restart the counter");
        }
    }
}
