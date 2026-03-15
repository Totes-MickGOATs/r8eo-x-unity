using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for WheelTerrainDiagnostics wheel discovery.
    /// Validates that diagnostics correctly discovers wheels regardless of
    /// Unity Start() execution order between RCCar and WheelTerrainDiagnostics.
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


        // ---- Tests ----

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
            // Critical order test: diagnostics Start() runs BEFORE RCCar Start().
            // This simulates the actual bug scenario where RCCar.DiscoverWheels()
            // hasn't been called yet when diagnostics tries to read wheels.
            var root = CreateCarHierarchy(k_WheelCount);
            var diag = root.AddComponent<WheelTerrainDiagnostics>();

            try
            {
                // All Awakes run first (Unity guarantees this)
                root.GetComponent<RCCar>().SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                diag.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

                // Diagnostics Start() runs BEFORE RCCar Start() — the problematic order
                diag.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

                // Even without RCCar.Start() having run, diagnostics should find wheels
                Assert.IsTrue(diag.enabled,
                    "WheelTerrainDiagnostics should find wheels even if its Start() runs before RCCar.Start()");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
