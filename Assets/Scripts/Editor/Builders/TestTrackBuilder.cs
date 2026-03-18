#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Constructs the test track: ground plane, ramps, obstacles, lighting, and camera.
    /// </summary>
    internal static class TestTrackBuilder
    {
        // ---- Constants ----

        const float k_GroundSize = 200f;

        // ---- Public API ----

        /// <summary>
        /// Creates the ground plane, ramps, and obstacle boxes.
        /// </summary>
        internal static void BuildTestTrack()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(k_GroundSize, 1f, k_GroundSize);
            ground.GetComponent<Renderer>().material = CreateMaterial("Ground", new Color(0.4f, 0.45f, 0.4f));
            ground.isStatic = true;

            BuildRamp("SmallRamp", new Vector3(0f,  0f, 20f), 3f, 0.5f);
            BuildRamp("LargeRamp", new Vector3(15f, 0f, 20f), 5f, 1.2f);

            BuildObstacle("Box1", new Vector3(  5f, 0.50f,  10f), new Vector3(1.0f, 1.0f, 1.0f));
            BuildObstacle("Box2", new Vector3( -8f, 0.25f,  15f), new Vector3(0.5f, 0.5f, 2.0f));
            BuildObstacle("Box3", new Vector3( 10f, 0.75f,  -5f), new Vector3(1.5f, 1.5f, 0.5f));
            BuildObstacle("Box4", new Vector3( -5f, 0.40f, -15f), new Vector3(0.8f, 0.8f, 0.8f));
            BuildObstacle("Box5", new Vector3( 20f, 0.30f,   5f), new Vector3(2.0f, 0.6f, 0.6f));
        }

        /// <summary>
        /// Configures or creates the directional light.
        /// </summary>
        internal static void SetupLighting()
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
        }

        /// <summary>
        /// Positions the main camera and attaches a CameraController targeting the car.
        /// </summary>
        internal static void SetupCamera(GameObject car)
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
            var cameraController = mainCam.gameObject.AddComponent<R8EOX.Camera.CameraController>();
            cameraController.Target = car.transform;
        }

        // ---- Private Helpers ----

        static void BuildRamp(string name, Vector3 position, float length, float height)
        {
            float surfaceLen = Mathf.Sqrt(length * length + height * height);
            float slopeAngle = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            const float width = 3f;
            const float thickness = 0.1f;

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

        static Material CreateMaterial(string name, Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = name;
            mat.color = color;
            return mat;
        }
    }
}
#endif
