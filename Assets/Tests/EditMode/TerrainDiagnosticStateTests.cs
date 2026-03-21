using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for TerrainDiagnosticChecks: WheelState initialisation, CanLog, and
    /// CheckNormalDeviation. Force-spike tests live in TerrainDiagnosticSpikeTests.cs.
    /// </summary>
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
            const int k_WindowFrames = 10;
            var state = new TerrainDiagnosticChecks.WheelState
            {
                GroundHistory = new bool[k_WindowFrames],
                GroundHistoryIndex = 0
            };

            Assert.AreEqual(k_WindowFrames, state.GroundHistory.Length);
        }

        [Test]
        public void CanLog_FirstCall_ReturnsTrue()
        {
            float lastLogTime = 0f;
            bool result = TerrainDiagnosticChecks.CanLog(ref lastLogTime, k_DefaultCooldown);
            Assert.IsTrue(result);
        }

        [Test]
        public void CanLog_SecondCallWithinCooldown_ReturnsFalse()
        {
            float lastLogTime = Time.time;
            bool result = TerrainDiagnosticChecks.CanLog(ref lastLogTime, k_DefaultCooldown);
            Assert.IsFalse(result);
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

            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, threshold: 0f, k_DefaultCooldown);
            Assert.AreEqual(0f, state.LastNormalLogTime, 0.001f,
                "Log time must stay zero when threshold is zero (no warning fired)");

            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckNormalDeviation_WithinCooldown_DoesNotLogAgain()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            float seedTime = Time.time;
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = seedTime };

            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, k_NormalThreshold, k_DefaultCooldown);
            Assert.AreEqual(seedTime, state.LastNormalLogTime, 0.001f,
                "Log time must not advance when within cooldown window");

            Object.DestroyImmediate(wheelGo);
        }
    }
}
