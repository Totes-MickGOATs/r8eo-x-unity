#if UNITY_EDITOR || DEBUG
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Editor startup and domain-unload lifecycle hooks for <see cref="AuditDb"/>.
    /// Extracted to keep <see cref="AuditDb"/> focused on connection management and queries.
    /// </summary>
    internal static class AuditDbLifecycle
    {
#if UNITY_EDITOR
        /// <summary>
        /// Registers post-domain-load purge and domain-unload cleanup with Unity Editor.
        /// Called automatically by <see cref="AuditDb.OnEditorStartup"/>.
        /// </summary>
        internal static void RegisterEditorCallbacks()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    AuditDb.Initialize();
                    AuditDb.Purge();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[AuditDb] Editor startup purge failed: {ex.Message}");
                }
            };

            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        private static void OnDomainUnload(object sender, EventArgs e)
            => AuditDb.CloseConnection();
#endif
    }
}
#endif
