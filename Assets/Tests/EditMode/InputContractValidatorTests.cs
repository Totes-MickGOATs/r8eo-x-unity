using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Validators;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for InputContractValidator.
    /// Tests the static validation method directly — no MonoBehaviour needed.
    /// </summary>
    [TestFixture]
    public class InputContractValidatorTests
    {
#if UNITY_EDITOR || DEBUG
        // ---- Minimal IVehicleInput stub ----

        private struct StubInput : IVehicleInput
        {
            public float Throttle { get; set; }
            public float Brake { get; set; }
            public float Steer { get; set; }
            public bool ResetPressed => false;
            public bool DebugTogglePressed => false;
            public bool CameraCyclePressed => false;
            public bool PausePressed => false;
        }


        // ---- Null safety ----

        [Test]
        public void ValidateInputContracts_NullInput_ReturnsZeroViolations()
        {
            int violations = InputContractValidator.ValidateInputContracts(null, false);
            Assert.AreEqual(0, violations);
        }


        // ---- Throttle ----

        [Test]
        public void ValidateInputContracts_ThrottleInRange_ReturnsZeroViolations()
        {
            var input = new StubInput { Throttle = 0.5f, Brake = 0f, Steer = 0f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(0, violations);
        }

        [Test]
        public void ValidateInputContracts_ThrottleAboveOne_ReturnsOneViolation()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                @"\[ContractDebugger\] INPUT VIOLATION: Throttle out of range"));
            var input = new StubInput { Throttle = 1.5f, Brake = 0f, Steer = 0f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(1, violations);
        }

        [Test]
        public void ValidateInputContracts_ThrottleNegative_ReturnsOneViolation()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                @"\[ContractDebugger\] INPUT VIOLATION: Throttle out of range"));
            var input = new StubInput { Throttle = -0.1f, Brake = 0f, Steer = 0f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(1, violations);
        }


        // ---- Brake ----

        [Test]
        public void ValidateInputContracts_BrakeAboveOne_ReturnsOneViolation()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                @"\[ContractDebugger\] INPUT VIOLATION: Brake out of range"));
            var input = new StubInput { Throttle = 0f, Brake = 1.2f, Steer = 0f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(1, violations);
        }


        // ---- Steer ----

        [Test]
        public void ValidateInputContracts_SteerBeyondNegativeOne_ReturnsOneViolation()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                @"\[ContractDebugger\] INPUT VIOLATION: Steer out of range"));
            var input = new StubInput { Throttle = 0f, Brake = 0f, Steer = -1.5f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(1, violations);
        }

        [Test]
        public void ValidateInputContracts_AllValidExtremes_ReturnsZeroViolations()
        {
            var input = new StubInput { Throttle = 1f, Brake = 1f, Steer = -1f };
            int violations = InputContractValidator.ValidateInputContracts(input, false);
            Assert.AreEqual(0, violations);
        }
#endif // UNITY_EDITOR || DEBUG
    }
}
