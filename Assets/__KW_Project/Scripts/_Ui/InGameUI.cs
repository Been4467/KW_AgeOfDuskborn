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

            RegisterToManagers();

          /*   if (UiManager.Instance != null)
            {
                UiManager.Instance.RegisterIngameCanvas(this.gameObject);
            }
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ingameUi = this;
            }
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.RegisterIngameCanvas(this.gameObject);
            } */

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
            if (scene.name == "LoadingScene") return;

            RegisterToManagers();
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
