using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceK
{
    /// <summary>Loads the configured gameplay scene from the build entry scene.</summary>
    public sealed class MainSceneLoader : MonoBehaviour
    {
        [SerializeField, Tooltip("Full scene asset path included in the build scene list.")]
        string gameplayScenePath;

        IEnumerator Start()
        {
            if (string.IsNullOrWhiteSpace(gameplayScenePath) ||
                gameplayScenePath == gameObject.scene.path ||
                !Application.CanStreamedLevelBeLoaded(gameplayScenePath))
            {
                Debug.LogError("Main needs a different gameplay scene included in the build scene list.", this);
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(gameplayScenePath, LoadSceneMode.Single);
        }
    }
}
