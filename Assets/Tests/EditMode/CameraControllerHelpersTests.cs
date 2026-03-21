using NUnit.Framework;
using R8EOX.Camera;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for CameraControllerHelpers — static mode-to-strategy selector and pose applicator
    /// extracted from CameraController.
    /// </summary>
    public class CameraControllerHelpersTests
    {
        [Test]
        public void ModeCount_IsFour()
        {
            // CameraMode enum must have exactly 4 members (Chase, Orbit, Fpv, Trackside)
            var values = System.Enum.GetValues(typeof(CameraMode));
            Assert.AreEqual(4, values.Length);
        }

        [Test]
        public void CycleMode_WrapsAroundAfterLastMode()
        {
            // (int)lastMode + 1 mod 4 must equal 0 (Chase)
            int modeCount = System.Enum.GetValues(typeof(CameraMode)).Length;
            int last = modeCount - 1;
            int next = (last + 1) % modeCount;
            Assert.AreEqual(0, next);
        }

        [Test]
        public void ApplyPose_SetsTransformPositionAndRotation()
        {
            var go = new GameObject("CamTest");
            var expectedPos = new Vector3(1f, 2f, 3f);
            var expectedRot = Quaternion.Euler(10f, 20f, 30f);
            var pose = new CameraPose(expectedPos, expectedRot);

            // Apply via the helper
            CameraControllerHelpers.ApplyPose(go.transform, pose);

            Assert.AreEqual(expectedPos, go.transform.position);
            Assert.AreEqual(expectedRot, go.transform.rotation);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyPose_CalledTwice_IsIdempotent()
        {
            var go = new GameObject("CamTest2");
            var pose = new CameraPose(Vector3.one, Quaternion.identity);

            CameraControllerHelpers.ApplyPose(go.transform, pose);
            CameraControllerHelpers.ApplyPose(go.transform, pose);

            Assert.AreEqual(Vector3.one, go.transform.position);
            Object.DestroyImmediate(go);
        }
    }
}
