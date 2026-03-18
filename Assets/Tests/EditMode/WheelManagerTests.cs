using NUnit.Framework;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for WheelManager wheel discovery and configuration logic.
    /// </summary>
    public class WheelManagerTests
    {
        const float k_Eps = 0.001f;

        private static GameObject BuildHierarchy(int frontCount, int rearCount)
        {
            var root = new GameObject("Car");
            root.AddComponent<Rigidbody>().useGravity = false;

            for (int i = 0; i < frontCount; i++)
            {
                var go = new GameObject($"FrontWheel_{i}");
                go.transform.parent = root.transform;
                go.transform.localPosition = new Vector3(i * 0.1f, 0f, 0.15f);
                go.AddComponent<RaycastWheel>();
            }
            for (int i = 0; i < rearCount; i++)
            {
                var go = new GameObject($"RearWheel_{i}");
                go.transform.parent = root.transform;
                go.transform.localPosition = new Vector3(i * 0.1f, 0f, -0.15f);
                go.AddComponent<RaycastWheel>();
            }
            return root;
        }

        [Test]
        public void Discover_FindsAllWheels()
        {
            var root = BuildHierarchy(2, 2);
            var mgr  = new WheelManager();
            mgr.Discover(root.transform);

            Assert.AreEqual(4, mgr.All.Length);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Discover_PartitionsFrontAndRear()
        {
            var root = BuildHierarchy(2, 2);
            var mgr  = new WheelManager();
            mgr.Discover(root.transform);

            Assert.AreEqual(2, mgr.Front.Length, "Front wheel count");
            Assert.AreEqual(2, mgr.Rear.Length,  "Rear wheel count");
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplySuspension_SetsFrontAndRearSeparately()
        {
            var root = BuildHierarchy(2, 2);
            var mgr  = new WheelManager();
            mgr.Discover(root.transform);
            mgr.ApplySuspension(700f, 41f, 350f, 29f);

            foreach (var w in mgr.Front)
            {
                Assert.AreEqual(700f, w.SpringStrength, k_Eps, "Front spring strength");
                Assert.AreEqual(41f,  w.SpringDamping,  k_Eps, "Front spring damping");
            }
            foreach (var w in mgr.Rear)
            {
                Assert.AreEqual(350f, w.SpringStrength, k_Eps, "Rear spring strength");
                Assert.AreEqual(29f,  w.SpringDamping,  k_Eps, "Rear spring damping");
            }
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyTraction_SetsGripOnAllWheels()
        {
            var root = BuildHierarchy(2, 2);
            var mgr  = new WheelManager();
            mgr.Discover(root.transform);
            mgr.ApplyTraction(0.85f);

            foreach (var w in mgr.All)
                Assert.AreEqual(0.85f, w.GripCoeff, k_Eps, $"Wheel {w.name} grip");
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Discover_EmptyHierarchy_AllArraysEmpty()
        {
            var root = new GameObject("EmptyCar");
            var mgr  = new WheelManager();
            mgr.Discover(root.transform);

            Assert.IsNotNull(mgr.All);
            Assert.AreEqual(0, mgr.All.Length);
            Object.DestroyImmediate(root);
        }
    }
}
