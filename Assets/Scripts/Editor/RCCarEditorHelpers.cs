#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace R8EOX.Editor
{
    /// <summary>
    /// Static drawing helpers shared by RCCarEditor.
    /// Keeps the main editor class under 200 lines.
    /// </summary>
    internal static class RCCarEditorHelpers
    {
        /// <summary>
        /// Session-persistent foldout backed by <see cref="SessionState"/>.
        /// Returns the expanded state after toggling.
        /// </summary>
        internal static bool Foldout(string key, string label, bool defaultOpen)
        {
            bool current = SessionState.GetBool(key, defaultOpen);
            bool next    = EditorGUILayout.Foldout(current, label, true, EditorStyles.foldoutHeader);
            if (next != current)
                SessionState.SetBool(key, next);
            return next;
        }

        /// <summary>
        /// Draws an editable field in human-friendly display units alongside a
        /// grayed-out read-only label showing the raw internal value.
        /// Layout: [ displayValue ] displayUnit  ░ internalValue internalUnit
        /// </summary>
        internal static void UnitField(
            SerializedProperty prop,
            string label,
            string displayUnit,
            string internalUnit,
            Func<float, float> toDisplay,
            Func<float, float> toInternal)
        {
            float displayVal = toDisplay(prop.floatValue);
            EditorGUILayout.BeginHorizontal();
            float newDisplay = EditorGUILayout.FloatField(new GUIContent(label), displayVal);
            GUILayout.Label(displayUnit, GUILayout.Width(36));
            GUI.enabled = false;
            EditorGUILayout.FloatField(prop.floatValue, GUILayout.Width(60));
            GUI.enabled = true;
            GUILayout.Label(internalUnit, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
            if (!Mathf.Approximately(newDisplay, displayVal))
                prop.floatValue = toInternal(newDisplay);
        }
    }
}
#endif
