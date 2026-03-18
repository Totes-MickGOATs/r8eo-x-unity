using NUnit.Framework;
using UnityEngine;
using R8EOX.Camera;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for <see cref="ChaseCameraMode"/>.</summary>
    public class ChaseCameraModeTests
    {
        // ---- Helpers ----

        private static Transform MakeTransform(Vector3 position, Quaternion rotation)
        {
            var go = new GameObject();
            go.transform.SetPositionAndRotation(position, rotation);
            return go.transform;
        }

        private static Transform MakeTransform(Vector3 position)
            => MakeTransform(position, Quaternion.identity);


        // ---- ComputeDesiredPosition ----

        [Test]
        public void ComputeTargetPose_DefaultConfig_PositionIsBehindAndAboveTarget()
        {
            var mode = new ChaseCameraMode { Distance = 3f, Height = 1.5f };
            Transform target = MakeTransform(Vector3.zero);

            CameraPose pose = mode.ComputeTargetPose(target);

            // Target faces +Z (forward). Camera should be behind (-Z) and above (+Y).
            Assert.Less(pose.Position.z, 0f,
                "Camera must be behind target (negative Z when target faces +Z)");
            Assert.Greater(pose.Position.y, 0f,
                "Camera must be above target");
        }

        [Test]
        public void ComputeTargetPose_TargetFacingRight_PositionIsBehindInXAxis()
        {
            var mode = new ChaseCameraMode { Distance = 3f, Height = 0f };
            Transform target = MakeTransform(Vector3.zero,
                Quaternion.Euler(0f, 90f, 0f)); // facing +X

            CameraPose pose = mode.ComputeTargetPose(target);

            // Camera behind target facing +X should be at negative X.
            Assert.Less(pose.Position.x, 0f,
                "Camera must be behind target rotated to face +X");
        }

        [Test]
        public void ComputeTargetPose_RotationLooksAtTarget()
        {
            var mode = new ChaseCameraMode
            {
                Distance = 3f,
                Height = 0f,
                LookHeight = 0f
            };
            Transform target = MakeTransform(Vector3.zero);

            CameraPose pose = mode.ComputeTargetPose(target);

            // Forward vector of returned rotation must point toward target.
            Vector3 toTarget = (target.position - pose.Position).normalized;
            Vector3 camForward = pose.Rotation * Vector3.forward;
            float dot = Vector3.Dot(toTarget, camForward);

            Assert.Greater(dot, 0.99f,
                "Camera rotation must look toward the target");
        }

        [Test]
        public void ComputeTargetPose_WithLookHeight_RotationAimsBelowTarget()
        {
            var mode = new ChaseCameraMode
            {
                Distance = 5f,
                Height = 5f,
                LookHeight = 0f   // look at ground level
            };
            Transform target = MakeTransform(new Vector3(0f, 0f, 0f));

            CameraPose pose = mode.ComputeTargetPose(target);

            // Camera is above target; forward must tilt downward.
            float pitchAngle = pose.Rotation.eulerAngles.x;
            // Unity euler: 0-360; downward pitch is >0 (or equivalently < 360)
            // A value in (0, 90) or mapped equivalent means looking down.
            bool lookingDown = pitchAngle > 0f && pitchAngle < 90f;
            Assert.IsTrue(lookingDown, $"Expected downward look angle, got {pitchAngle}");
        }

        [Test]
        public void ComputeDesiredPosition_DistanceZero_PositionEqualsTargetPlusHeight()
        {
            var mode = new ChaseCameraMode { Distance = 0f, Height = 2f };
            Transform target = MakeTransform(new Vector3(1f, 0f, 1f));

            Vector3 pos = mode.ComputeDesiredPosition(target);

            Assert.AreEqual(target.position + Vector3.up * 2f, pos,
                "With zero distance only height offset should apply");
        }
    }
}
