using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for TerrainDiagnosticChecks.CheckForceSpikeDetection.
    /// WheelState and CheckNormalDeviation tests live in TerrainDiagnosticStateTests.cs.
    /// </summary>
    [TestFixture]
    public class TerrainDiagnosticSpikeTests
    {
        const float k_DefaultCooldown = 0.5f;
        const float k_ForceThreshold = 30f;

        [Test]
        public void CheckForceSpikeDetection_LargeDeltaWhileGrounded_LogsWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState
            {
                PrevSuspensionForce = k_ForceThreshold + 10f,
                PrevIsOnGround = true,
                LastForceLogTime = 0f
            };

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
                PrevIsOnGround = false,
                LastForceLogTime = 0f
            };

            float prevLogTime = state.LastForceLogTime;
            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);
            // LogTime unchanged confirms no warning was emitted (first landing frame is suppressed)
            Assert.AreEqual(prevLogTime, state.LastForceLogTime, 0.001f,
                "Log time must not advance when first-landing suppression is active");

            Object.DestroyImmediate(wheelGo);
        }

        [Test]
        public void CheckForceSpikeDetection_SmallDelta_NoWarning()
        {
            var wheelGo = new GameObject("TestWheel");
            var wheel = wheelGo.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            var state = new TerrainDiagnosticChecks.WheelState
            {
                PrevSuspensionForce = 1f,
                PrevIsOnGround = true,
                LastForceLogTime = 0f
            };

            float prevLogTime = state.LastForceLogTime;
            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);
            Assert.AreEqual(prevLogTime, state.LastForceLogTime, 0.001f,
                "Log time must not advance for small delta below threshold");

            Object.DestroyImmediate(wheelGo);
        }
    }
}
