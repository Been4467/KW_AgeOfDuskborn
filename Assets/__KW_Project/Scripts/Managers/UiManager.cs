using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    [System.Serializable]
    public class UiPanel
    {
        public string name;
        public KeyCode key;
        public GameObject panelObject;
        public bool isOpen = false;
    }

    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance { get; private set; }

        public static event Action<bool> OnAnyUiStateChanged;           // 패널이 열고 닫을 때 호출 되는 이벤트 (PlayerMovement에서구독함)
        public static event Action OnDeathed;           // 죽었을 때 호출 되는 이벤트 (PlayerMovement에서구독함)

        [SerializeField] private GameObject ingameUi;
        public InGameUI inGameUIScript;
        [SerializeField] private GameObject systemCanvas;
        public SystemCanvasUI systemCanvasUIScript;

        [SerializeField] private List<UiPanel> uiPanels;                // 각 (인벤/맵/설정/퀘스트) 패널을 담을 리스트

        [SerializeField] private UiPanel currentOpenPanel = null;

        [Header("휴식 상태")]
        public bool isRestMode = false;

        [Header("부활 상태")]
        public bool isResponseMode = false;

        [Header("페이드")]
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("카메라")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private float restingFOV = 19f;
        [SerializeField] private float normalFOV = 38f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnAnyUiStateChanged = null;
                OnDeathed = null;
                
                Instance = null;
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeUI();
        }

        public void RegisterSystemCanvas(GameObject canvasObj)
        {
            systemCanvas = canvasObj;
        }

        public void RegisterIngameCanvas(GameObject canvasObj)
        {
            ingameUi = canvasObj;
        }

        private void InitializeUI()
        {
            // 스크립트가 없으면 씬에서 찾기 (안전장치)
            if (inGameUIScript == null) inGameUIScript = FindObjectOfType<InGameUI>(true);
            if (systemCanvasUIScript == null) systemCanvasUIScript = FindObjectOfType<SystemCanvasUI>(true);

            // GameObject 연결 및 초기화
            if (inGameUIScript != null)
            {
                ingameUi = inGameUIScript.gameObject;
                ingameUi.SetActive(true);
            }

            if (systemCanvasUIScript != null)
            {
                systemCanvas = systemCanvasUIScript.gameObject;
                systemCanvas.SetActive(true);
            }

            // 패널들 초기화 (비활성화)
            // 주의: SystemCanvas 자식들을 찾아 연결하는 로직이 필요하다면 여기 추가해야 함
            foreach (var panel in uiPanels)
            {
                if (panel.panelObject != null)
                {
                    panel.panelObject.SetActive(false);
                    panel.isOpen = false;
                }
            }

            if (ingameUi != null) ingameUi.SetActive(true);
            if (systemCanvas != null) systemCanvas.SetActive(false); // 평소엔 꺼둠
        }

        private void Start()
        {
            if (inGameUIScript != null)
            {
                // Debug.Log("[UiManager] 인게임Ui 등록완료");
                ingameUi = inGameUIScript.transform.gameObject;
                ingameUi.SetActive(true);
            }
            if (systemCanvasUIScript != null)
            {
                // Debug.Log("[UiManager] 시스템Ui 등록완료");
                systemCanvas = systemCanvasUIScript.transform.gameObject;
                systemCanvas.SetActive(true);
            }

            foreach (var panel in uiPanels)
            {
                panel.panelObject.SetActive(false);                     // 패널 클래스를 가진 패널 각각 setActive(false)
                panel.isOpen = false;
            }
            ingameUi.SetActive(true);                                   // 게임 실행 시 인게임 캔버스 on
            systemCanvas.SetActive(false);                              // 게임 실행 시 시스템 캔버스 off
        }

        private void Update()
        {
            if (DialogueManager.isDialogueActive)
            {
                return;
            }

            if (isResponseMode || isRestMode) return;

            if (currentOpenPanel != null && Input.GetKeyDown(KeyCode.Escape))   // 열려있는 게 있으면 우선적으로 닫음
            {
                TogglePanel(currentOpenPanel);
                return;
            }
            foreach (var panel in uiPanels)                                     // 열려있는 게 없으면 선택된 패널을 연다
            {
                if (Input.GetKeyDown(panel.key))
                {
                    TogglePanel(panel);
                    return;                                             // 한 프레임에 하나의 입력 처리
                }
            }
        }

        private void TogglePanel(UiPanel panelToToggle)
        {
            if (currentOpenPanel == panelToToggle)                      // 현재 열려있는 게 panelToToggle이면 -> 끄기
            {
                currentOpenPanel = null;
            }
            else                                                        // 열려있는게 없으면                  -> 열기
            {
                currentOpenPanel = panelToToggle;
            }
            UpdateAllPanelViews();
            OnAnyUiStateChanged?.Invoke(currentOpenPanel != null);      // currentOpenPanel 이 있으면 이벤트 호출
        }

        private void UpdateAllPanelViews()
        {
            // currenOpenPanel 이 있거나, 외부 팝업창이 열려있으면 true
            bool isAnyUiOpen = (currentOpenPanel != null);

            if (isAnyUiOpen)
            {
                Time.timeScale = 0.0001f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            ingameUi.SetActive(!isAnyUiOpen);
            systemCanvas.SetActive(isAnyUiOpen);

            foreach (var panel in uiPanels)
            {
                panel.isOpen = (panel == currentOpenPanel);             // 해당 panel 이 현재 열려있는 패널이면 isOpen
                panel.panelObject.SetActive(panel.isOpen);              // isOpen 이 true 면 해당 패널 오브젝트 SetActive
            }

            // OnAnyUiStateChanged?.Invoke(isAnyUiOpen);
        }

        // 패널의 버튼을 위한 함수
        public void OpenPanelByName(string panelName)
        {
            // 'IsPanelMatch'라는 이름의 함수를 조건으로 사용해 패널을 찾습니다.
            UiPanel panelToOpen = FindPanelUsingFunction(panelName);

            if (currentOpenPanel == panelToOpen)                         // 버튼으로 패널을 열 때, 현재 열려있는게 열어야 될 패널이면 버튼으로 꺼지지 않게끔 반환
            {
                return;
            }

            if (panelToOpen != null)                                    // 열어야 될 패널이 있으면(들어온 값이 있다), toggleOpen실행
            {
                TogglePanel(panelToOpen);
            }
        }

        private UiPanel FindPanelUsingFunction(string nameToFind)
        {
            // 리스트의 모든 패널을 처음부터 끝까지 확인합니다.
            foreach (var panel in uiPanels)
            {
                // 만약 현재 확인 중인 패널의 이름이 우리가 찾으려는 이름과 같다면
                if (panel.name == nameToFind)
                {
                    // 바로 그 패널을 반환하고 함수를 종료합니다.
                    return panel;
                }
            }

            // 루프가 끝날 때까지 찾지 못했다면 null을 반환합니다.
            return null;
        }

        public void StartRestMode()
        {
            isRestMode = true;

            ingameUi.SetActive(!isRestMode);
            systemCanvas.SetActive(isRestMode);
            systemCanvasUIScript.ToggleGameMode(!isRestMode);

            OnAnyUiStateChanged?.Invoke(true);

            DoFadeIn(() =>
             {
                 if (virtualCamera != null)
                     virtualCamera.m_Lens.FieldOfView = restingFOV;

                 Invoke(nameof(EndRestMode), 0.5f);
             });
        }

        private void EndRestMode()
        {
            systemCanvasUIScript.ToggleRestMode(isRestMode);

            DoFadeOut(null);
        }

        public void StartGameMode()
        {
            isRestMode = false;

            systemCanvasUIScript.ToggleRestMode(isRestMode);

            DoFadeIn(() =>
            {
                if (virtualCamera != null)
                    virtualCamera.m_Lens.FieldOfView = normalFOV;

                Invoke(nameof(EndGameMode), 0.5f);
            });
        }

        private void EndGameMode()
        {
            ingameUi.SetActive(!isRestMode);
            systemCanvas.SetActive(isRestMode);
            systemCanvasUIScript.ToggleGameMode(!isRestMode);

            DoFadeOut(() =>
            {
                OnAnyUiStateChanged?.Invoke(false);
            });
        }

        public void StartResponse()
        {
            isResponseMode = true;

            systemCanvas.SetActive(isResponseMode);
            systemCanvasUIScript.ToggleGameMode(!isResponseMode);

            DoFadeIn(() =>
            {
                Invoke(nameof(EndResponse), 0.5f);
            });
        }

        private void EndResponse()
        {
            OnDeathed.Invoke();

            DoFadeOut(() =>
            {
                Debug.Log("[UiManager] EndResponse");

                isResponseMode = false;

                systemCanvas.SetActive(isRestMode);
                systemCanvasUIScript.ToggleGameMode(!isRestMode);
            });
        }

        public void DoFadeIn(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0, 1, onComplete));
        }

        public void DoFadeOut(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1, 0, onComplete));
        }

        private IEnumerator FadeRoutine(float start, float end, Action onComplete)
        {
            float timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(start, end, timer / fadeDuration);
                yield return null;
            }
            fadeGroup.alpha = end;
            onComplete?.Invoke();
        }
    }
}
