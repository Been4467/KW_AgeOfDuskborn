using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace KW
{
    public class DialogueManager : MonoBehaviour
    {
        // 대화 자체를 보관한 SO 만들기 ->
        // dialogue 를 띄울 매니저 스크립트 (안에는 대화 내용을 띄우는 함수,
        // 플레이어가 특정 버튼을 누르면 어떤 다음 dialogue 를 띄울지 정하는 함수 등) ->
        // npc 자체를 만들어 interact 가능한 object로 만들기
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

        // Queue 는 순차적으로 정보를 보여주는 형식의 List와 비슷한 것
        private Queue<string> _dialogueQueue;                           // 각 NPC 의 dialogue 를 보관할 Queue
        private DialogueSO _currentDialogueData;                        // 현재 대사 (DialogueSO 스크립터블 오브젝트 형식)

        private Npc currentNpc;

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }

            _dialogueQueue = new Queue<string>();
            isDialogueActive = false;
        }

       private void OnEnable()
        {
            // 씬이 로드될 때 OnSceneLoaded 를 구독함
            SceneManager.sceneLoaded += OnSceneLoaded;

        }

        private void OnDisable()
        {
            // 씬에서 나갈 때 OnSceneLoaded 를 구독취소함
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            Instance = null;
        }


        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeUI();         // 각종 UI 들을 변수로 등록해주는 함수
        }

        private void InitializeUI()
        {
            // <NpcCanvasUI> 형식의 오브젝트를 찾음. 비활성화되어있는 오브젝트까지 다 체크
            if (npcCanvas == null) npcCanvas = FindObjectOfType<NpcCanvasUI>(true);
           
            // <InGameUI> 와 <SystemCanvasUI> 형식의 오브젝트를 찾음. 비활성화되어있는 오브젝트까지 다 체크. gameObject로 선언되어있어서 .gameObject
            if (_INGAMECANVAS == null) _INGAMECANVAS = FindObjectOfType<InGameUI>(true).gameObject;
            if (_SYSTEMCANVAS == null) _SYSTEMCANVAS = FindObjectOfType<SystemCanvasUI>(true).gameObject;

            // npcCanvas 가 존재할 시, npcCanvas의 각 변수 할당
            if (npcCanvas != null)
            {
                dialoguePanel = npcCanvas.dialoguePanel;
                dialogueText = npcCanvas.dialogueText;
                nextBtn = npcCanvas.nextBtn;
                choiceBtnHolder = npcCanvas.choiceBtnHolder;
                choiceBtnPrefab = npcCanvas.choiceBtnPrefab;
            }
            else
            {
                // npcCanavs 를 찾을 수 없을 때
                Debug.LogError("[DialogueManager] NPC Canvas를 찾을 수 없음");
            }

            dialoguePanel.SetActive(false);

            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(DisplayNextLine);
            }
        }

        // DialogueMangaer에서 찾지 못했을 경우를 대비해, SystemCanvasUI.cs 에서도 자체적으롣 등록할 수 있도록 하는 함수
        public void RegisterSystemCanvas(GameObject canvasObj)
        {
            _SYSTEMCANVAS = canvasObj;
        }

        // DialogueMangaer에서 찾지 못했을 경우를 대비해, IngameUI.cs 에서도 자체적으롣 등록할 수 있도록 하는 함수
        public void RegisterIngameCanvas(GameObject canvasObj)
        {
            _INGAMECANVAS = canvasObj;
        }

        // DialogueMangaer에서 찾지 못했을 경우를 대비해, NpcCanvasUI.cs 에서도 자체적으롣 등록할 수 있도록 하는 함수
        public void RegisterNpcCanvas(NpcCanvasUI canvas)
        {
            npcCanvas = canvas;

            // 등록되자마자 초기화
            if (npcCanvas != null)
            {
                npcCanvas.dialoguePanel.SetActive(false);

                if (npcCanvas.nextBtn != null)
                {
                    npcCanvas.nextBtn.onClick.RemoveAllListeners();
                    npcCanvas.nextBtn.onClick.AddListener(DisplayNextLine);
                }

                Debug.Log("[DialogueManager] NpcCanvas 등록 완료!");
            }
        }

        private void Start()
        {
            /* if (npcCanvas != null)
            {
                dialoguePanel = npcCanvas.dialoguePanel;
                dialogueText = npcCanvas.dialogueText;
                nextBtn = npcCanvas.nextBtn;
                choiceBtnHolder = npcCanvas.choiceBtnHolder;
                choiceBtnPrefab = npcCanvas.choiceBtnPrefab;
            }
            else
            {
                Debug.LogError("[DialogueManager] NPC Canvas를 찾을 수 없음");
            }

            if (_INGAMECANVAS == null) _INGAMECANVAS = FindObjectOfType<InGameUI>().gameObject;
            if (_SYSTEMCANVAS == null) _SYSTEMCANVAS = FindObjectOfType<SystemCanvasUI>().gameObject;

            // 시작 때 대화창 숨기기
            dialoguePanel.SetActive(false); */

         /*    // 다음 버튼에 DisplayNextLine 을 미리 연결
            // nextBtn.onClick.AddListener(=> );
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(DisplayNextLine);
            } */
        }
      

        public void StartDialogue(DialogueSO dialogueData, Npc npc)
        {
            // 대화창 패널 활성화
            dialoguePanel.SetActive(true);

            isDialogueActive = true;

            _SYSTEMCANVAS.SetActive(false);
            _INGAMECANVAS.SetActive(false);

            // 게임 시간 정지 / 플레이어 정지
            Time.timeScale = 0.00001f;

            // 현재 대화 중인 NPC 저장
            currentNpc = npc;

            // 현재 대화 데이터 저장 (선택지를 위해)
            _currentDialogueData = dialogueData;

            // lineQueue 비우기 (clear)
            _dialogueQueue.Clear();

            // SO 안의 모든 대사를 lineQueue 에 추가
            foreach(string line in dialogueData.dialogueLines)
            {
                _dialogueQueue.Enqueue(line);
            }

            // 첫 번째 대사 표시
            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            // 다음 버튼을 일단 보이게 함
            if (nextBtn != null)
            {
                nextBtn.gameObject.SetActive(true);
            }

            // lineQueue 에 남은 대사가 있는지 확인
            if (_dialogueQueue.Count > 0)
            {
                string line = _dialogueQueue.Dequeue();
                dialogueText.text = line;
            }
            else
            {
                // 남은 대사가 없다면 
                if(nextBtn!= null)
                {
                    nextBtn.gameObject.SetActive(false);
                }
                // 선택지 표시
                DisplayChoices();
            }
        }

        private void DisplayChoices()
        {
            // _currentDialogueData.choices 에 선택지가 0보다 많은가?
            if (_currentDialogueData != null && _currentDialogueData.dialogueChoices.Length > 0)
            {
                // 기존에 있던 버튼들 삭제 후 선택지 버튼 생성
                foreach (Transform child in choiceBtnHolder)
                {
                    Destroy(child.gameObject);
                }

                // currentDialogueData.choices 배열을 순회 (foreach)
                foreach (DialogueChoice choice in _currentDialogueData.dialogueChoices)
                {
                    // choiceBtnPrefab 을  choiceBtnHolder 자식으로 생성 (Instantiate)
                    GameObject buttonObj = Instantiate(choiceBtnPrefab, choiceBtnHolder);

                    // 생성된 버튼의 텍스트를 지정
                    buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;

                    // 생성된 버튼의 onClick 리스너에 새 기능 추가
                    Button button = buttonObj.GetComponent<Button>();
                    button.onClick.AddListener(() =>
                    {
                        // (리스너 내부)선택한 다음대화 로 바로 넘어가기
                        OnChoiceSelected(choice);
                    });
                }
            }
            else
            {
                // else 선택지가 없다면 (대화 끝)
                EndDialogue();
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            // 모든 선택지 버튼 삭제
            foreach(Transform child in choiceBtnHolder)
            {
                Destroy(child.gameObject);
            }

            // NPC에게 어떤 선택질를 골랐는지 전달
            if(currentNpc != null)
            {
                currentNpc.OnChoiceMade(choice);
            }

            // 다음 대화가 있는지 확인 (null 체크)
            if (choice.nextDialogue != null)
            {
                // 다음 대화로 이어서 시작
                StartDialogue(choice.nextDialogue, currentNpc);
            }
            else
            {
                // 다음 대화가 없다면 (대화 끝)
                EndDialogue();
            }            
        }

        private void EndDialogue()
        {
            // 대화창 패널 비활성화
            dialoguePanel.SetActive(false);
            _INGAMECANVAS.SetActive(true);

            isDialogueActive = false;

            // 시간 되돌리기
            Time.timeScale = 1f;

            // NPC 참조 비우기
            currentNpc = null;
            _currentDialogueData = null;
        }
    }
}