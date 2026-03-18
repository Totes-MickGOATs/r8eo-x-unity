using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public class AirborneDetectorTests
    {
        [Test]
        public void Update_GroundedAlways_ReturnsFalse()
        {
            var det = new AirborneDetector(5);
            for (int i = 0; i < 10; i++)
                Assert.IsFalse(det.Update(false), $"Frame {i} should be grounded");
        }

        [Test]
        public void Update_AirborneBeforeThreshold_ReturnsFalse()
        {
            var det = new AirborneDetector(5);
            for (int i = 0; i < 4; i++)
                Assert.IsFalse(det.Update(true), $"Frame {i} should be below threshold");
        }

        [Test]
        public void Update_AirborneAtThreshold_ReturnsTrue()
        {
            var det = new AirborneDetector(5);
            for (int i = 0; i < 4; i++) det.Update(true);
            Assert.IsTrue(det.Update(true));
        }

        [Test]
        public void Update_GroundedAfterAirborne_ResetsToFalse()
        {
            var det = new AirborneDetector(5);
            for (int i = 0; i < 5; i++) det.Update(true);
            Assert.IsTrue(det.Update(true));
            Assert.IsFalse(det.Update(false));
        }

        [Test]
        public void Update_Threshold1_TrueOnFirstAirborneFrame()
        {
            var det = new AirborneDetector(1);
            Assert.IsTrue(det.Update(true));
        }
    }
}
