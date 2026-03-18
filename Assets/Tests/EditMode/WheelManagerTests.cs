using NUnit.Framework;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public class WheelManagerTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
            _root.AddComponent<Rigidbody>().useGravity = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private RaycastWheel CreateWheel(string name, float z)
        {
            var go = new GameObject(name);
            go.transform.parent = _root.transform;
            go.transform.localPosition = new Vector3(0f, 0f, z);
            return go.AddComponent<RaycastWheel>();
        }

        [Test]
        public void Discover_FourWheels_SplitsFrontRear()
        {
            CreateWheel("FL", 0.15f); CreateWheel("FR", 0.15f);
            CreateWheel("RL", -0.15f); CreateWheel("RR", -0.15f);

            var wm = new WheelManager();
            wm.Discover(_root.transform);

            Assert.AreEqual(4, wm.All.Length);
            Assert.AreEqual(2, wm.Front.Length);
            Assert.AreEqual(2, wm.Rear.Length);
        }

        [Test]
        public void PushSuspension_UpdatesFrontAndRearWheels()
        {
            CreateWheel("FL", 0.15f); CreateWheel("FR", 0.15f);
            CreateWheel("RL", -0.15f); CreateWheel("RR", -0.15f);

            var wm = new WheelManager();
            wm.Discover(_root.transform);
            wm.PushSuspension(700f, 41f, 350f, 29f);

            foreach (var w in wm.Front)
            {
                Assert.AreEqual(700f, w.SpringStrength, 0.001f);
                Assert.AreEqual(41f,  w.SpringDamping,  0.001f);
            }
            foreach (var w in wm.Rear)
            {
                Assert.AreEqual(350f, w.SpringStrength, 0.001f);
                Assert.AreEqual(29f,  w.SpringDamping,  0.001f);
            }
        }

        [Test]
        public void PushGrip_UpdatesAllWheels()
        {
            CreateWheel("FL", 0.15f); CreateWheel("RL", -0.15f);

            var wm = new WheelManager();
            wm.Discover(_root.transform);
            wm.PushGrip(0.85f);

            foreach (var w in wm.All)
                Assert.AreEqual(0.85f, w.GripCoeff, 0.001f);
        }

        [Test]
        public void AnyOnGround_NoWheels_ReturnsFalse()
        {
            var wm = new WheelManager();
            wm.Discover(_root.transform);
            Assert.IsFalse(wm.AnyOnGround());
        }

        [Test]
        public void Discover_NoWheels_AllArraysEmpty()
        {
            var wm = new WheelManager();
            wm.Discover(_root.transform);
            Assert.AreEqual(0, wm.All.Length);
            Assert.AreEqual(0, wm.Front.Length);
            Assert.AreEqual(0, wm.Rear.Length);
        }
    }
}
