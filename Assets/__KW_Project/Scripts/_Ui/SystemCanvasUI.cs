using Unity.VisualScripting;
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
                return;
            }
            /*    if (UiManager.Instance != null)
             {
                 UiManager.Instance.RegisterSystemCanvas(this.gameObject);
             }

             if (DialogueManager.Instance != null)
             {
                 DialogueManager.Instance.RegisterSystemCanvas(this.gameObject);
             } */

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
            RegisterToManagers();
        }

        // [핵심] 등록 로직 분리
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
            
            // Debug.Log("[SystemCanvasUI] 매니저들에 재등록 완료");
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
