using NUnit.Framework;
using R8EOX.Vehicle;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Smoke tests for the <see cref="RCCar"/> serialized-field partial class split.
    /// Verifies that all public accessor properties declared in <c>RCCarSerializedFields.cs</c>
    /// remain readable after the partial-class extraction and that tuning setters
    /// round-trip correctly.
    /// </summary>
    public class RCCarSerializedFieldsTests
    {
        RCCar _car;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestCar");
            go.AddComponent<Rigidbody>();
            _car = go.AddComponent<RCCar>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_car.gameObject);
        }

        // ---- Accessor smoke tests ----

        [Test]
        public void EngineForceMax_DefaultValue_IsPositive()
        {
            Assert.Greater(_car.EngineForceMax, 0f,
                "EngineForceMax must have a positive default value.");
        }

        [Test]
        public void MaxSpeed_DefaultValue_IsPositive()
        {
            Assert.Greater(_car.MaxSpeed, 0f,
                "MaxSpeed must have a positive default value.");
        }

        [Test]
        public void BrakeForce_DefaultValue_IsPositive()
        {
            Assert.Greater(_car.BrakeForce, 0f,
                "BrakeForce must have a positive default value.");
        }

        [Test]
        public void SteeringMax_DefaultValue_IsPositive()
        {
            Assert.Greater(_car.SteeringMax, 0f,
                "SteeringMax must have a positive default value.");
        }

        // ---- Setter round-trip tests ----

        [Test]
        public void SetMotorParams_WhenCalled_UpdatesEngineForce()
        {
            const float newForce = 999f;
            _car.SetMotorParams(newForce, 20f, 100f, 80f, 15f);

            Assert.AreEqual(newForce, _car.EngineForceMax, 0.001f,
                "SetMotorParams must update EngineForceMax.");
        }

        [Test]
        public void SetSteeringParams_WhenCalled_UpdatesSteeringMax()
        {
            const float newMax = 0.75f;
            _car.SetSteeringParams(newMax, 8f, 10f, 0.5f);

            Assert.AreEqual(newMax, _car.SteeringMax, 0.001f,
                "SetSteeringParams must update SteeringMax.");
        }

        [Test]
        public void SetTraction_WhenCalled_UpdatesGripCoeff()
        {
            const float newGrip = 0.9f;
            _car.SetTraction(newGrip);

            Assert.AreEqual(newGrip, _car.GripCoeff, 0.001f,
                "SetTraction must update GripCoeff.");
        }

        [Test]
        public void SelectMotorPreset_Motor215T_SetsExpectedEngineForce()
        {
            _car.SelectMotorPreset(RCCar.MotorPreset.Motor21_5T);

            // 21.5T preset: 155N engine force (from MotorPresetRegistry)
            Assert.AreEqual(155f, _car.EngineForceMax, 0.001f,
                "Motor21_5T preset must set EngineForceMax to 155N.");
        }

        [Test]
        public void ActiveMotorPreset_AfterSelectPreset_ReturnsSelectedPreset()
        {
            _car.SelectMotorPreset(RCCar.MotorPreset.Motor9_5T);

            Assert.AreEqual(RCCar.MotorPreset.Motor9_5T, _car.ActiveMotorPreset,
                "ActiveMotorPreset must reflect the most recently selected preset.");
        }
    }
}
