using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace R8EOX.Editor.Tests
{
    public class BuggyMaterialsTests
    {
        const string k_PrefabPath = "Assets/Prefabs/RCBuggy.prefab";

        [Test]
        public void ApplyBuggyMaterials_BodyShellHasBlueSemiMaterial()
        {
            AddBuggyMaterials.Apply();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
            var bodyShell = prefab.transform.Find("BodyShell");
            Assert.IsNotNull(bodyShell, "BodyShell child not found");
            var renderer = bodyShell.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, "BodyShell has no Renderer");
            Assert.IsNotNull(renderer.sharedMaterial, "BodyShell material is null");
            Assert.AreEqual("BlueSemi", renderer.sharedMaterial.name);
        }

        [Test]
        public void ApplyBuggyMaterials_WheelVisualHasBlackTireMaterial()
        {
            AddBuggyMaterials.Apply();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
            var wheelFL = prefab.transform.Find("WheelFL");
            Assert.IsNotNull(wheelFL, "WheelFL not found");
            var tireVisual = wheelFL.Find("WheelVisual");
            Assert.IsNotNull(tireVisual, "WheelVisual not found under WheelFL");
            var renderer = tireVisual.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, "WheelVisual has no Renderer");
            Assert.IsNotNull(renderer.sharedMaterial, "WheelVisual material is null");
            Assert.AreEqual("BlackTire", renderer.sharedMaterial.name);
        }

        [Test]
        public void ApplyBuggyMaterials_CalledTwice_NoErrors()
        {
            LogAssert.NoUnexpectedReceived();
            AddBuggyMaterials.Apply();
            AddBuggyMaterials.Apply();
        }
    }
}
