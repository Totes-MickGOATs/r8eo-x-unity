using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Shared base fixture for terrain anti-snag PlayMode tests.
    ///
    /// Provides seamed-ground and flat-ground scene setup, vehicle spawning,
    /// and common timing helpers used by TerrainSeamTests and TerrainRegressionTests.
    ///
    /// Seamed ground simulates terrain triangle edge seams using a row of thin cubes
    /// with alternating height offsets. This reproduces the conditions under which
    /// Physics.Raycast produces discontinuous normals at edges.
    ///
    /// Reference: feat/terrain-anti-snag — SphereCast anti-snag fix + beveled colliders
    /// </summary>
    public abstract class TerrainTestFixture
    {
        // ---- Timing Constants ----

        /// <summary>Physics frames for settling (1s at 50Hz).</summary>
        protected const int k_SettleFrames = 60;
        /// <summary>Physics frames for measurement window (2.4s at 50Hz).</summary>
        protected const int k_MeasureFrames = 120;

        // ---- Seam Geometry Constants ----

        /// <summary>Number of seam slabs in the row (arranged along Z axis so vehicle drives over them).</summary>
        protected const int k_SeamSlabCount = 10;
        /// <summary>Width of each slab along the drive axis (Z), in metres.</summary>
        protected const float k_SlabDriveLength = 2f;
        /// <summary>Width of each slab across the drive axis (X), in metres.</summary>
        protected const float k_SlabCrossWidth = 10f;
        /// <summary>Thickness of each slab (m) — thin to expose triangle edges.</summary>
        protected const float k_SlabThickness = 0.1f;
        /// <summary>Height offset applied to alternating slabs (m).</summary>
        protected const float k_SeamOffset = 0.008f;

        // ---- Scene State ----

        protected List<GameObject> SeamedGround;
        protected GameObject FlatGround;
        protected GameObject Car;
        protected Rigidbody CarRb;
        protected R8EOX.Vehicle.RaycastWheel[] Wheels;


        // ---- Teardown ----

        protected void TearDownScene()
        {
            if (Car != null) Object.DestroyImmediate(Car);
            if (FlatGround != null) Object.DestroyImmediate(FlatGround);
            if (SeamedGround != null)
            {
                foreach (var slab in SeamedGround)
                    if (slab != null) Object.DestroyImmediate(slab);
                SeamedGround = null;
            }
            Car = null;
            FlatGround = null;
            CarRb = null;
            Wheels = null;
        }


        // ---- Ground Factories ----

        /// <summary>
        /// Creates a row of thin cubes with alternating height offsets arranged along
        /// the vehicle's drive axis (Z). The vehicle drives forward along Z and crosses
        /// each seam boundary, reproducing terrain triangle edge discontinuities.
        /// Slabs are wide in X so the car stays fully on the slab laterally.
        /// </summary>
        protected List<GameObject> CreateSeamedGround()
        {
            var slabs = new List<GameObject>();
            float totalLength = k_SeamSlabCount * k_SlabDriveLength;
            float startZ = -totalLength * 0.5f;

            for (int i = 0; i < k_SeamSlabCount; i++)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"SeamSlab_{i}";

                float slabCenterZ = startZ + i * k_SlabDriveLength + k_SlabDriveLength * 0.5f;
                float heightOffset = (i % 2 == 0) ? 0f : k_SeamOffset;

                slab.transform.position = new Vector3(
                    0f,
                    heightOffset - k_SlabThickness * 0.5f,
                    slabCenterZ);
                slab.transform.localScale = new Vector3(k_SlabCrossWidth, k_SlabThickness, k_SlabDriveLength);
                slab.layer = ConformanceSceneSetup.k_GroundLayer;
                slabs.Add(slab);
            }

            return slabs;
        }

        /// <summary>
        /// Spawns the test vehicle on seamed ground.
        /// Vehicle is placed above y=0 (the base surface level).
        /// </summary>
        protected void SpawnOnSeamedGround()
        {
            SeamedGround = CreateSeamedGround();
            // Spawn above seam surface — seams alternate between y=0 and y=k_SeamOffset
            Car = ConformanceSceneSetup.CreateTestVehicle(new Vector3(0f, 0.5f, 0f));
            CarRb = Car.GetComponent<Rigidbody>();
            Wheels = Car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>
        /// Spawns the test vehicle on a single flat ground cube (regression baseline).
        /// </summary>
        protected void SpawnOnFlatGround()
        {
            FlatGround = ConformanceSceneSetup.CreateGround();
            Car = ConformanceSceneSetup.CreateTestVehicle(new Vector3(0f, 0.5f, 0f));
            CarRb = Car.GetComponent<Rigidbody>();
            Wheels = Car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>Yields the given number of FixedUpdate frames.</summary>
        protected static IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }
    }
}
