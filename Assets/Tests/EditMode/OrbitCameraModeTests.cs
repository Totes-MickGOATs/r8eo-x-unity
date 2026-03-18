using NUnit.Framework;
using UnityEngine;
using R8EOX.Camera;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for <see cref="OrbitCameraMode"/> pitch clamping and yaw wrap.</summary>
    public class OrbitCameraModeTests
    {
        // ---- Helpers ----

        private static OrbitCameraMode MakeMode()
            => new OrbitCameraMode { Distance = 5f, Sensitivity = 100f };

        private static Transform MakeTarget()
        {
            var go = new GameObject();
            return go.transform;
        }


        // ---- Pitch Clamping ----

        [Test]
        public void Pitch_AfterOnEnterWithCameraAboveTarget_ClampedToMaxPitch()
        {
            OrbitCameraMode mode = MakeMode();
            Transform target = MakeTarget();

            // Place camera directly above — would give pitch near 90 degrees.
            var camGo = new GameObject();
            camGo.transform.position = new Vector3(0f, 20f, 0f);

            mode.OnEnter(camGo.transform, target);

            Assert.LessOrEqual(mode.Pitch, mode.MaxPitch,
                "Pitch must not exceed MaxPitch after OnEnter");
        }

        [Test]
        public void Pitch_AfterOnEnterWithCameraBelowMinimum_ClampedToMinPitch()
        {
            OrbitCameraMode mode = MakeMode();
            Transform target = MakeTarget();

            // Place camera below the look-at plane — would give negative pitch beyond min.
            var camGo = new GameObject();
            camGo.transform.position = new Vector3(0f, -20f, 0f);

            mode.OnEnter(camGo.transform, target);

            Assert.GreaterOrEqual(mode.Pitch, mode.MinPitch,
                "Pitch must not be below MinPitch after OnEnter");
        }

        [Test]
        public void Pitch_AlwaysWithinClampRange_ForAnyOnEnterPosition()
        {
            var positions = new[]
            {
                new Vector3(0f, 100f, 0f),
                new Vector3(0f, -100f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(0f, 0f, 100f),
                new Vector3(5f, 3f, -7f),
            };

            foreach (Vector3 camPos in positions)
            {
                OrbitCameraMode mode = MakeMode();
                Transform target = MakeTarget();
                var camGo = new GameObject();
                camGo.transform.position = camPos;

                mode.OnEnter(camGo.transform, target);

                Assert.GreaterOrEqual(mode.Pitch, mode.MinPitch,
                    $"Pitch below min for cam pos {camPos}");
                Assert.LessOrEqual(mode.Pitch, mode.MaxPitch,
                    $"Pitch above max for cam pos {camPos}");
            }
        }


        // ---- Yaw ----

        [Test]
        public void Yaw_AfterOnEnterWithCameraToRight_ApproximatelyPositive90()
        {
            OrbitCameraMode mode = new OrbitCameraMode { Distance = 5f, LookHeight = 0f };
            Transform target = MakeTarget();
            var camGo = new GameObject();
            camGo.transform.position = new Vector3(5f, 0f, 0f); // to the right

            mode.OnEnter(camGo.transform, target);

            // atan2(x=5, z=0) = 90 degrees.
            Assert.AreEqual(90f, mode.Yaw, 0.5f,
                "Yaw should be ~90 degrees when camera is to the right");
        }


        // ---- ComputeTargetPose ----

        [Test]
        public void ComputeTargetPose_PositionIsAtCorrectDistance()
        {
            OrbitCameraMode mode = MakeMode();
            Transform target = MakeTarget();
            mode.OnEnter(null, null); // sets default yaw/pitch

            CameraPose pose = mode.ComputeTargetPose(target);

            Vector3 lookAt = target.position + Vector3.up * mode.LookHeight;
            float dist = Vector3.Distance(pose.Position, lookAt);

            Assert.AreEqual(mode.Distance, dist, 0.01f,
                "Camera position must be exactly Distance from the look-at point");
        }

        [Test]
        public void ComputeTargetPose_RotationLooksAtTarget()
        {
            OrbitCameraMode mode = new OrbitCameraMode
            {
                Distance = 5f,
                LookHeight = 0f
            };
            Transform target = MakeTarget();
            mode.OnEnter(null, null);

            CameraPose pose = mode.ComputeTargetPose(target);

            Vector3 toTarget = (target.position - pose.Position).normalized;
            Vector3 camForward = pose.Rotation * Vector3.forward;
            float dot = Vector3.Dot(toTarget, camForward);

            Assert.Greater(dot, 0.99f,
                "Orbit camera must look toward the target");
        }
    }
}
