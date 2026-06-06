using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Image 사용을 위해 추가

namespace KW
{
    public class InGameUI : MonoBehaviour
    {
        public static InGameUI Instance { get; private set; }

        [Header("화톳불 발견 연출 이미지")]
        [Tooltip("LostBonfireDiscoverd 하위의 Image 오브젝트를 연결해 주세요.")]
        public Image bonfireDiscoveryImage; // ← Bonfire들이 공유할 Image 참조

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
                Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "LoadingScene" || scene.name == "TitleScene" || scene.buildIndex == 0) return;

            if (Instance == this)
                RegisterToManagers();
        }

        private void RegisterToManagers()
        {
            if (UiManager.Instance != null)
                UiManager.Instance.RegisterIngameCanvas(this.gameObject);

            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ingameUi = this;
                NotificationManager.Instance.FindPopupHolder();
            }

            if (DialogueManager.Instance != null)
                DialogueManager.Instance.RegisterIngameCanvas(this.gameObject);
        }
    }
}