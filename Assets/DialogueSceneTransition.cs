using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeneGames.DialogueSystem
{
    public class DialogueSceneTransition : MonoBehaviour
    {
        [Header("Scene Transition Settings")]
        [SerializeField] private string sceneToLoad;
        [SerializeField] private float delayBeforeTransition = 2f;

        public void TransitionToScene()
        {
            StartCoroutine(DelayedSceneTransition());
        }

        private IEnumerator DelayedSceneTransition()
        {
            yield return new WaitForSeconds(delayBeforeTransition);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}