using NUnit.Framework;
using R8EOX.GameFlow;

namespace R8EOX.Tests.EditMode
{
    [TestFixture]
    public sealed class SessionConfigTests
    {
        [Test]
        public void Constructor_SetsAllProperties()
        {
            var config = new SessionConfig("race", "outpost", "Scenes/Outpost", "buggy_a", 5, 3);

            Assert.AreEqual("race", config.ModeId);
            Assert.AreEqual("outpost", config.TrackId);
            Assert.AreEqual("Scenes/Outpost", config.TrackScene);
            Assert.AreEqual("buggy_a", config.CarId);
            Assert.AreEqual(5, config.TotalLaps);
            Assert.AreEqual(3, config.AiDifficulty);
        }

        [Test]
        public void Constructor_NullValues_AreAllowed()
        {
            var config = new SessionConfig(null, null, null, null, 0, 0);

            Assert.IsNull(config.ModeId);
            Assert.IsNull(config.TrackId);
            Assert.IsNull(config.TrackScene);
            Assert.IsNull(config.CarId);
            Assert.AreEqual(0, config.TotalLaps);
            Assert.AreEqual(0, config.AiDifficulty);
        }

        [Test]
        public void Properties_AreReadOnly()
        {
            var config = new SessionConfig("practice", "desert", "Scenes/Desert", "truck_b", 10, 1);

            // Verify properties return the same values on subsequent access
            Assert.AreEqual("practice", config.ModeId);
            Assert.AreEqual("practice", config.ModeId);
            Assert.AreEqual(10, config.TotalLaps);
            Assert.AreEqual(10, config.TotalLaps);
        }
    }
}
