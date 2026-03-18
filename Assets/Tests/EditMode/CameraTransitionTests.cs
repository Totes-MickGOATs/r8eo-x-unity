using NUnit.Framework;
using UnityEngine;
using R8EOX.Camera;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for <see cref="CameraTransition"/> lerp, SmoothStep, and completion.</summary>
    public class CameraTransitionTests
    {
        // ---- Helpers ----

        private static CameraPose MakePose(Vector3 pos) =>
            new CameraPose(pos, Quaternion.identity);

        private static CameraTransition MakeStarted(
            Vector3 from, Vector3 to, float speed = 1f)
        {
            var t = new CameraTransition();
            t.Begin(MakePose(from), MakePose(to), speed);
            return t;
        }


        // ---- Begin / IsActive ----

        [Test]
        public void Begin_NewTransition_IsActive()
        {
            CameraTransition t = MakeStarted(Vector3.zero, Vector3.one);
            Assert.IsTrue(t.IsActive, "Transition must be active immediately after Begin");
        }

        [Test]
        public void Begin_BeforeStart_IsNotActive()
        {
            var t = new CameraTransition();
            Assert.IsFalse(t.IsActive, "Fresh transition must not be active");
        }


        // ---- Evaluate (SmoothStep) ----

        [Test]
        public void Evaluate_AtZero_ReturnsStartPosition()
        {
            CameraTransition t = MakeStarted(Vector3.zero, new Vector3(10f, 0f, 0f));

            CameraPose pose = t.Evaluate(0f);

            Assert.AreEqual(Vector3.zero, pose.Position,
                "At t=0 position must equal start");
        }

        [Test]
        public void Evaluate_AtOne_ReturnsEndPosition()
        {
            CameraTransition t = MakeStarted(Vector3.zero, new Vector3(10f, 0f, 0f));

            CameraPose pose = t.Evaluate(1f);

            Assert.AreEqual(new Vector3(10f, 0f, 0f), pose.Position,
                "At t=1 position must equal end");
        }

        [Test]
        public void Evaluate_AtHalf_ReturnsMidpointDueToSmoothStep()
        {
            // SmoothStep(0.5) = 0.5 exactly (symmetry), so midpoint is 5.
            CameraTransition t = MakeStarted(Vector3.zero, new Vector3(10f, 0f, 0f));

            CameraPose pose = t.Evaluate(0.5f);

            Assert.AreEqual(5f, pose.Position.x, 0.001f,
                "SmoothStep(0.5) = 0.5 so midpoint must be half-way");
        }

        [Test]
        public void Evaluate_SmoothStep_CurveIsSlowerAtEdgesThanLinear()
        {
            // At t=0.1 SmoothStep < linear => position.x should be less than 1 (linear).
            CameraTransition t = MakeStarted(Vector3.zero, new Vector3(10f, 0f, 0f));

            CameraPose pose01 = t.Evaluate(0.1f);
            float linear01 = 1f; // 0.1 * 10
            float smooth01 = Mathf.SmoothStep(0f, 1f, 0.1f) * 10f;

            Assert.AreEqual(smooth01, pose01.Position.x, 0.001f,
                "Evaluate must use SmoothStep not linear interpolation");
            Assert.Less(pose01.Position.x, linear01,
                "SmoothStep at 0.1 must be less than linear at 0.1 (ease-in)");
        }


        // ---- Progress and Completion ----

        [Test]
        public void Progress_AfterBegin_IsZero()
        {
            CameraTransition t = MakeStarted(Vector3.zero, Vector3.one);
            Assert.AreEqual(0f, t.Progress, 0.001f,
                "Progress must start at zero");
        }

        [Test]
        public void IsActive_AfterEvaluateAtOne_RemainsActiveUntilAdvanced()
        {
            // Evaluate does NOT advance internal state.
            CameraTransition t = MakeStarted(Vector3.zero, Vector3.one);
            t.Evaluate(1f);
            Assert.IsTrue(t.IsActive,
                "Evaluate must not change IsActive — only Advance does");
        }

        [Test]
        public void Begin_RestartsExistingTransition_ProgressResetsToZero()
        {
            CameraTransition t = MakeStarted(Vector3.zero, Vector3.one);
            // Partially advance (simulate time passing without Time.deltaTime).
            // We can't call Advance() in edit mode without a running time loop,
            // so re-begin and check progress reset.
            t.Begin(MakePose(Vector3.one), MakePose(Vector3.zero * 2f), 2f);

            Assert.AreEqual(0f, t.Progress, 0.001f,
                "Calling Begin again must reset progress to zero");
        }

        [Test]
        public void Evaluate_ClampsBeyondOne_ToEndPosition()
        {
            CameraTransition t = MakeStarted(Vector3.zero, new Vector3(5f, 0f, 0f));

            CameraPose pose = t.Evaluate(2f); // clamped to 1

            Assert.AreEqual(5f, pose.Position.x, 0.001f,
                "t > 1 must clamp to end position");
        }

        [Test]
        public void Evaluate_NegativeT_ClampsToStartPosition()
        {
            CameraTransition t = MakeStarted(new Vector3(3f, 0f, 0f), Vector3.zero);

            CameraPose pose = t.Evaluate(-1f);

            Assert.AreEqual(3f, pose.Position.x, 0.001f,
                "Negative t must clamp to start position");
        }
    }
}
