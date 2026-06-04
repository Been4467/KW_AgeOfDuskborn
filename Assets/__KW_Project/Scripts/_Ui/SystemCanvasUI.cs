using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    public class SystemCanvasUI : MonoBehaviour
    {
        public static SystemCanvasUI Instance { get; private set; }

        [SerializeField] private GameObject gameMode;
        [SerializeField] private GameObject restMode;

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
                return; // 👈 가짜 오브젝트의 하위 실행을 완벽히 차단
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 🛠️ 핵심 수정: 파괴되기 직전의 가짜 복제본이 다른 매니저들을 오염시키는 것을 절대 차단!
            if (Instance != this) return;

            RegisterToManagers();
        }

        // 등록 로직 분리
        private void RegisterToManagers()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.RegisterSystemCanvas(this.gameObject);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterSystemCanvas(this.gameObject);
            }

            // Debug.Log("[SystemCanvasUI] 진짜 인스턴스가 매니저들에 재등록 완료");
        }

        public void ToggleGameMode(bool gameModeToToggle)
        {
            if (gameMode != null)
                gameMode.SetActive(gameModeToToggle);
        }

        public void ToggleRestMode(bool restModeToToggle)
        {
            if (restMode != null)
                restMode.SetActive(restModeToToggle);
        }
    }
}