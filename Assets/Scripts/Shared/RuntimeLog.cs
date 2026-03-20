using UnityEngine;

namespace R8EOX.Shared
{
    /// <summary>
    /// Runtime-safe logging facade. Only active in Editor and Development builds.
    /// Stripped automatically in production builds via [Conditional] attributes.
    /// Use this instead of UnityEngine.Debug.Log* in all runtime assemblies.
    /// </summary>
    public static class RuntimeLog
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }
    }
}
