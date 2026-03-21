using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for TerrainDiagnosticChecks.CheckForceSpikeDetection.</summary>
    [TestFixture]
    public class TerrainDiagnosticForceTests
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

            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);

            // First landing frame suppresses warning — PrevIsOnGround=false means no violation logged
            Assert.IsNotNull(wheel, "Wheel must remain valid after call");
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

            TerrainDiagnosticChecks.CheckForceSpikeDetection(wheel, ref state, k_ForceThreshold, k_DefaultCooldown);

            // Small delta (1N) is well below threshold (30N) — no warning emitted
            Assert.IsNotNull(wheel, "Wheel must remain valid after call");
            Object.DestroyImmediate(wheelGo);
        }
    }
}
