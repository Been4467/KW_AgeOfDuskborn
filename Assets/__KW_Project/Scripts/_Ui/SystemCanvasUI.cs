using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    public class SystemCanvasUI : MonoBehaviour
    {
        public static SystemCanvasUI Instance { get; private set; }

        [SerializeField] private GameObject fade;
        [SerializeField] private GameObject restScreen;

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

        public void ToggleCanvas(bool canvasToToggle)
        {
            foreach (Transform child in this.transform)
                child.gameObject.SetActive(canvasToToggle);
        }

        public void ToggleFade(bool fadeToToggle)
        {
            if (fade != null)
                fade.SetActive(fadeToToggle);
        }

        public void ToggleRestScreen(bool restScreenToToggle)
        {
            if (restScreen != null)
                restScreen.SetActive(restScreenToToggle);
        }
    }
}
