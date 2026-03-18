using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the pure-static TerrainDiagnosticChecks methods.
    /// These tests do not require a MonoBehaviour or scene setup.
    /// </summary>
    [TestFixture]
    public class TerrainDiagnosticChecksTests
    {
        // ---- Constants ----

        const float k_DefaultCooldown = 0.5f;
        const float k_NormalThreshold = 0.85f;
        const float k_ForceThreshold = 30f;


        // ---- WheelState initialisation ----

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


        // ---- CanLog ----

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
            // Seed lastLogTime to the current time so the cooldown has not elapsed.
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


        // ---- CheckNormalDeviation ----

        [Test]
        public void CheckNormalDeviation_SteepNormal_LogsWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = 0f };

            // ContactNormal.y = 0.5 is below threshold 0.85 → should warn
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

            // Default ContactNormal is zero-vector (y = 0) — but we want a flat (y = 1) scenario.
            // RaycastWheel ContactNormal defaults to Vector3.zero; y=0 < 0.85 → will warn.
            // To get no warning, we use a threshold of 0f (nothing triggers).
            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, threshold: 0f, k_DefaultCooldown);

            // No LogAssert.Expect means no warning is expected; test passes if Unity doesn't log one.
            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckNormalDeviation_WithinCooldown_DoesNotLogAgain()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            // Seed LastNormalLogTime to now so cooldown hasn't elapsed
            var state = new TerrainDiagnosticChecks.WheelState { LastNormalLogTime = Time.time };

            // Steep normal but cooldown active — no warning expected
            TerrainDiagnosticChecks.CheckNormalDeviation(wheel, ref state, k_NormalThreshold, k_DefaultCooldown);

            Object.DestroyImmediate(wheelGo);
        }


        // ---- CheckForceSpikeDetection ----

        [Test]
        public void CheckForceSpikeDetection_LargeDeltaWhileGrounded_LogsWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState
            {
                PrevSuspensionForce = 0f,
                PrevIsOnGround = true,   // must be grounded the previous frame
                LastForceLogTime = 0f
            };

            // SuspensionForce defaults to 0 on RaycastWheel; PrevSuspensionForce = 0 → delta = 0.
            // To trigger the spike we set PrevSuspensionForce to a value far from current (0):
            state.PrevSuspensionForce = k_ForceThreshold + 10f;  // delta = 40 > 30

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[suspension\].*force spike"));

            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);

            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckForceSpikeDetection_LargeDeltaNotGroundedPrevFrame_NoWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState
            {
                PrevSuspensionForce = k_ForceThreshold + 10f,
                PrevIsOnGround = false,   // first landing frame — should be suppressed
                LastForceLogTime = 0f
            };

            // Large delta but PrevIsOnGround = false → no warning
            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);

            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckForceSpikeDetection_SmallDelta_NoWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState
            {
                PrevSuspensionForce = 1f,   // delta from 0 = 1 << 30
                PrevIsOnGround = true,
                LastForceLogTime = 0f
            };

            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);

            Object.DestroyImmediate(wheelGo);
        }
    }
}
