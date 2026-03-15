#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Editor
{
    /// <summary>
    /// Custom inspector for RCCar. Provides collapsible foldout groups,
    /// conditional visibility for preset-driven fields, range sliders for
    /// bounded parameters, and a degree conversion label for steering angle.
    /// </summary>
    [CustomEditor(typeof(RCCar))]
    public class RCCarEditor : UnityEditor.Editor
    {
        // ---- Session-persistent foldout keys ----

        const string k_FoldMotorPreset = "RCCarEditor.FoldMotorPreset";
        const string k_FoldEngine      = "RCCarEditor.FoldEngine";
        const string k_FoldThrottle    = "RCCarEditor.FoldThrottle";
        const string k_FoldSteering    = "RCCarEditor.FoldSteering";
        const string k_FoldSuspension  = "RCCarEditor.FoldSuspension";
        const string k_FoldTraction    = "RCCarEditor.FoldTraction";
        const string k_FoldCoM         = "RCCarEditor.FoldCoM";
        const string k_FoldCrash       = "RCCarEditor.FoldCrash";


        // ---- Cached SerializedProperties ----

        SerializedProperty _motorPreset;
        SerializedProperty _engineForceMax;
        SerializedProperty _maxSpeed;
        SerializedProperty _brakeForce;
        SerializedProperty _reverseForce;
        SerializedProperty _coastDrag;
        SerializedProperty _throttleRampUp;
        SerializedProperty _throttleRampDown;
        SerializedProperty _steeringMax;
        SerializedProperty _steeringSpeed;
        SerializedProperty _steeringSpeedLimit;
        SerializedProperty _steeringHighSpeedFactor;
        SerializedProperty _springStrength;
        SerializedProperty _springDamping;
        SerializedProperty _gripCoeff;
        SerializedProperty _comGround;
        SerializedProperty _tumbleEngageDeg;
        SerializedProperty _tumbleFullDeg;
        SerializedProperty _tumbleBounce;
        SerializedProperty _tumbleFriction;
        SerializedProperty _tumbleHysteresisDeg;


        void OnEnable()
        {
            _motorPreset          = serializedObject.FindProperty("_motorPreset");
            _engineForceMax       = serializedObject.FindProperty("_engineForceMax");
            _maxSpeed             = serializedObject.FindProperty("_maxSpeed");
            _brakeForce           = serializedObject.FindProperty("_brakeForce");
            _reverseForce         = serializedObject.FindProperty("_reverseForce");
            _coastDrag            = serializedObject.FindProperty("_coastDrag");
            _throttleRampUp       = serializedObject.FindProperty("_throttleRampUp");
            _throttleRampDown     = serializedObject.FindProperty("_throttleRampDown");
            _steeringMax          = serializedObject.FindProperty("_steeringMax");
            _steeringSpeed        = serializedObject.FindProperty("_steeringSpeed");
            _steeringSpeedLimit   = serializedObject.FindProperty("_steeringSpeedLimit");
            _steeringHighSpeedFactor = serializedObject.FindProperty("_steeringHighSpeedFactor");
            _springStrength       = serializedObject.FindProperty("_springStrength");
            _springDamping        = serializedObject.FindProperty("_springDamping");
            _gripCoeff            = serializedObject.FindProperty("_gripCoeff");
            _comGround            = serializedObject.FindProperty("_comGround");
            _tumbleEngageDeg      = serializedObject.FindProperty("_tumbleEngageDeg");
            _tumbleFullDeg        = serializedObject.FindProperty("_tumbleFullDeg");
            _tumbleBounce         = serializedObject.FindProperty("_tumbleBounce");
            _tumbleFriction       = serializedObject.FindProperty("_tumbleFriction");
            _tumbleHysteresisDeg  = serializedObject.FindProperty("_tumbleHysteresisDeg");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool isCustom = _motorPreset.enumValueIndex == (int)RCCar.MotorPreset.Custom;

            // ---- Motor Preset ----
            bool foldMotorPreset = Foldout(k_FoldMotorPreset, "Motor Preset", true);
            if (foldMotorPreset)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_motorPreset);
                if (!isCustom)
                    EditorGUILayout.HelpBox(
                        "Engine and Throttle RampUp values are overridden by this preset at Start().\n" +
                        "Select Custom to tune manually.",
                        MessageType.Info);
                EditorGUI.indentLevel--;
            }

            // ---- Engine ----
            bool foldEngine = Foldout(k_FoldEngine, "Engine", true);
            if (foldEngine)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!isCustom);
                EditorGUILayout.PropertyField(_engineForceMax,  new GUIContent("Engine Force Max (N)"));
                EditorGUILayout.PropertyField(_maxSpeed,        new GUIContent("Max Speed (m/s)"));
                EditorGUILayout.PropertyField(_brakeForce,      new GUIContent("Brake Force (N)"));
                EditorGUILayout.PropertyField(_reverseForce,    new GUIContent("Reverse Force (N)"));
                EditorGUILayout.PropertyField(_coastDrag,       new GUIContent("Coast Drag (N)"));
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            // ---- Throttle Response ----
            bool foldThrottle = Foldout(k_FoldThrottle, "Throttle Response", true);
            if (foldThrottle)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!isCustom);
                EditorGUILayout.PropertyField(_throttleRampUp,   new GUIContent("Ramp Up (units/s)"));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(_throttleRampDown, new GUIContent("Ramp Down (units/s)"));
                EditorGUI.indentLevel--;
            }

            // ---- Steering ----
            bool foldSteering = Foldout(k_FoldSteering, "Steering", true);
            if (foldSteering)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_steeringMax, new GUIContent("Steering Max (rad)"));
                float deg = _steeringMax.floatValue * Mathf.Rad2Deg;
                EditorGUILayout.LabelField(" ", $"= {deg:F1}°", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(_steeringSpeed,      new GUIContent("Steering Speed (rad/s)"));
                EditorGUILayout.PropertyField(_steeringSpeedLimit, new GUIContent("Speed Limit (m/s)"));
                EditorGUILayout.Slider(_steeringHighSpeedFactor,   0f, 1f,
                    new GUIContent("High-Speed Factor", "Fraction of max steering angle retained at high speed"));
                EditorGUI.indentLevel--;
            }

            // ---- Suspension ----
            bool foldSuspension = Foldout(k_FoldSuspension, "Suspension", true);
            if (foldSuspension)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_springStrength, new GUIContent("Spring Strength (N/m)"));
                EditorGUILayout.PropertyField(_springDamping,  new GUIContent("Spring Damping"));
                EditorGUI.indentLevel--;
            }

            // ---- Traction ----
            bool foldTraction = Foldout(k_FoldTraction, "Traction", true);
            if (foldTraction)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(_gripCoeff, 0f, 1f, new GUIContent("Grip Coefficient"));
                EditorGUI.indentLevel--;
            }

            // ---- Centre of Mass ----
            bool foldCoM = Foldout(k_FoldCoM, "Centre of Mass", true);
            if (foldCoM)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_comGround, new GUIContent("CoM Offset"));
                EditorGUI.indentLevel--;
            }

            // ---- Crash Physics ----
            bool foldCrash = Foldout(k_FoldCrash, "Crash Physics", true);
            if (foldCrash)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_tumbleEngageDeg,     new GUIContent("Tumble Engage (deg)"));
                EditorGUILayout.PropertyField(_tumbleFullDeg,       new GUIContent("Tumble Full (deg)"));
                EditorGUILayout.Slider(_tumbleBounce,   0f, 1f, new GUIContent("Tumble Bounce"));
                EditorGUILayout.Slider(_tumbleFriction, 0f, 1f, new GUIContent("Tumble Friction"));
                EditorGUILayout.PropertyField(_tumbleHysteresisDeg, new GUIContent("Hysteresis (deg)"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }


        // ---- Helpers ----

        static bool Foldout(string key, string label, bool defaultOpen)
        {
            bool current = SessionState.GetBool(key, defaultOpen);
            bool next    = EditorGUILayout.Foldout(current, label, true, EditorStyles.foldoutHeader);
            if (next != current)
                SessionState.SetBool(key, next);
            return next;
        }
    }
}
#endif
