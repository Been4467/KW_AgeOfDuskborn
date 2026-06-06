using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KW
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }
        public static bool isDialogueActive { get; private set; }

        // UI 요소 — InitializeUI()에서 NpcCanvasUI를 통해 코드로 연결, Inspector 직접 할당 X
        private GameObject dialoguePanel;
        private TextMeshProUGUI dialogueText;
        private Button nextBtn;
        private Transform choiceBtnHolder;
        private GameObject choiceBtnPrefab;

        [HideInInspector] public NpcCanvasUI npcCanvas;

        // 씬에 종속된 Canvas들 — 씬 전환마다 재탐색
        private GameObject _INGAMECANVAS;
        private GameObject _SYSTEMCANVAS;

        private Queue<string> _dialogueQueue;
        private DialogueSO _currentDialogueData;
        private Npc _currentNpc;

        // ─────────────────────────────────────────
        // 생명주기
        // ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _dialogueQueue = new Queue<string>();
            isDialogueActive = false;
        }

        private void Start()
        {
            // 최초 실행 씬에서도 UI 연결
            // Awake 타이밍에는 같은 씬의 다른 오브젝트가 아직 초기화 전일 수 있으므로 Start에서 호출
            InitializeUI();
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
            // 진짜 인스턴스만 실행
            if (Instance != this) return;

            // Start()에서 이미 최초 1회 실행했으므로
            // OnSceneLoaded는 씬 전환 시에만 실질적으로 동작
            InitializeUI();
        }

        // ─────────────────────────────────────────
        // UI 초기화 — 씬 전환마다 + 최초 실행 시 호출
        // ─────────────────────────────────────────

        private void InitializeUI()
        {
            // 기존 참조 초기화 (Unity Missing 참조 방지)
            dialoguePanel = null;
            dialogueText = null;
            nextBtn = null;
            choiceBtnHolder = null;
            choiceBtnPrefab = null;
            npcCanvas = null;
            _INGAMECANVAS = null;
            _SYSTEMCANVAS = null;

            // NpcCanvasUI 탐색
            npcCanvas = FindObjectOfType<NpcCanvasUI>(true);

            Debug.Log($"[DialogueManager] InitializeUI 호출 — 씬: {SceneManager.GetActiveScene().name}, " +
                      $"npcCanvas: {(npcCanvas == null ? "NULL" : npcCanvas.gameObject.name)}");

            if (npcCanvas != null)
            {
                dialoguePanel = npcCanvas.dialoguePanel;
                dialogueText = npcCanvas.dialogueText;
                nextBtn = npcCanvas.nextBtn;
                choiceBtnHolder = npcCanvas.choiceBtnHolder;
                choiceBtnPrefab = npcCanvas.choiceBtnPrefab;

                if (dialoguePanel != null)
                    dialoguePanel.SetActive(false);

                // nextBtn 리스너는 반드시 이 한 곳에서만 등록
                if (nextBtn != null)
                {
                    nextBtn.onClick.RemoveAllListeners();
                    nextBtn.onClick.AddListener(DisplayNextLine);
                }
            }
            else
            {
                Debug.LogError($"[DialogueManager] NpcCanvasUI를 찾을 수 없습니다. " +
                               $"씬 '{SceneManager.GetActiveScene().name}'에 NpcCanvasUI가 있는지 확인하세요.");
            }

            // 씬 종속 Canvas 탐색
            var inGameUI = FindObjectOfType<InGameUI>(true);
            if (inGameUI != null) _INGAMECANVAS = inGameUI.gameObject;

            var systemUI = FindObjectOfType<SystemCanvasUI>(true);
            if (systemUI != null) _SYSTEMCANVAS = systemUI.gameObject;

            Debug.Log($"[DialogueManager] InitializeUI 완료 — " +
                      $"dialoguePanel: {(dialoguePanel == null ? "NULL" : "OK")}, " +
                      $"nextBtn: {(nextBtn == null ? "NULL" : "OK")}, " +
                      $"InGameCanvas: {(_INGAMECANVAS == null ? "NULL" : "OK")}");
        }

        // ─────────────────────────────────────────
        // 외부 등록 함수
        // ─────────────────────────────────────────

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
            if (canvas == null) return;

            npcCanvas = canvas;
            dialoguePanel = npcCanvas.dialoguePanel;
            dialogueText = npcCanvas.dialogueText;
            choiceBtnHolder = npcCanvas.choiceBtnHolder;
            choiceBtnPrefab = npcCanvas.choiceBtnPrefab;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // nextBtn 리스너 재등록
            nextBtn = npcCanvas.nextBtn;
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(DisplayNextLine);
            }
        }

        // ─────────────────────────────────────────
        // 대화 로직
        // ─────────────────────────────────────────

        public void StartDialogue(DialogueSO dialogueData, Npc npc)
        {
            if (dialoguePanel == null)
            {
                Debug.LogError("[DialogueManager] StartDialogue 실패: dialoguePanel이 null입니다. " +
                               "InitializeUI가 정상적으로 실행됐는지 확인하세요.");
                return;
            }

            if (dialogueData == null)
            {
                Debug.LogError("[DialogueManager] StartDialogue 실패: dialogueData가 null입니다. " +
                               "NPC Inspector에서 DialogueSO가 할당됐는지 확인하세요.");
                return;
            }

            dialoguePanel.SetActive(true);
            isDialogueActive = true;

            if (_SYSTEMCANVAS != null) _SYSTEMCANVAS.SetActive(false);
            if (_INGAMECANVAS != null) _INGAMECANVAS.SetActive(false);

            Time.timeScale = 0.00001f;

            _currentNpc = npc;
            _currentDialogueData = dialogueData;
            _dialogueQueue.Clear();

            foreach (string line in dialogueData.dialogueLines)
                _dialogueQueue.Enqueue(line);

            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            if (nextBtn != null)
                nextBtn.gameObject.SetActive(true);

            if (_dialogueQueue.Count > 0)
            {
                string line = _dialogueQueue.Dequeue();
                if (dialogueText != null) dialogueText.text = line;
            }
            else
            {
                if (nextBtn != null)
                    nextBtn.gameObject.SetActive(false);

                DisplayChoices();
            }
        }

        private void DisplayChoices()
        {
            // 이전 선택지 버튼 제거
            if (choiceBtnHolder != null)
                foreach (Transform child in choiceBtnHolder)
                    Destroy(child.gameObject);

            if (_currentDialogueData != null && _currentDialogueData.dialogueChoices.Length > 0)
            {
                foreach (DialogueChoice choice in _currentDialogueData.dialogueChoices)
                {
                    if (choiceBtnPrefab == null || choiceBtnHolder == null) continue;

                    GameObject btnObj = Instantiate(choiceBtnPrefab, choiceBtnHolder);
                    btnObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

                    Button btn = btnObj.GetComponent<Button>();
                    DialogueChoice captured = choice; // 클로저 캡처 방지
                    btn.onClick.AddListener(() => OnChoiceSelected(captured));
                }
            }
            else
            {
                EndDialogue();
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            // 선택지 버튼 제거
            if (choiceBtnHolder != null)
                foreach (Transform child in choiceBtnHolder)
                    Destroy(child.gameObject);

            // NPC에게 선택 결과 전달 (상태 변경, 보상 지급 등)
            if (_currentNpc != null)
                _currentNpc.OnChoiceMade(choice);

            // 다음 대화가 있으면 이어서 시작, 없으면 종료
            if (choice.nextDialogue != null)
                StartDialogue(choice.nextDialogue, _currentNpc);
            else
                EndDialogue();
        }

        private void EndDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (_INGAMECANVAS != null) _INGAMECANVAS.SetActive(true);

            isDialogueActive = false;
            Time.timeScale = 1f;
            _currentNpc = null;
            _currentDialogueData = null;
        }
    }
}