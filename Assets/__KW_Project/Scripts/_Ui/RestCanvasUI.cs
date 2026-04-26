using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace KW
{
    public class RestCanvasUI : MonoBehaviour
    {
        public static RestCanvasUI Instance { get; private set; }

        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 1.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            RegisterToManagers();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "LoadingScene") return;

            RegisterToManagers();
        }

        private void RegisterToManagers()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.RegisterRestCanvas(this.gameObject);
            }

            //if (NotificationManager.Instance != null)
            //{
            //    NotificationManager.Instance.relaxCanvas = this;

            //    NotificationManager.Instance.FindPopupHolder();
            //}

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterRestCanvas(this.gameObject);
            }
        }

        public void DoFadeIn(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0, 1, onComplete));
        }

        public void DoFadeOut(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1, 0, onComplete));
        }

        private IEnumerator FadeRoutine(float start, float end, Action onComplete)
        {
            float timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(start, end, timer / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = end;
            onComplete?.Invoke();
        }

    }
}
