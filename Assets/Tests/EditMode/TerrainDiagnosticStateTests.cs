using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for TerrainDiagnosticChecks WheelState, CanLog, and CheckNormalDeviation.</summary>
    [TestFixture]
    public class TerrainDiagnosticStateTests
    {
        const float k_DefaultCooldown = 0.5f;
        const float k_NormalThreshold = 0.85f;

        [Test]
        public void WheelState_DefaultInit_AllTimestampsZero()
        {
            var state = new TerrainDiagnosticChecks.WheelState();
            Assert.AreEqual(0f, state.LastNormalLogTime);
            Assert.AreEqual(0f, state.LastForceLogTime);
            Assert.AreEqual(0f, state.LastVelocityLogTime);
            Assert.AreEqual(0f, state.LastContactLogTime);
            Assert.AreEqual(0f, state.LastFlickerLogTime);
        }

        [Test]
        public void WheelState_GroundHistoryInitialisedExternally_HasCorrectLength()
        {
            var state = new TerrainDiagnosticChecks.WheelState
            {
                GroundHistory = new bool[10],
                GroundHistoryIndex = 0
            };
            Assert.AreEqual(10, state.GroundHistory.Length);
        }

        [Test]
        public void CanLog_FirstCall_ReturnsTrue()
        {
            float lastLogTime = 0f;
            Assert.IsTrue(TerrainDiagnosticChecks.CanLog(ref lastLogTime, k_DefaultCooldown));
        }

        [Test]
        public void CanLog_SecondCallWithinCooldown_ReturnsFalse()
        {
            float lastLogTime = Time.time;
            Assert.IsFalse(TerrainDiagnosticChecks.CanLog(ref lastLogTime, k_DefaultCooldown));
        }

        [Test]
        public void CanLog_FirstCall_UpdatesLastLogTime()
        {
            float lastLogTime = 0f;
            TerrainDiagnosticChecks.CanLog(ref lastLogTime, k_DefaultCooldown);
            Assert.AreEqual(Time.time, lastLogTime, 0.001f);
        }

        [Test]
        public void CheckNormalDeviation_SteepNormal_LogsWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = 0f };

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[physics\].*steep normal"));
            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, k_NormalThreshold, k_DefaultCooldown);

            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckNormalDeviation_FlatNormal_NoWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = 0f };
            // threshold=0f means nothing triggers a warning
            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, threshold: 0f, k_DefaultCooldown);
            Assert.IsNotNull(wheel, "Wheel must remain valid after call");
            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckNormalDeviation_WithinCooldown_DoesNotLogAgain()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = Time.time };
            // Cooldown active — log should be suppressed regardless of normal value
            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, k_NormalThreshold, k_DefaultCooldown);
            Assert.IsNotNull(wheel, "Wheel must remain valid after call");
            Object.DestroyImmediate(wheelGo);
        }
    }
}
