using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaFall.Core
{
    /// <summary>
    /// Safety launcher for player builds. The source Boot scene is intentionally minimal,
    /// so this ensures a device build always proceeds to the generated main menu.
    /// </summary>
    public sealed class ArenaFallRuntimeLauncher : MonoBehaviour
    {
        private static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (_started || SceneManager.GetActiveScene().name != "Boot")
                return;

            _started = true;
            var launcher = new GameObject("[AUTO] ArenaFall Runtime Launcher");
            DontDestroyOnLoad(launcher);
            launcher.AddComponent<ArenaFallRuntimeLauncher>();
        }

        private IEnumerator Start()
        {
            // Let Unity finish activating the initial scene and its runtime builders.
            yield return null;
            yield return new WaitForSecondsRealtime(0.75f);

            if (SceneManager.GetActiveScene().name == "Boot")
            {
                Debug.Log("[ArenaFall] Boot scene fallback: loading MainMenu.");
                SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            }

            Destroy(gameObject);
        }
    }
}
