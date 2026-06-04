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

        public static event Action<bool> OnAnyUiStateChanged; // 패널 열고 닫힐 때 호출
        public static event Action OnDeathed;               // 죽었을 때 호출

        [SerializeField] private GameObject ingameUi;
        public InGameUI inGameUIScript;
        [SerializeField] private GameObject systemCanvas;
        public SystemCanvasUI systemCanvasUIScript;

        [SerializeField] private List<UiPanel> uiPanels; // 각 (인벤/맵/설정/퀘스트) 패널 리스트
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
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
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
            if (Instance == this)
            {
                InitializeUI();
            }
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
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

            if (inGameUIScript == null) inGameUIScript = FindObjectOfType<InGameUI>(true);
            if (systemCanvasUIScript == null) systemCanvasUIScript = FindObjectOfType<SystemCanvasUI>(true);

            if (inGameUIScript != null)
            {
                ingameUi = inGameUIScript.gameObject;
            }

            if (systemCanvasUIScript != null)
            {
                systemCanvas = systemCanvasUIScript.gameObject;

                // 🛠️ [핵심] 자식 패널들을 찾기 위해 임시로 활성화합니다.
                systemCanvas.SetActive(true);
                RebindUiPanels(systemCanvas.transform);
            }

            currentOpenPanel = null;
            Time.timeScale = 1f;

            // 모든 패널 상태 및 오브젝트 강제 초기화 (비활성화)
            foreach (var panel in uiPanels)
            {
                panel.isOpen = false;
                if (panel.panelObject != null)
                {
                    panel.panelObject.SetActive(false);
                }
            }

            // 🛠️ [원하시는 초기 상태 설정] 
            // 게임 시작 및 씬 로드 시 인게임 UI는 켜고, 시스템 캔버스는 완전히 끕니다.
            if (ingameUi != null) ingameUi.SetActive(true);
            if (systemCanvas != null) systemCanvas.SetActive(false);
        }

        private void RebindUiPanels(Transform canvasRoot)
        {
            foreach (var panel in uiPanels)
            {
                Transform foundPanel = canvasRoot.Find(panel.name);
                if (foundPanel != null)
                {
                    panel.panelObject = foundPanel.gameObject;
                }
                else
                {
                    panel.panelObject = FindChildPanelRecursively(canvasRoot, panel.name);
                }

                // 패널을 못 찾았을 때 디버그 경고창 출력
                if (panel.panelObject == null)
                {
                    Debug.LogError($"[UiManager] '{panel.name}' 패널을 하이어라키에서 찾을 수 없습니다. 대소문자를 확인하세요.");
                }
            }
        }

        private GameObject FindChildPanelRecursively(Transform current, string targetName)
        {
            if (current.name == targetName) return current.gameObject;
            for (int i = 0; i < current.childCount; i++)
            {
                GameObject result = FindChildPanelRecursively(current.GetChild(i), targetName);
                if (result != null) return result;
            }
            return null;
        }

        private void Start()
        {
            InitializeUI();
        }

        private void Update()
        {
            if (DialogueManager.isDialogueActive) return;
            if (isResponseMode || isRestMode) return;

            if (currentOpenPanel != null && Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePanel(currentOpenPanel);
                return;
            }
            foreach (var panel in uiPanels)
            {
                if (Input.GetKeyDown(panel.key))
                {
                    TogglePanel(panel);
                    return;
                }
            }
        }

        private void TogglePanel(UiPanel panelToToggle)
        {
            if (currentOpenPanel == panelToToggle)
            {
                currentOpenPanel = null;
            }
            else
            {
                currentOpenPanel = panelToToggle;
            }
            UpdateAllPanelViews();
            OnAnyUiStateChanged?.Invoke(currentOpenPanel != null);
        }

        private void UpdateAllPanelViews()
        {
            bool isAnyUiOpen = (currentOpenPanel != null);

            if (isAnyUiOpen)
            {
                Time.timeScale = 0.0001f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            // 🛠️ [요구사항 반영] UI가 켜지면 InGameUI는 꺼지고, 닫히면 InGameUI가 켜집니다.
            if (ingameUi != null) ingameUi.SetActive(!isAnyUiOpen);

            // 🛠️ [요구사항 반영] UI가 하나라도 열리면 SystemCanvas를 켜고, 다 닫히면 니다.
            if (systemCanvas != null) systemCanvas.SetActive(isAnyUiOpen);

            foreach (var panel in uiPanels)
            {
                if (panel.panelObject != null)
                {
                    panel.isOpen = (panel == currentOpenPanel);
                    panel.panelObject.SetActive(panel.isOpen);
                }
            }
        }

        public void OpenPanelByName(string panelName)
        {
            UiPanel panelToOpen = FindPanelUsingFunction(panelName);

            if (currentOpenPanel == panelToOpen) return;

            if (panelToOpen != null)
            {
                TogglePanel(panelToOpen);
            }
        }

        private UiPanel FindPanelUsingFunction(string nameToFind)
        {
            foreach (var panel in uiPanels)
            {
                if (panel.name == nameToFind) return panel;
            }
            return null;
        }

        public void StartRestMode()
        {
            isRestMode = true;

            if (ingameUi != null) ingameUi.SetActive(!isRestMode);
            if (systemCanvas != null) systemCanvas.SetActive(isRestMode);
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleGameMode(!isRestMode);

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
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleRestMode(isRestMode);
            DoFadeOut(null);
        }

        public void StartGameMode()
        {
            isRestMode = false;

            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleRestMode(isRestMode);

            DoFadeIn(() =>
            {
                if (virtualCamera != null)
                    virtualCamera.m_Lens.FieldOfView = normalFOV;

                Invoke(nameof(EndGameMode), 0.5f);
            });
        }

        private void EndGameMode()
        {
            if (ingameUi != null) ingameUi.SetActive(!isRestMode);
            if (systemCanvas != null) systemCanvas.SetActive(isRestMode);
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleGameMode(!isRestMode);

            DoFadeOut(() =>
            {
                OnAnyUiStateChanged?.Invoke(false);
            });
        }

        public void StartResponse()
        {
            isResponseMode = true;

            if (systemCanvas != null) systemCanvas.SetActive(isResponseMode);
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleGameMode(!isResponseMode);

            // 1. 화면을 암전(FadeIn) 시킨다.
            DoFadeIn(() =>
            {
                // 2. 완전히 어두워지면 0.5초 대기 후 부활 실무 로직(EndResponse) 호출
                Invoke(nameof(EndResponse), 0.5f);
            });
        }

        private void EndResponse()
        {
            Debug.Log("[UiManager] EndResponse - 부활 프로세스 시작 (화면 암전 상태)");
            isResponseMode = false;

            if (systemCanvas != null) systemCanvas.SetActive(isRestMode);
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleGameMode(!isRestMode);

            if (SaveManager.Instance != null)
            {
                string checkPath = Application.persistentDataPath + "/saveGame.json";

                if (System.IO.File.Exists(checkPath))
                {
                    Debug.Log("[UiManager] 세이브 파일이 존재하므로 씬 전환 및 데이터 로드를 시작합니다.");
                    // ★ 중요: 여기서 로드를 시작하면 SaveManager가 로드 완료 후 페이드 아웃을 켜줄 것입니다.
                    SaveManager.Instance.LoadGame(isRespawn: true);
                }
                else
                {
                    Debug.LogWarning("[UiManager] 세이브 파일이 없습니다. 현재 씬에서 즉시 부활합니다.");

                    SaveManager.Instance.ReApplyEquippedItems();

                    if (SaveManager.Instance.player != null)
                    {
                        if (SaveManager.Instance.player.cController != null)
                            SaveManager.Instance.player.cController.enabled = false;

                        SaveManager.Instance.player.RespawnPlayerAtSpawnPoint(0);

                        if (SaveManager.Instance.player.cController != null)
                            SaveManager.Instance.player.cController.enabled = true;
                    }

                    if (SaveManager.Instance.playerHealth != null)
                    {
                        SaveManager.Instance.playerHealth.SetHealth(SaveManager.Instance.playerHealth.maxHp);
                    }

                    // 세이브가 없는 경우엔 씬 이동이 없으므로, 여기서 즉시 페이드 아웃을 해줍니다.
                    DoFadeOut(() => {
                        OnAnyUiStateChanged?.Invoke(false);
                    });
                }
            }
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
                if (fadeGroup != null) fadeGroup.alpha = Mathf.Lerp(start, end, timer / fadeDuration);
                yield return null;
            }
            if (fadeGroup != null) fadeGroup.alpha = end;
            onComplete?.Invoke();
        }
    }
}