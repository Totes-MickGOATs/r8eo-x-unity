using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TerrainStartupChecks — startup validation extracted from TerrainDiagnosticChecks.
    /// </summary>
    [TestFixture]
    public class TerrainStartupChecksTests
    {
        // ---- CheckCollisionDetectionMode ----

        [Test]
        public void CheckCollisionDetectionMode_NullRigidbody_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => TerrainStartupChecks.CheckCollisionDetectionMode(null, "TestCar"));
        }

        [Test]
        public void CheckCollisionDetectionMode_ContinuousSpeculative_NoWarning()
        {
            var go = new GameObject("TestCar");
            var rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // No warning expected — verify the call completes without exception
            Assert.DoesNotThrow(() => TerrainStartupChecks.CheckCollisionDetectionMode(rb, "TestCar"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CheckCollisionDetectionMode_DiscreteMode_LogsWarning()
        {
            var go = new GameObject("TestCar");
            var rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"\[physics\].*collision detection"));

            TerrainStartupChecks.CheckCollisionDetectionMode(rb, "TestCar");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CheckCollisionDetectionMode_ContinuousDynamic_NoWarning()
        {
            var go = new GameObject("TestCar");
            var rb = go.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // No warning expected — verify the call completes without exception
            Assert.DoesNotThrow(() => TerrainStartupChecks.CheckCollisionDetectionMode(rb, "TestCar"));

            Object.DestroyImmediate(go);
        }

        // ---- TerrainDiagnosticDrawing ----

        [Test]
        public void DrawContactNormal_ZeroNormal_DoesNotThrow()
        {
            var go = new GameObject("TestWheel");
            var wheel = go.AddComponent<R8EOX.Vehicle.RaycastWheel>();

            Assert.DoesNotThrow(() =>
                TerrainDiagnosticDrawing.DrawContactNormal(wheel, deviationThreshold: 0.85f, steepThreshold: 0.7f));

            Object.DestroyImmediate(go);
        }
    }
}
