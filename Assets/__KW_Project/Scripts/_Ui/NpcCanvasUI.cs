using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KW
{
    public class NpcCanvasUI : MonoBehaviour
    {
        public static NpcCanvasUI Instance { get; private set; }


        [Header("대화 UI 요소들")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI dialogueText;
        public Button nextBtn;
        public Transform choiceBtnHolder;
        public GameObject choiceBtnPrefab;


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
            RegisterToDialogueManager();
            
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
            Instance = null;
        }

        // 씬 로딩이 끝나면 무조건 실행됨 -> 다시 등록!
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterToDialogueManager();
        }
        

        // 등록 로직 분리 (재사용을 위해)
        private void RegisterToDialogueManager()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.npcCanvas = this;
                // Debug.Log("[NpcCanvasUI] DialogueManager에 재등록 완료!");
            }

            // 씬 바뀌면 대화창 꺼두기 (안전장치)
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        // Start는 이제 필요 없지만, 혹시 모르니 놔둬도 됨a
        private void Start()
        {
            // RegisterToDialogueManager();
        }


    }
}
