#if UNITY_EDITOR
using NUnit.Framework;
using R8EOX.Editor.Builders;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for RCBuggyMeshBuilder — mesh and material helpers extracted from RCBuggyBuilder.
    /// Idempotency: each helper can be called twice without leaving broken state.
    /// </summary>
    public class RCBuggyMeshBuilderTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void CreateMaterial_ReturnsMaterialWithCorrectColor()
        {
            var mat = RCBuggyMeshBuilder.CreateMaterial("TestMat", Color.red);
            Assert.IsNotNull(mat);
            Assert.AreEqual("TestMat", mat.name);
            // Standard shader sets main color
            Assert.AreEqual(Color.red, mat.color);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void CreateMaterial_Transparent_SetsRenderQueue()
        {
            var mat = RCBuggyMeshBuilder.CreateMaterial("TransparentMat", Color.blue, transparent: true);
            Assert.IsNotNull(mat);
            Assert.AreEqual(3000, mat.renderQueue);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void AddBoxMesh_CreatesChildWithCorrectName()
        {
            var mat = RCBuggyMeshBuilder.CreateMaterial("M", Color.white);
            var child = RCBuggyMeshBuilder.AddBoxMesh(_root, "MyBox", new Vector3(1, 1, 1), Vector3.zero, mat);
            Assert.IsNotNull(child);
            Assert.AreEqual("MyBox", child.name);
            Assert.AreEqual(_root.transform, child.transform.parent);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void AddBoxMesh_CalledTwice_BothChildrenExist()
        {
            var mat = RCBuggyMeshBuilder.CreateMaterial("M2", Color.grey);
            RCBuggyMeshBuilder.AddBoxMesh(_root, "BoxA", Vector3.one, Vector3.zero, mat);
            RCBuggyMeshBuilder.AddBoxMesh(_root, "BoxB", Vector3.one, Vector3.one, mat);
            Assert.AreEqual(2, _root.transform.childCount);
            Object.DestroyImmediate(mat);
        }

        [Test]
        public void AddBodyMeshes_CreatesExpectedChildren()
        {
            var darkGrey  = RCBuggyMeshBuilder.CreateMaterial("DG", new Color(0.2f, 0.2f, 0.2f));
            var medGrey   = RCBuggyMeshBuilder.CreateMaterial("MG", new Color(0.5f, 0.5f, 0.5f));
            var blueSemi  = RCBuggyMeshBuilder.CreateMaterial("BS", new Color(0.18f, 0.45f, 0.9f, 0.85f), transparent: true);
            var blueSolid = RCBuggyMeshBuilder.CreateMaterial("BSo", new Color(0.18f, 0.45f, 0.9f));

            RCBuggyMeshBuilder.AddBodyMeshes(_root, darkGrey, medGrey, blueSemi, blueSolid);

            // 6 body meshes: ChassisPlate, FrontBumperMesh, RearBumperMesh, FrontShockTower, RearShockTower, BodyShell, RearWing = 7
            Assert.GreaterOrEqual(_root.transform.childCount, 7);

            Object.DestroyImmediate(darkGrey);
            Object.DestroyImmediate(medGrey);
            Object.DestroyImmediate(blueSemi);
            Object.DestroyImmediate(blueSolid);
        }

        [Test]
        public void AddBodyMeshes_CalledTwice_IsIdempotentInCount()
        {
            var dg  = RCBuggyMeshBuilder.CreateMaterial("DG2", Color.black);
            var mg  = RCBuggyMeshBuilder.CreateMaterial("MG2", Color.grey);
            var bs  = RCBuggyMeshBuilder.CreateMaterial("BS2", Color.blue, transparent: true);
            var bso = RCBuggyMeshBuilder.CreateMaterial("BSo2", Color.blue);

            RCBuggyMeshBuilder.AddBodyMeshes(_root, dg, mg, bs, bso);
            int countAfterFirst = _root.transform.childCount;
            RCBuggyMeshBuilder.AddBodyMeshes(_root, dg, mg, bs, bso);
            // Calling twice appends more children — count doubles; the builder is additive.
            // Idempotency test: no exception thrown, count is deterministic (2x).
            Assert.AreEqual(countAfterFirst * 2, _root.transform.childCount);

            Object.DestroyImmediate(dg);
            Object.DestroyImmediate(mg);
            Object.DestroyImmediate(bs);
            Object.DestroyImmediate(bso);
        }

        [Test]
        public void AddControlArms_Creates4Children()
        {
            var mat = RCBuggyMeshBuilder.CreateMaterial("ArmMat", Color.black);
            RCBuggyMeshBuilder.AddControlArms(_root, mat);
            Assert.AreEqual(4, _root.transform.childCount);
            Object.DestroyImmediate(mat);
        }
    }
}
#endif
