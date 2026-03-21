namespace R8EOX.UI
{
    using System.Collections;
    using R8EOX.Shared;
    using UnityEngine;

    /// <summary>
    /// Partial class — screen transition coroutines and destroy helper for UIManager.
    /// Extracted to keep each file under 150 lines.
    /// </summary>
    public sealed partial class UIManager : MonoBehaviour
    {
        internal static void ExitAndDestroy(IScreen screen)
        {
            screen.Exit();
            if (screen is MonoBehaviour mb)
            {
                Destroy(mb.gameObject);
            }
        }

        private IEnumerator TransitionToScreen(GameObject prefab, string screenId, object data)
        {
            if (_activeScreen != null)
            {
                yield return _activeScreen.AnimateOut();
                ExitAndDestroy(_activeScreen);
            }

            var instance = Instantiate(prefab, _menuLayer);
            var screen = instance.GetComponent<IScreen>();
            if (screen == null)
            {
                RuntimeLog.LogError($"[UIManager] Prefab '{screenId}' has no IScreen component.");
                Destroy(instance);
                yield break;
            }

            _activeScreen = screen;
            screen.Enter(data);
            yield return screen.AnimateIn();
        }

        private IEnumerator PopOverlayRoutine(IScreen overlay)
        {
            yield return overlay.AnimateOut();
            ExitAndDestroy(overlay);
        }
    }
}
