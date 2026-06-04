using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KW
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; set; }
        public static bool isDialogueActive { get; private set; }

        [Header("UI 요소")]
        [SerializeField] private GameObject dialoguePanel;                       // 대화창 
        [SerializeField] private TextMeshProUGUI dialogueText;                   // 대화가 표시될 텍스트
        [SerializeField] private Button nextBtn;                                 // 다음 버튼

        [SerializeField] private Transform choiceBtnHolder;
        [SerializeField] private GameObject choiceBtnPrefab;
        [HideInInspector] public NpcCanvasUI npcCanvas;
        [HideInInspector] public InGameUI ingameUi;
        [HideInInspector] public SystemCanvasUI systemCanvasUi;

        [SerializeField] private GameObject _INGAMECANVAS;
        [SerializeField] private GameObject _SYSTEMCANVAS;

        private Queue<string> _dialogueQueue;                               // 각 NPC 의 dialogue 를 보관할 Queue
        private DialogueSO _currentDialogueData;                            // 현재 대사 (DialogueSO 스크립터블 오브젝트 형식)
        private Npc currentNpc;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return; // 🛠️ 중요: 가짜 오브젝트가 아래 로직을 실행하지 못하도록 즉시 탈출합니다.
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // this 대신 gameObject가 명확합니다.
            }

            _dialogueQueue = new Queue<string>();
            isDialogueActive = false;
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
            // 🛠️ 중요 안전장치: 오직 '진짜 인스턴스'만 새 씬의 UI를 초기화하도록 제한합니다.
            if (Instance == this)
            {
                InitializeUI();
            }
        }

        private void InitializeUI()
        {
            // 🛠️ 유니티의 Missing 참조 특성 때문에 씬 이동 후에는 강제로 새로 찾도록 기존 참조를 비워줍니다.
            npcCanvas = null;
            _INGAMECANVAS = null;
            _SYSTEMCANVAS = null;

            // 새로운 씬에서 오브젝트들을 다시 탐색
            npcCanvas = FindObjectOfType<NpcCanvasUI>(true);

            var inGameUIScript = FindObjectOfType<InGameUI>(true);
            if (inGameUIScript != null) _INGAMECANVAS = inGameUIScript.gameObject;

            var systemCanvasUIScript = FindObjectOfType<SystemCanvasUI>(true);
            if (systemCanvasUIScript != null) _SYSTEMCANVAS = systemCanvasUIScript.gameObject;

            // npcCanvas 가 존재할 시, npcCanvas의 각 변수 할당
            if (npcCanvas != null)
            {
                dialoguePanel = npcCanvas.dialoguePanel;
                dialogueText = npcCanvas.dialogueText;
                nextBtn = npcCanvas.nextBtn;
                choiceBtnHolder = npcCanvas.choiceBtnHolder;
                choiceBtnPrefab = npcCanvas.choiceBtnPrefab;

                if (dialoguePanel != null) dialoguePanel.SetActive(false);
            }
            else
            {
                Debug.LogError("[DialogueManager] NPC Canvas를 찾을 수 없음");
            }

            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(DisplayNextLine);
            }
        }

        public void RegisterSystemCanvas(GameObject canvasObj)
        {
            _SYSTEMCANVAS = canvasObj;
        }

        public void RegisterIngameCanvas(GameObject canvasObj)
        {
            _INGAMECANVAS = canvasObj;
        }

        public void RegisterNpcCanvas(NpcCanvasUI canvas)
        {
            npcCanvas = canvas;

            if (npcCanvas != null)
            {
                dialoguePanel = npcCanvas.dialoguePanel;
                dialogueText = npcCanvas.dialogueText;
                nextBtn = npcCanvas.nextBtn;
                choiceBtnHolder = npcCanvas.choiceBtnHolder;
                choiceBtnPrefab = npcCanvas.choiceBtnPrefab;

                if (npcCanvas.dialoguePanel != null) npcCanvas.dialoguePanel.SetActive(false);

                if (npcCanvas.nextBtn != null)
                {
                    npcCanvas.nextBtn.onClick.RemoveAllListeners();
                    npcCanvas.nextBtn.onClick.AddListener(DisplayNextLine);
                }

                Debug.Log("[DialogueManager] NpcCanvas 등록 완료!");
            }
        }

        public void StartDialogue(DialogueSO dialogueData, Npc npc)
        {
            if (dialoguePanel == null) return;

            dialoguePanel.SetActive(true);
            isDialogueActive = true;

            // 🛠️ 안전장치: Null 체크 추가
            if (_SYSTEMCANVAS != null) _SYSTEMCANVAS.SetActive(false);
            if (_INGAMECANVAS != null) _INGAMECANVAS.SetActive(false);

            Time.timeScale = 0.00001f;

            currentNpc = npc;
            _currentDialogueData = dialogueData;
            _dialogueQueue.Clear();

            foreach (string line in dialogueData.dialogueLines)
            {
                _dialogueQueue.Enqueue(line);
            }

            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            if (nextBtn != null)
            {
                nextBtn.gameObject.SetActive(true);
            }

            if (_dialogueQueue.Count > 0)
            {
                string line = _dialogueQueue.Dequeue();
                if (dialogueText != null) dialogueText.text = line;
            }
            else
            {
                if (nextBtn != null)
                {
                    nextBtn.gameObject.SetActive(false);
                }
                DisplayChoices();
            }
        }

        private void DisplayChoices()
        {
            if (_currentDialogueData != null && _currentDialogueData.dialogueChoices.Length > 0)
            {
                if (choiceBtnHolder != null)
                {
                    foreach (Transform child in choiceBtnHolder)
                    {
                        Destroy(child.gameObject);
                    }
                }

                foreach (DialogueChoice choice in _currentDialogueData.dialogueChoices)
                {
                    if (choiceBtnPrefab != null && choiceBtnHolder != null)
                    {
                        GameObject buttonObj = Instantiate(choiceBtnPrefab, choiceBtnHolder);
                        buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

                        Button button = buttonObj.GetComponent<Button>();
                        button.onClick.AddListener(() =>
                        {
                            OnChoiceSelected(choice);
                        });
                    }
                }
            }
            else
            {
                EndDialogue();
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (choiceBtnHolder != null)
            {
                foreach (Transform child in choiceBtnHolder)
                {
                    Destroy(child.gameObject);
                }
            }

            if (currentNpc != null)
            {
                currentNpc.OnChoiceMade(choice);
            }

            if (choice.nextDialogue != null)
            {
                StartDialogue(choice.nextDialogue, currentNpc);
            }
            else
            {
                EndDialogue();
            }
        }

        private void EndDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (_INGAMECANVAS != null) _INGAMECANVAS.SetActive(true);

            isDialogueActive = false;
            Time.timeScale = 1f;

            currentNpc = null;
            _currentDialogueData = null;
        }
    }
}