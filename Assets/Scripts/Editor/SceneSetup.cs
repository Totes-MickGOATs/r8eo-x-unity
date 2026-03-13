#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor menu item that builds the complete test scene and RC Buggy prefab.
/// Use: Menu → RC Buggy → Build Test Scene
/// </summary>
public class SceneSetup
{
    [MenuItem("RC Buggy/Build Test Scene")]
    static void BuildTestScene()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Configure physics
        ConfigurePhysics();

        // Build car
        GameObject car = BuildRCBuggy();
        car.transform.position = new Vector3(0f, 0.5f, 0f);

        // Build environment
        BuildTestTrack();

        // Camera
        SetupCamera(car);

        // Telemetry HUD
        GameObject hudGO = new GameObject("TelemetryHUD");
        var hud = hudGO.AddComponent<TelemetryHUD>();
        hud.car = car.GetComponent<RCCar>();

        // Lighting
        SetupLighting();

        // Save scene
        string scenePath = "Assets/Scenes/TestTrack.unity";
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);

        // Save car as prefab
        string prefabPath = "Assets/Prefabs/RCBuggy.prefab";
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(car, prefabPath);

        Debug.Log("[SceneSetup] Test scene and prefab created successfully!");
        Debug.Log($"  Scene: {scenePath}");
        Debug.Log($"  Prefab: {prefabPath}");
    }

    [MenuItem("RC Buggy/Configure Physics Settings")]
    static void ConfigurePhysics()
    {
        // These are also set in ProjectSettings files, but this menu item
        // ensures they're applied if settings were overwritten
        Time.fixedDeltaTime = 0.008333f;
        Physics.gravity = new Vector3(0f, -9.81f, 0f);
        Physics.defaultSolverIterations = 8;
        Physics.defaultSolverVelocityIterations = 4;

        Debug.Log("[SceneSetup] Physics configured: 120Hz, gravity=-9.81, solver=8/4");
        Debug.Log("[SceneSetup] These settings are also pre-configured in ProjectSettings/");
    }

    static GameObject BuildRCBuggy()
    {
        // -- Root --
        GameObject root = new GameObject("RCBuggy");
        root.layer = LayerMask.NameToLayer("Default");

        // Try to use a dedicated layer for the car
        int carLayer = LayerMask.NameToLayer("Vehicle");
        if (carLayer < 0) carLayer = 8; // Use layer 8 if "Vehicle" doesn't exist
        root.layer = carLayer;

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        root.AddComponent<RCInput>();
        root.AddComponent<RCCar>();

        // -- Collision Shapes --
        AddBoxCollider(root, "ChassisSlab", new Vector3(0.56f, 0.12f, 1.56f), new Vector3(0f, -0.17f, 0f));
        AddBoxCollider(root, "BodyShell", new Vector3(0.48f, 0.28f, 1.0f), new Vector3(0f, 0.03f, 0f));
        AddBoxCollider(root, "FrontBumper", new Vector3(0.44f, 0.14f, 0.16f), new Vector3(0f, -0.1f, 0.86f));
        AddBoxCollider(root, "RearBumper", new Vector3(0.36f, 0.18f, 0.18f), new Vector3(0f, -0.08f, -0.86f));

        // -- Visual Meshes --
        // Chassis plate
        AddBoxMesh(root, "ChassisPlate",
            new Vector3(0.52f, 0.032f, 1.36f), new Vector3(0f, -0.233f, 0f),
            CreateMaterial("DarkGrey", new Color(0.2f, 0.2f, 0.2f)));

        // Front bumper mesh
        AddBoxMesh(root, "FrontBumperMesh",
            new Vector3(0.48f, 0.12f, 0.1f), new Vector3(0f, -0.153f, 0.78f),
            CreateMaterial("DarkGrey", new Color(0.2f, 0.2f, 0.2f)));

        // Rear bumper mesh
        AddBoxMesh(root, "RearBumperMesh",
            new Vector3(0.4f, 0.16f, 0.16f), new Vector3(0f, -0.153f, -0.72f),
            CreateMaterial("DarkGrey", new Color(0.2f, 0.2f, 0.2f)));

        // Shock towers
        AddBoxMesh(root, "FrontShockTower",
            new Vector3(0.4f, 0.24f, 0.02f), new Vector3(0f, -0.073f, 0.48f),
            CreateMaterial("MediumGrey", new Color(0.5f, 0.5f, 0.5f)));
        AddBoxMesh(root, "RearShockTower",
            new Vector3(0.32f, 0.24f, 0.02f), new Vector3(0f, -0.073f, -0.48f),
            CreateMaterial("MediumGrey", new Color(0.5f, 0.5f, 0.5f)));

        // Body shell (semi-transparent blue)
        Material blueSemi = CreateMaterial("BlueSemi", new Color(0.18f, 0.45f, 0.9f, 0.85f), true);
        AddBoxMesh(root, "BodyShell",
            new Vector3(0.48f, 0.16f, 1.12f), new Vector3(0f, -0.05f, 0.08f),
            blueSemi);

        // Rear wing (tilted ~22.5 deg)
        Material blueSolid = CreateMaterial("BlueSolid", new Color(0.18f, 0.45f, 0.9f));
        GameObject wing = AddBoxMesh(root, "RearWing",
            new Vector3(0.48f, 0.008f, 0.16f), new Vector3(0f, 0.167f, -0.6f),
            blueSolid);
        wing.transform.localRotation = Quaternion.Euler(-22.5f, 0f, 0f);

        // Control arms
        Material darkGrey = CreateMaterial("DarkGrey", new Color(0.2f, 0.2f, 0.2f));
        AddBoxMesh(root, "FrontArmL", new Vector3(0.26f, 0.02f, 0.08f), new Vector3(-0.26f, -0.213f, 0.68f), darkGrey);
        AddBoxMesh(root, "FrontArmR", new Vector3(0.26f, 0.02f, 0.08f), new Vector3(0.26f, -0.213f, 0.68f), darkGrey);
        AddBoxMesh(root, "RearArmL", new Vector3(0.26f, 0.02f, 0.08f), new Vector3(-0.26f, -0.213f, -0.68f), darkGrey);
        AddBoxMesh(root, "RearArmR", new Vector3(0.26f, 0.02f, 0.08f), new Vector3(0.26f, -0.213f, -0.68f), darkGrey);

        // -- Wheels (Z flipped from Godot) --
        Material tireMat = CreateMaterial("BlackTire", new Color(0.05f, 0.05f, 0.05f));
        Material hubMat = CreateMaterial("WhiteHub", new Color(0.9f, 0.9f, 0.9f));

        // FL: front-left (steer)
        BuildWheel(root, "WheelFL", new Vector3(-0.5f, 0f, 0.68f),
            isSteer: true, isMotor: false,
            tireRadius: 0.166f, tireHeight: 0.112f,
            hubRadius: 0.11f, hubHeight: 0.128f,
            tireMat, hubMat, carLayer);

        // FR: front-right (steer)
        BuildWheel(root, "WheelFR", new Vector3(0.5f, 0f, 0.68f),
            isSteer: true, isMotor: false,
            tireRadius: 0.166f, tireHeight: 0.112f,
            hubRadius: 0.11f, hubHeight: 0.128f,
            tireMat, hubMat, carLayer);

        // RL: rear-left (motor)
        BuildWheel(root, "WheelRL", new Vector3(-0.5f, 0f, -0.68f),
            isSteer: false, isMotor: true,
            tireRadius: 0.166f, tireHeight: 0.168f,
            hubRadius: 0.11f, hubHeight: 0.184f,
            tireMat, hubMat, carLayer);

        // RR: rear-right (motor)
        BuildWheel(root, "WheelRR", new Vector3(0.5f, 0f, -0.68f),
            isSteer: false, isMotor: true,
            tireRadius: 0.166f, tireHeight: 0.168f,
            hubRadius: 0.11f, hubHeight: 0.184f,
            tireMat, hubMat, carLayer);

        // -- Air Physics + Drivetrain --
        GameObject airPhysGO = new GameObject("AirPhysics");
        airPhysGO.transform.SetParent(root.transform, false);
        airPhysGO.AddComponent<RCAirPhysics>();

        GameObject drivetrainGO = new GameObject("Drivetrain");
        drivetrainGO.transform.SetParent(root.transform, false);
        var dt = drivetrainGO.AddComponent<Drivetrain>();
        dt.driveLayout = Drivetrain.DriveLayout.RWD;
        dt.rearDiffType = Drivetrain.DiffType.Open; // Scene override from rc_buggy.tscn

        // Set all children to car layer
        SetLayerRecursive(root, carLayer);

        return root;
    }

    static void BuildWheel(GameObject parent, string name, Vector3 localPos,
        bool isSteer, bool isMotor,
        float tireRadius, float tireHeight,
        float hubRadius, float hubHeight,
        Material tireMat, Material hubMat, int layer)
    {
        // Wheel pivot (RaycastWheel component lives here)
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent.transform, false);
        pivot.transform.localPosition = localPos;
        pivot.layer = layer;

        var wheel = pivot.AddComponent<RaycastWheel>();
        wheel.isSteer = isSteer;
        wheel.isMotor = isMotor;
        wheel.wheelRadius = tireRadius;

        // Tire visual (cylinder rotated 90° around Z to lie on its side)
        GameObject tireGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tireGO.name = "WheelVisual";
        tireGO.transform.SetParent(pivot.transform, false);
        tireGO.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        tireGO.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        tireGO.transform.localScale = new Vector3(tireRadius * 2f, tireHeight * 0.5f, tireRadius * 2f);
        tireGO.GetComponent<Renderer>().material = tireMat;
        Object.DestroyImmediate(tireGO.GetComponent<Collider>()); // Remove auto-generated collider
        tireGO.layer = layer;

        // Hub visual
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
        // Ground plane (200x200)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(200f, 1f, 200f);
        ground.GetComponent<Renderer>().material = CreateMaterial("Ground", new Color(0.4f, 0.45f, 0.4f));
        ground.isStatic = true;

        // Small ramp
        BuildRamp("SmallRamp", new Vector3(0f, 0f, 20f), 3f, 0.5f);

        // Large ramp
        BuildRamp("LargeRamp", new Vector3(15f, 0f, 20f), 5f, 1.2f);

        // Scattered obstacles
        BuildObstacle("Box1", new Vector3(5f, 0.5f, 10f), new Vector3(1f, 1f, 1f));
        BuildObstacle("Box2", new Vector3(-8f, 0.25f, 15f), new Vector3(0.5f, 0.5f, 2f));
        BuildObstacle("Box3", new Vector3(10f, 0.75f, -5f), new Vector3(1.5f, 1.5f, 0.5f));
        BuildObstacle("Box4", new Vector3(-5f, 0.4f, -15f), new Vector3(0.8f, 0.8f, 0.8f));
        BuildObstacle("Box5", new Vector3(20f, 0.3f, 5f), new Vector3(2f, 0.6f, 0.6f));
    }

    static void BuildRamp(string name, Vector3 position, float length, float height)
    {
        // Create a ramp as a thin tilted slab
        // The slab lies along the slope from ground to (length, height)
        float surfaceLen = Mathf.Sqrt(length * length + height * height);
        float slopeAngle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
        float width = 3f;
        float thickness = 0.1f;

        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = name;
        ramp.isStatic = true;
        ramp.transform.localScale = new Vector3(width, thickness, surfaceLen);

        // Position: center of the slope surface
        // The slab center needs to be at (position.x, height/2, position.z + length/2)
        // Then rotated around X by slopeAngle so front edge touches ground
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
        // Find or create main camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        mainCam.transform.position = new Vector3(0f, 2f, -5f);
        var chase = mainCam.gameObject.AddComponent<ChaseCamera>();
        chase.target = car.transform;
    }

    static void SetupLighting()
    {
        // Find existing directional light or create one
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

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
    }

    static void AddBoxCollider(GameObject parent, string name, Vector3 size, Vector3 center)
    {
        // Add collider directly to parent with offset
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
        Object.DestroyImmediate(go.GetComponent<Collider>()); // Visual only
        return go;
    }

    static Material CreateMaterial(string name, Color color, bool transparent = false)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        if (transparent)
        {
            mat.SetFloat("_Mode", 3); // Transparent
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
#endif
