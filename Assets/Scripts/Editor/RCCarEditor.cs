#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using R8EOX.Shared;
using R8EOX.Vehicle;

namespace R8EOX.Editor
{
    /// <summary>
    /// Custom inspector for RCCar. Provides collapsible foldout groups,
    /// conditional visibility for preset-driven fields, range sliders for
    /// bounded parameters, and a degree conversion label for steering angle.
    /// Property bindings and foldout keys live in RCCarEditorProperties.cs (partial).
    /// </summary>
    [CustomEditor(typeof(RCCar))]
    public partial class RCCarEditor : UnityEditor.Editor
    {
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
                UnitField(_engineForceMax, "Engine Force Max", "kgf", "N",
                    UnitConversion.NToKgf, UnitConversion.KgfToN);
                UnitField(_maxSpeed, "Max Speed", "km/h", "m/s",
                    UnitConversion.MsToKmh, UnitConversion.KmhToMs);
                UnitField(_brakeForce, "Brake Force", "kgf", "N",
                    UnitConversion.NToKgf, UnitConversion.KgfToN);
                UnitField(_reverseForce, "Reverse Force", "kgf", "N",
                    UnitConversion.NToKgf, UnitConversion.KgfToN);
                EditorGUILayout.PropertyField(_coastDrag, new GUIContent("Coast Drag (N)"));
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
                UnitField(_steeringMax, "Steering Max", "deg", "rad",
                    UnitConversion.RadToDeg, UnitConversion.DegToRad);
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
                UnitField(_springStrength, "Spring Strength", "N/mm", "N/m",
                    UnitConversion.NmToNmm, UnitConversion.NmmToNm);
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

        // ---- Helpers (static wrappers for RCCarEditorHelpers) ----

        static bool Foldout(string key, string label, bool defaultOpen) =>
            RCCarEditorHelpers.Foldout(key, label, defaultOpen);

        static void UnitField(SerializedProperty prop, string label, string displayUnit, string internalUnit,
            System.Func<float, float> toDisplay, System.Func<float, float> toInternal) =>
            RCCarEditorHelpers.UnitField(prop, label, displayUnit, internalUnit, toDisplay, toInternal);
    }
}
#endif
