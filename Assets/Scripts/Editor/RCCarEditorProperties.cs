#if UNITY_EDITOR
using UnityEditor;

namespace R8EOX.Editor
{
    /// <summary>
    /// Partial class — foldout key constants and SerializedProperty bindings for RCCarEditor.
    /// Extracted to keep each file under 150 lines.
    /// </summary>
    public partial class RCCarEditor : UnityEditor.Editor
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
            _motorPreset             = serializedObject.FindProperty("_motorPreset");
            _engineForceMax          = serializedObject.FindProperty("_engineForceMax");
            _maxSpeed                = serializedObject.FindProperty("_maxSpeed");
            _brakeForce              = serializedObject.FindProperty("_brakeForce");
            _reverseForce            = serializedObject.FindProperty("_reverseForce");
            _coastDrag               = serializedObject.FindProperty("_coastDrag");
            _throttleRampUp          = serializedObject.FindProperty("_throttleRampUp");
            _throttleRampDown        = serializedObject.FindProperty("_throttleRampDown");
            _steeringMax             = serializedObject.FindProperty("_steeringMax");
            _steeringSpeed           = serializedObject.FindProperty("_steeringSpeed");
            _steeringSpeedLimit      = serializedObject.FindProperty("_steeringSpeedLimit");
            _steeringHighSpeedFactor = serializedObject.FindProperty("_steeringHighSpeedFactor");
            _springStrength          = serializedObject.FindProperty("_springStrength");
            _springDamping           = serializedObject.FindProperty("_springDamping");
            _gripCoeff               = serializedObject.FindProperty("_gripCoeff");
            _comGround               = serializedObject.FindProperty("_comGround");
            _tumbleEngageDeg         = serializedObject.FindProperty("_tumbleEngageDeg");
            _tumbleFullDeg           = serializedObject.FindProperty("_tumbleFullDeg");
            _tumbleBounce            = serializedObject.FindProperty("_tumbleBounce");
            _tumbleFriction          = serializedObject.FindProperty("_tumbleFriction");
            _tumbleHysteresisDeg     = serializedObject.FindProperty("_tumbleHysteresisDeg");
        }
    }
}
#endif
