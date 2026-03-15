#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Editor
{
    /// <summary>
    /// Custom inspector for Drivetrain. Hides Front and Center differential
    /// sections when the drive layout is RWD, disables preload fields unless
    /// the selected diff type is BallDiff, and shows a human-readable torque
    /// split label for the center front-bias slider.
    /// </summary>
    [CustomEditor(typeof(Drivetrain))]
    public class DrivetrainEditor : UnityEditor.Editor
    {
        // ---- Session-persistent foldout keys ----

        const string k_FoldLayout  = "DrivetrainEditor.FoldLayout";
        const string k_FoldRear    = "DrivetrainEditor.FoldRear";
        const string k_FoldFront   = "DrivetrainEditor.FoldFront";
        const string k_FoldCenter  = "DrivetrainEditor.FoldCenter";


        // ---- Cached SerializedProperties ----

        SerializedProperty _driveLayout;
        SerializedProperty _rearDiffType;
        SerializedProperty _rearPreload;
        SerializedProperty _frontDiffType;
        SerializedProperty _frontPreload;
        SerializedProperty _centerDiffType;
        SerializedProperty _centerPreload;
        SerializedProperty _centerFrontBias;


        void OnEnable()
        {
            _driveLayout     = serializedObject.FindProperty("_driveLayout");
            _rearDiffType    = serializedObject.FindProperty("_rearDiffType");
            _rearPreload     = serializedObject.FindProperty("_rearPreload");
            _frontDiffType   = serializedObject.FindProperty("_frontDiffType");
            _frontPreload    = serializedObject.FindProperty("_frontPreload");
            _centerDiffType  = serializedObject.FindProperty("_centerDiffType");
            _centerPreload   = serializedObject.FindProperty("_centerPreload");
            _centerFrontBias = serializedObject.FindProperty("_centerFrontBias");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool isRWD = _driveLayout.enumValueIndex == (int)Drivetrain.DriveLayout.RWD;

            // ---- Drive Layout ----
            bool foldLayout = Foldout(k_FoldLayout, "Drive Layout", true);
            if (foldLayout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_driveLayout, new GUIContent("Drive Layout"));
                EditorGUI.indentLevel--;
            }

            // ---- Rear Differential (always visible) ----
            bool foldRear = Foldout(k_FoldRear, "Rear Differential", true);
            if (foldRear)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_rearDiffType, new GUIContent("Diff Type"));
                bool rearIsBallDiff = _rearDiffType.enumValueIndex == (int)Drivetrain.DiffType.BallDiff;
                EditorGUI.BeginDisabledGroup(!rearIsBallDiff);
                EditorGUILayout.PropertyField(_rearPreload, new GUIContent("Preload (N)"));
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            // ---- Front Differential (hidden when RWD) ----
            if (!isRWD)
            {
                bool foldFront = Foldout(k_FoldFront, "Front Differential", true);
                if (foldFront)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_frontDiffType, new GUIContent("Diff Type"));
                    bool frontIsBallDiff = _frontDiffType.enumValueIndex == (int)Drivetrain.DiffType.BallDiff;
                    EditorGUI.BeginDisabledGroup(!frontIsBallDiff);
                    EditorGUILayout.PropertyField(_frontPreload, new GUIContent("Preload (N)"));
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }
            }

            // ---- Center Differential (hidden when RWD) ----
            if (!isRWD)
            {
                bool foldCenter = Foldout(k_FoldCenter, "Center Differential", true);
                if (foldCenter)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_centerDiffType, new GUIContent("Diff Type"));
                    bool centerIsBallDiff = _centerDiffType.enumValueIndex == (int)Drivetrain.DiffType.BallDiff;
                    EditorGUI.BeginDisabledGroup(!centerIsBallDiff);
                    EditorGUILayout.PropertyField(_centerPreload, new GUIContent("Preload (N)"));
                    EditorGUI.EndDisabledGroup();

                    float bias = _centerFrontBias.floatValue;
                    int frontPct = Mathf.RoundToInt(bias * 100f);
                    int rearPct  = 100 - frontPct;
                    EditorGUILayout.Slider(_centerFrontBias, 0f, 1f,
                        new GUIContent("Front Bias", $"{frontPct}% front / {rearPct}% rear"));
                    EditorGUILayout.LabelField(" ", $"{frontPct}% front / {rearPct}% rear",
                        EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
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
