#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using R8EOX.Vehicle;
using R8EOX.Camera;
using R8EOX.Debug;

namespace R8EOX.Editor
{
    /// <summary>
    /// Editor menu item that builds the complete test scene and RC Buggy prefab.
    /// Use: Menu -> RC Buggy -> Build Test Scene
    /// </summary>
    public static class SceneSetup
    {
        // ---- Constants ----

        const string k_ScenePath = "Assets/Scenes/TestTrack.unity";
        const string k_PrefabPath = "Assets/Prefabs/RCBuggy.prefab";
        const float k_FixedTimestep = 0.008333f; // 120 Hz
        const int k_SolverIterations = 8;
        const int k_SolverVelocityIterations = 4;
        const float k_DefaultCarHeight = 0.5f;
        const float k_GroundSize = 200f;
        const int k_DefaultCarLayer = 8;


        // ---- Menu Items ----

        [MenuItem("RC Buggy/Build Test Scene")]
        static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigurePhysics();

            GameObject car = BuildRCBuggy();
            car.transform.position = new Vector3(0f, k_DefaultCarHeight, 0f);

            BuildTestTrack();
            SetupCamera(car);

            GameObject hudGO = new GameObject("TelemetryHUD");
            var hud = hudGO.AddComponent<TelemetryHUD>();
            // TelemetryHUD._car is [SerializeField] private — set via SerializedObject
            var so = new SerializedObject(hud);
            so.FindProperty("_car").objectReferenceValue = car.GetComponent<RCCar>();
            so.ApplyModifiedProperties();

            SetupLighting();

            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(car, k_PrefabPath);

            UnityEngine.Debug.Log("[SceneSetup] Test scene and prefab created successfully!");
            UnityEngine.Debug.Log($"  Scene: {k_ScenePath}");
            UnityEngine.Debug.Log($"  Prefab: {k_PrefabPath}");
        }

        [MenuItem("RC Buggy/Configure Physics Settings")]
        static void ConfigurePhysics()
        {
            Time.fixedDeltaTime = k_FixedTimestep;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.defaultSolverIterations = k_SolverIterations;
            Physics.defaultSolverVelocityIterations = k_SolverVelocityIterations;

            UnityEngine.Debug.Log("[SceneSetup] Physics configured: 120Hz, gravity=-9.81, " +
                                  $"solver={k_SolverIterations}/{k_SolverVelocityIterations}");
        }


        // ---- Builder Methods ----

        static GameObject BuildRCBuggy()
        {
            GameObject root = new GameObject("RCBuggy");

            int carLayer = LayerMask.NameToLayer("Vehicle");
            if (carLayer < 0) carLayer = k_DefaultCarLayer;
            root.layer = carLayer;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 15f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            root.AddComponent<R8EOX.Input.RCInput>();
            root.AddComponent<RCCar>();

            AddBoxCollider(root, new Vector3(1.4f, 0.3f, 3.9f), new Vector3(0f, -0.425f, 0f));
            AddBoxCollider(root, new Vector3(1.2f, 0.7f, 2.5f), new Vector3(0f, 0.075f, 0f));
            AddBoxCollider(root, new Vector3(1.1f, 0.35f, 0.4f), new Vector3(0f, -0.25f, 2.15f));
            AddBoxCollider(root, new Vector3(0.9f, 0.45f, 0.45f), new Vector3(0f, -0.2f, -2.15f));

            Material darkGrey = CreateMaterial("DarkGrey", new Color(0.2f, 0.2f, 0.2f));
            Material medGrey = CreateMaterial("MediumGrey", new Color(0.5f, 0.5f, 0.5f));
            Material blueSemi = CreateMaterial("BlueSemi", new Color(0.18f, 0.45f, 0.9f, 0.85f), true);
            Material blueSolid = CreateMaterial("BlueSolid", new Color(0.18f, 0.45f, 0.9f));
            Material tireMat = CreateMaterial("BlackTire", new Color(0.05f, 0.05f, 0.05f));
            Material hubMat = CreateMaterial("WhiteHub", new Color(0.9f, 0.9f, 0.9f));

            AddBoxMesh(root, "ChassisPlate", new Vector3(1.3f, 0.08f, 3.4f), new Vector3(0f, -0.5825f, 0f), darkGrey);
            AddBoxMesh(root, "FrontBumperMesh", new Vector3(1.2f, 0.3f, 0.25f), new Vector3(0f, -0.3825f, 1.95f), darkGrey);
            AddBoxMesh(root, "RearBumperMesh", new Vector3(1.0f, 0.4f, 0.4f), new Vector3(0f, -0.3825f, -1.8f), darkGrey);
            AddBoxMesh(root, "FrontShockTower", new Vector3(1.0f, 0.6f, 0.05f), new Vector3(0f, -0.1825f, 1.2f), medGrey);
            AddBoxMesh(root, "RearShockTower", new Vector3(0.8f, 0.6f, 0.05f), new Vector3(0f, -0.1825f, -1.2f), medGrey);
            AddBoxMesh(root, "BodyShell", new Vector3(1.2f, 0.4f, 2.8f), new Vector3(0f, -0.125f, 0.2f), blueSemi);

            GameObject wing = AddBoxMesh(root, "RearWing", new Vector3(1.2f, 0.02f, 0.4f), new Vector3(0f, 0.4175f, -1.5f), blueSolid);
            wing.transform.localRotation = Quaternion.Euler(-22.5f, 0f, 0f);

            AddBoxMesh(root, "FrontArmL", new Vector3(0.65f, 0.05f, 0.2f), new Vector3(-0.65f, -0.5325f, 1.7f), darkGrey);
            AddBoxMesh(root, "FrontArmR", new Vector3(0.65f, 0.05f, 0.2f), new Vector3(0.65f, -0.5325f, 1.7f), darkGrey);
            AddBoxMesh(root, "RearArmL", new Vector3(0.65f, 0.05f, 0.2f), new Vector3(-0.65f, -0.5325f, -1.7f), darkGrey);
            AddBoxMesh(root, "RearArmR", new Vector3(0.65f, 0.05f, 0.2f), new Vector3(0.65f, -0.5325f, -1.7f), darkGrey);

            BuildWheel(root, "WheelFL", new Vector3(-1.25f, 0f, 1.7f), true, false, 1.66f, 1.12f, 0.275f, 1.28f, tireMat, hubMat, carLayer);
            BuildWheel(root, "WheelFR", new Vector3(1.25f, 0f, 1.7f), true, false, 1.66f, 1.12f, 0.275f, 1.28f, tireMat, hubMat, carLayer);
            BuildWheel(root, "WheelRL", new Vector3(-1.25f, 0f, -1.7f), false, true, 1.66f, 1.68f, 0.275f, 1.84f, tireMat, hubMat, carLayer);
            BuildWheel(root, "WheelRR", new Vector3(1.25f, 0f, -1.7f), false, true, 1.66f, 1.68f, 0.275f, 1.84f, tireMat, hubMat, carLayer);

            GameObject airPhysGO = new GameObject("AirPhysics");
            airPhysGO.transform.SetParent(root.transform, false);
            airPhysGO.AddComponent<RCAirPhysics>();

            GameObject drivetrainGO = new GameObject("Drivetrain");
            drivetrainGO.transform.SetParent(root.transform, false);
            drivetrainGO.AddComponent<Drivetrain>();

            SetLayerRecursive(root, carLayer);
            return root;
        }

        static void BuildWheel(GameObject parent, string name, Vector3 localPos,
            bool isSteer, bool isMotor,
            float tireRadius, float tireHeight, float hubRadius, float hubHeight,
            Material tireMat, Material hubMat, int layer)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent.transform, false);
            pivot.transform.localPosition = localPos;
            pivot.layer = layer;

            var wheel = pivot.AddComponent<RaycastWheel>();
            wheel.IsSteer = isSteer;
            wheel.IsMotor = isMotor;
            wheel.SpringStrength = 75f;
            wheel.SpringDamping = 4.25f;
            wheel.RestDistance = 2.0f;
            // _overExtend and _minSpringLen are serialized fields — set via SerializedObject
            var so = new SerializedObject(wheel);
            so.FindProperty("_overExtend").floatValue = 0.8f;
            so.FindProperty("_minSpringLen").floatValue = 0.32f;
            so.ApplyModifiedProperties();

            GameObject tireGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tireGO.name = "WheelVisual";
            tireGO.transform.SetParent(pivot.transform, false);
            tireGO.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            tireGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tireGO.transform.localScale = new Vector3(tireRadius * 2f, tireHeight * 0.5f, tireRadius * 2f);
            tireGO.GetComponent<Renderer>().material = tireMat;
            Object.DestroyImmediate(tireGO.GetComponent<Collider>());
            tireGO.layer = layer;

            GameObject hubGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hubGO.name = "HubVisual";
            hubGO.transform.SetParent(pivot.transform, false);
            hubGO.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            hubGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hubGO.transform.localScale = new Vector3(hubRadius * 2f, hubHeight * 0.5f, hubRadius * 2f);
            hubGO.GetComponent<Renderer>().material = hubMat;
            Object.DestroyImmediate(hubGO.GetComponent<Collider>());
            hubGO.layer = layer;
        }

        static void BuildTestTrack()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(k_GroundSize, 1f, k_GroundSize);
            ground.GetComponent<Renderer>().material = CreateMaterial("Ground", new Color(0.4f, 0.45f, 0.4f));
            ground.isStatic = true;

            BuildRamp("SmallRamp", new Vector3(0f, 0f, 20f), 3f, 0.5f);
            BuildRamp("LargeRamp", new Vector3(15f, 0f, 20f), 5f, 1.2f);

            BuildObstacle("Box1", new Vector3(5f, 0.5f, 10f), new Vector3(1f, 1f, 1f));
            BuildObstacle("Box2", new Vector3(-8f, 0.25f, 15f), new Vector3(0.5f, 0.5f, 2f));
            BuildObstacle("Box3", new Vector3(10f, 0.75f, -5f), new Vector3(1.5f, 1.5f, 0.5f));
            BuildObstacle("Box4", new Vector3(-5f, 0.4f, -15f), new Vector3(0.8f, 0.8f, 0.8f));
            BuildObstacle("Box5", new Vector3(20f, 0.3f, 5f), new Vector3(2f, 0.6f, 0.6f));
        }

        static void BuildRamp(string name, Vector3 position, float length, float height)
        {
            float surfaceLen = Mathf.Sqrt(length * length + height * height);
            float slopeAngle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            float width = 3f;
            float thickness = 0.1f;

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.isStatic = true;
            ramp.transform.localScale = new Vector3(width, thickness, surfaceLen);
            ramp.transform.position = position + new Vector3(0f, height * 0.5f, length * 0.5f);
            ramp.transform.rotation = Quaternion.Euler(-slopeAngle, 0f, 0f);
            ramp.GetComponent<Renderer>().material = CreateMaterial("Ramp", new Color(0.6f, 0.55f, 0.5f));
        }

        static void BuildObstacle(string name, Vector3 position, Vector3 scale)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.isStatic = true;
            obj.GetComponent<Renderer>().material = CreateMaterial("Obstacle", new Color(0.7f, 0.3f, 0.2f));
        }

        static void SetupCamera(GameObject car)
        {
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                mainCam = camGO.AddComponent<UnityEngine.Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.tag = "MainCamera";
            }

            mainCam.transform.position = new Vector3(0f, 2f, -5f);
            var cameraController = mainCam.gameObject.AddComponent<CameraController>();
            cameraController.Target = car.transform;
        }

        static void SetupLighting()
        {
            Light dirLight = Object.FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lightGO = new GameObject("Directional Light");
                dirLight = lightGO.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            dirLight.intensity = 1.2f;
            dirLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
        }


        // ---- Utility Methods ----

        static void AddBoxCollider(GameObject parent, Vector3 size, Vector3 center)
        {
            var col = parent.AddComponent<BoxCollider>();
            col.size = size;
            col.center = center;
        }

        static GameObject AddBoxMesh(GameObject parent, string name, Vector3 size, Vector3 localPos, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = name;
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            return mat;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}
#endif