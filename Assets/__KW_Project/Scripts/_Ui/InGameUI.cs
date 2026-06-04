using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    public class InGameUI : MonoBehaviour
    {
        public static InGameUI Instance { get; private set; }

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

            // 진짜 인스턴스만 최초 1회 등록을 진행합니다.
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 🛠️ 보완: 로딩 씬뿐만 아니라 타이틀(0번 씬 등) 씬에서도 등록을 건너뜁니다.
            if (scene.name == "LoadingScene" || scene.name == "TitleScene" || scene.buildIndex == 0) return;

            // 🛠️ 중요 안전장치: 파괴될 가짜들이 등록하는 것을 막기 위해, 오직 '진짜 인스턴스'만 등록을 수행하도록 제한합니다.
            if (Instance == this)
            {
                RegisterToManagers();
            }
        }

        private void RegisterToManagers()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.RegisterIngameCanvas(this.gameObject);
            }

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ingameUi = this;
                NotificationManager.Instance.FindPopupHolder();
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterIngameCanvas(this.gameObject);
            }
        }
    }
}