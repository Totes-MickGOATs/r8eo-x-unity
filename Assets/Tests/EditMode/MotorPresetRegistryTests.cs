using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public class MotorPresetRegistryTests
    {
        const float k_Epsilon = 0.001f;

        [Test]
        public void Get_Custom_ReturnsNull()
        {
            var result = MotorPresetRegistry.Get(RCCar.MotorPreset.Custom);
            Assert.IsNull(result);
        }

        [Test]
        public void Get_Motor21_5T_ReturnsCorrectEngineForce()
        {
            var result = MotorPresetRegistry.Get(RCCar.MotorPreset.Motor21_5T);
            Assert.IsNotNull(result);
            Assert.AreEqual(155f, result.Value.EngineForceMax, k_Epsilon);
        }

        [Test]
        public void Get_Motor13_5T_ReturnsCorrectMaxSpeed()
        {
            var result = MotorPresetRegistry.Get(RCCar.MotorPreset.Motor13_5T);
            Assert.IsNotNull(result);
            Assert.AreEqual(27f, result.Value.MaxSpeed, k_Epsilon);
        }

        [Test]
        public void Get_AllNonCustomPresets_ReturnNonNull()
        {
            var presets = new[]
            {
                RCCar.MotorPreset.Motor21_5T, RCCar.MotorPreset.Motor17_5T, RCCar.MotorPreset.Motor13_5T,
                RCCar.MotorPreset.Motor9_5T,  RCCar.MotorPreset.Motor5_5T,  RCCar.MotorPreset.Motor1_5T
            };
            foreach (var p in presets)
                Assert.IsNotNull(MotorPresetRegistry.Get(p), $"Preset {p} returned null");
        }

        [Test]
        public void Presets_HasSixEntries()
        {
            Assert.AreEqual(6, MotorPresetRegistry.Presets.Length);
        }

        [Test]
        public void Get_Motor1_5T_HasHighestEngineForce()
        {
            var high = MotorPresetRegistry.Get(RCCar.MotorPreset.Motor1_5T);
            var low  = MotorPresetRegistry.Get(RCCar.MotorPreset.Motor21_5T);
            Assert.Greater(high.Value.EngineForceMax, low.Value.EngineForceMax);
        }
    }
}
