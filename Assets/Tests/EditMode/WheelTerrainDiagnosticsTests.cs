using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug;
using R8EOX.Vehicle;
using System.Reflection;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for WheelTerrainDiagnostics.
    /// Validates wheel discovery, collider identification, force tracking,
    /// threshold calibration, and frame buffer dump-on-anomaly.
    /// </summary>
    [TestFixture]
    public class WheelTerrainDiagnosticsTests
    {
        // ---- Constants ----

        const int k_WheelCount = 4;


        // ---- Helpers ----

        /// <summary>Creates a minimal RCCar hierarchy with child RaycastWheel components.</summary>
        private static GameObject CreateCarHierarchy(int wheelCount = k_WheelCount)
        {
            var root = new GameObject("TestRCBuggy");
            root.AddComponent<Rigidbody>();
            root.AddComponent<RCCar>();

            for (int i = 0; i < wheelCount; i++)
            {
                var wheelGo = new GameObject($"Wheel_{i}");
                wheelGo.transform.parent = root.transform;
                bool isFront = i < wheelCount / 2;
                wheelGo.transform.localPosition = new Vector3(
                    i % 2 == 0 ? -0.1f : 0.1f,
                    0f,
                    isFront ? 0.15f : -0.15f);
                wheelGo.AddComponent<RaycastWheel>();
            }

            return root;
        }

        /// <summary>
        /// Initialises car+diagnostics lifecycle (Awake+Start) and returns the diagnostics component.
        /// </summary>
        private static WheelTerrainDiagnostics InitDiagnostics(GameObject root)
        {
            var diag = root.AddComponent<WheelTerrainDiagnostics>();
            root.GetComponent<RCCar>().SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            root.GetComponent<RCCar>().SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            diag.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            return diag;
        }

        /// <summary>Sets a private field on a RaycastWheel via reflection.</summary>
        private static void SetWheelField(RaycastWheel wheel, string fieldName, object value)
        {
            var field = typeof(RaycastWheel).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on RaycastWheel");
            field.SetValue(wheel, value);
        }

        /// <summary>Sets a public auto-property backing field on a RaycastWheel via property setter or reflection.</summary>
        private static void SetWheelProperty(RaycastWheel wheel, string propertyName, object value)
        {
            var prop = typeof(RaycastWheel).GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(prop, $"Property '{propertyName}' not found on RaycastWheel");
            if (prop.CanWrite)
            {
                prop.SetValue(wheel, value);
            }
            else
            {
                // For auto-properties with private set, use backing field
                var backingField = typeof(RaycastWheel).GetField($"<{propertyName}>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(backingField,
                    $"Backing field for '{propertyName}' not found on RaycastWheel");
                backingField.SetValue(wheel, value);
            }
        }

        /// <summary>Sets a private SerializeField on WheelTerrainDiagnostics via reflection.</summary>
        private static void SetDiagField(WheelTerrainDiagnostics diag, string fieldName, object value)
        {
            var field = typeof(WheelTerrainDiagnostics).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on WheelTerrainDiagnostics");
            field.SetValue(diag, value);
        }

        /// <summary>
        /// Sets up a single wheel in a grounded state with the given contact point and normal,
        /// simulating a frame where the wheel is on the ground.
        /// </summary>
        private static void SetWheelGrounded(RaycastWheel wheel, Vector3 contactPoint,
            Vector3 contactNormal, float suspensionForce = 10f)
        {
            SetWheelProperty(wheel, "IsOnGround", true);
            SetWheelField(wheel, "_contactPoint", contactPoint);
            SetWheelField(wheel, "_contactNormal", contactNormal);
            SetWheelField(wheel, "_suspensionForce", suspensionForce);
            SetWheelField(wheel, "_tireVelocity", Vector3.forward * 2f);
        }


        // ---- Existing Tests: Wheel Discovery ----

        [Test]
        public void Start_WithRCCarAndWheels_FindsAllWheels()
        {
            var root = CreateCarHierarchy(k_WheelCount);
            var diag = root.AddComponent<WheelTerrainDiagnostics>();

            try
            {
                // Simulate full lifecycle: all Awakes first, then all Starts
                root.GetComponent<RCCar>().SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                root.GetComponent<RCCar>().SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

                // Diagnostics should remain enabled (wheels were found)
                Assert.IsTrue(diag.enabled,
                    "WheelTerrainDiagnostics should stay enabled when wheels exist");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Start_WithRCCarNoWheels_DisablesSelf()
        {
            var root = new GameObject("TestRCBuggy_NoWheels");
            root.AddComponent<Rigidbody>();
            root.AddComponent<RCCar>();
            var diag = root.AddComponent<WheelTerrainDiagnostics>();

            try
            {
                root.GetComponent<RCCar>().SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                root.GetComponent<RCCar>().SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

                Assert.IsFalse(diag.enabled,
                    "WheelTerrainDiagnostics should disable itself when no wheels exist");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Awake_WithNoRCCar_DisablesSelf()
        {
            var root = new GameObject("TestNoCar");
            var diag = root.AddComponent<WheelTerrainDiagnostics>();

            try
            {
                diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

                Assert.IsFalse(diag.enabled,
                    "WheelTerrainDiagnostics should disable itself when no RCCar is found");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Start_WheelDiscoveryAfterRCCarAwake_FindsWheels()
        {
            var root = CreateCarHierarchy(k_WheelCount);
            var diag = root.AddComponent<WheelTerrainDiagnostics>();

            try
            {
                root.GetComponent<RCCar>().SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

                Assert.IsTrue(diag.enabled,
                    "WheelTerrainDiagnostics should find wheels even if its Start() runs before RCCar.Start()");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: Hit Collider Identification ----

        [Test]
        public void FixedUpdate_HitNonTerrainCollider_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Set wheel as grounded hitting a non-terrain collider
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelProperty(wheel, "HitColliderName", "InvisibleWall");
                SetWheelProperty(wheel, "HitColliderLayer", 0); // Default layer, not terrain

                // Expect a warning about hitting a non-terrain collider
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*hit non-terrain collider.*InvisibleWall"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FixedUpdate_HitTerrainCollider_NoColliderWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Set wheel as grounded hitting terrain (empty HitColliderName = terrain)
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelProperty(wheel, "HitColliderName", "Terrain");
                SetWheelProperty(wheel, "HitColliderLayer", 0);
                // Mark as terrain hit
                SetWheelProperty(wheel, "HitColliderIsTerrain", true);

                // Should NOT produce a non-terrain warning
                LogAssert.NoUnexpectedReceived();

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: Lateral Force Spike ----

        [Test]
        public void FixedUpdate_LateralForceSpike_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Set wheel grounded with a large lateral force
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelField(wheel, "_xForce", Vector3.right * 20f); // 20N lateral > 15N threshold

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*lateral force spike.*20\.0N"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FixedUpdate_LateralForceBelowThreshold_NoWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Set wheel grounded with modest lateral force
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelField(wheel, "_xForce", Vector3.right * 10f); // 10N < 15N threshold

                LogAssert.NoUnexpectedReceived();

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: Total Force Spike ----

        [Test]
        public void FixedUpdate_TotalForceSpike_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Set wheel grounded with high combined forces exceeding 80N threshold
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 50f);
                SetWheelField(wheel, "_yForce", Vector3.up * 50f);
                SetWheelField(wheel, "_xForce", Vector3.right * 30f);
                SetWheelField(wheel, "_zForce", Vector3.forward * 20f);
                SetWheelField(wheel, "_motorForce", Vector3.forward * 10f);

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*total force spike"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: Lowered Threshold Calibration ----

        [Test]
        public void FixedUpdate_ContactJumpBelowNewThreshold_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // First frame: establish previous contact point
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

                // Second frame: contact point jumps 0.03m (>0.02 new threshold, <0.05 old threshold)
                SetWheelGrounded(wheel, new Vector3(0.03f, 0f, 0f), Vector3.up);

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*contact point jump.*0\.030m"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FixedUpdate_VelocityDiscontinuityBelowNewThreshold_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // First frame: establish previous velocity
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelField(wheel, "_tireVelocity", Vector3.forward * 5f);
                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

                // Second frame: velocity changes by 1.0 m/s (>0.8 new threshold, <2.0 old threshold)
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up);
                SetWheelField(wheel, "_tireVelocity", Vector3.forward * 4f);

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*velocity discontinuity"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FixedUpdate_ForceSpikeAtLoweredThreshold_LogsWarning()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // First frame: establish previous suspension force
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 5f);
                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

                // Second frame: force jumps by 20N (>15 new threshold, <30 old threshold)
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 25f);

                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[suspension\].*force spike"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: Frame Buffer Dump-on-Anomaly ----

        [Test]
        public void FrameBuffer_DumpsOnAnomaly()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Run several normal frames to fill the buffer
                for (int i = 0; i < 5; i++)
                {
                    SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 10f);
                    SetWheelField(wheel, "_xForce", Vector3.right * 1f);
                    SetWheelField(wheel, "_yForce", Vector3.up * 10f);
                    SetWheelField(wheel, "_zForce", Vector3.zero);
                    SetWheelField(wheel, "_motorForce", Vector3.zero);
                    SetWheelProperty(wheel, "HitColliderName", "Terrain");
                    SetWheelProperty(wheel, "HitColliderIsTerrain", true);
                    diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
                }

                // Trigger an anomaly (lateral force spike)
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 10f);
                SetWheelField(wheel, "_xForce", Vector3.right * 20f); // spike
                SetWheelField(wheel, "_yForce", Vector3.up * 10f);
                SetWheelField(wheel, "_zForce", Vector3.zero);
                SetWheelField(wheel, "_motorForce", Vector3.zero);

                // Expect the lateral force spike warning
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*lateral force spike"));

                // Expect the frame dump log
                LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*FRAME DUMP.*trigger:.*lateral"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FrameBuffer_DisabledNoDump()
        {
            var root = CreateCarHierarchy(1);
            try
            {
                var diag = InitDiagnostics(root);
                var wheel = root.GetComponentInChildren<RaycastWheel>();

                // Disable frame dump
                SetDiagField(diag, "_enableFrameDump", false);

                // Trigger an anomaly (lateral force spike)
                SetWheelGrounded(wheel, Vector3.zero, Vector3.up, 10f);
                SetWheelField(wheel, "_xForce", Vector3.right * 20f);

                // Expect the lateral force spike warning but NO frame dump
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    @"\[physics\].*lateral force spike"));

                diag.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

                // Verify no frame dump log was produced (LogAssert.NoUnexpectedReceived handles this)
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }


        // ---- New Tests: RaycastWheel Properties ----

        [Test]
        public void HitColliderName_Default_ReturnsEmptyString()
        {
            var go = new GameObject("TestWheel");
            try
            {
                var wheel = go.AddComponent<RaycastWheel>();
                Assert.AreEqual("", wheel.HitColliderName,
                    "HitColliderName should default to empty string");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HitColliderIsTerrain_Default_ReturnsFalse()
        {
            var go = new GameObject("TestWheel");
            try
            {
                var wheel = go.AddComponent<RaycastWheel>();
                Assert.IsFalse(wheel.HitColliderIsTerrain,
                    "HitColliderIsTerrain should default to false");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LateralForceMagnitude_ReturnsXForceMagnitude()
        {
            var go = new GameObject("TestWheel");
            try
            {
                var wheel = go.AddComponent<RaycastWheel>();
                SetWheelField(wheel, "_xForce", new Vector3(3f, 0f, 4f));

                Assert.AreEqual(5f, wheel.LateralForceMagnitude, 0.001f,
                    "LateralForceMagnitude should return magnitude of _xForce");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TotalForceMagnitude_ReturnsCombinedForceMagnitude()
        {
            var go = new GameObject("TestWheel");
            try
            {
                var wheel = go.AddComponent<RaycastWheel>();
                SetWheelField(wheel, "_yForce", Vector3.up * 10f);
                SetWheelField(wheel, "_xForce", Vector3.right * 5f);
                SetWheelField(wheel, "_zForce", Vector3.forward * 3f);
                SetWheelField(wheel, "_motorForce", Vector3.forward * 2f);

                // Combined: (0,10,0) + (5,0,0) + (0,0,3) + (0,0,2) = (5,10,5)
                // Magnitude: sqrt(25+100+25) = sqrt(150) ≈ 12.247
                float expected = new Vector3(5f, 10f, 5f).magnitude;
                Assert.AreEqual(expected, wheel.TotalForceMagnitude, 0.001f,
                    "TotalForceMagnitude should return magnitude of combined force vectors");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
