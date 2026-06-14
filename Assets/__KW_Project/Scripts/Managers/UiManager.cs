using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        public static event Action<bool> OnAnyUiStateChanged;
        public static event Action OnDeathed;

        [SerializeField] private GameObject ingameUi;
        public InGameUI inGameUIScript;
        [SerializeField] private GameObject systemCanvas;
        public SystemCanvasUI systemCanvasUIScript;

        [SerializeField] private List<UiPanel> uiPanels;
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

        // ─────────────────────────────────────────
        // 화톳불 발견 연출
        // ─────────────────────────────────────────

        [Header("화톳불 발견 연출")]
        [Tooltip("InGameUI 하위의 LostBonfireDiscoverd/Image를 연결하세요.")]
        [SerializeField] private Image _bonfireDiscoveryImage;

        private Coroutine _bonfireUiCoroutine;

        // ─────────────────────────────────────────
        // 생명주기
        // ─────────────────────────────────────────

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

        private void Start()
        {
            InitializeUI();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // ✅ 씬 전환 시 화톳불 연출이 재생 중이었다면 즉시 정리
            CancelBonfireDiscovery();

            if (Instance == this)
                InitializeUI();
        }

        // ─────────────────────────────────────────
        // 등록 함수
        // ─────────────────────────────────────────

        public void RegisterSystemCanvas(GameObject canvasObj)
        {
            systemCanvas = canvasObj;
        }

        public void RegisterIngameCanvas(GameObject canvasObj)
        {
            ingameUi = canvasObj;
        }

        // ─────────────────────────────────────────
        // UI 초기화
        // ─────────────────────────────────────────

        private void InitializeUI()
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

            if (inGameUIScript == null) inGameUIScript = FindObjectOfType<InGameUI>(true);
            if (systemCanvasUIScript == null) systemCanvasUIScript = FindObjectOfType<SystemCanvasUI>(true);

            if (inGameUIScript != null)
                ingameUi = inGameUIScript.gameObject;

            if (systemCanvasUIScript != null)
            {
                systemCanvas = systemCanvasUIScript.gameObject;
                systemCanvas.SetActive(true);
                RebindUiPanels(systemCanvas.transform);
            }

            if (inGameUIScript != null)
                _bonfireDiscoveryImage = inGameUIScript.bonfireDiscoveryImage;

            if (_bonfireDiscoveryImage == null)
                Debug.LogWarning("[UiManager] bonfireDiscoveryImage를 찾을 수 없습니다. InGameUI의 슬롯을 확인하세요.");

            currentOpenPanel = null;
            Time.timeScale = 1f;

            foreach (var panel in uiPanels)
            {
                panel.isOpen = false;
                if (panel.panelObject != null)
                    panel.panelObject.SetActive(false);
            }

            if (ingameUi != null) ingameUi.SetActive(true);
            if (systemCanvas != null) systemCanvas.SetActive(false);
        }

        private void RebindUiPanels(Transform canvasRoot)
        {
            foreach (var panel in uiPanels)
            {
                Transform foundPanel = canvasRoot.Find(panel.name);
                if (foundPanel != null)
                    panel.panelObject = foundPanel.gameObject;
                else
                    panel.panelObject = FindChildPanelRecursively(canvasRoot, panel.name);

                if (panel.panelObject == null)
                    Debug.LogError($"[UiManager] '{panel.name}' 패널을 하이어라키에서 찾을 수 없습니다. 대소문자를 확인하세요.");
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

        // ─────────────────────────────────────────
        // 화톳불 발견 연출 — Bonfire.cs에서 호출
        // ─────────────────────────────────────────

        public void ShowBonfireDiscovery(
            AudioSource audioSource,
            AudioClip discoverSound,
            Color flashColor,
            float fadeInDuration,
            float dipDuration,
            float stayDuration,
            float fadeOutDuration)
        {
            if (_bonfireDiscoveryImage == null)
            {
                Debug.LogError("[UiManager] bonfireDiscoveryImage가 null입니다. 연출을 재생할 수 없습니다.");
                return;
            }

            if (_bonfireUiCoroutine != null)
                StopCoroutine(_bonfireUiCoroutine);

            _bonfireUiCoroutine = StartCoroutine(BonfireDiscoveryRoutine(
                audioSource, discoverSound, flashColor,
                fadeInDuration, dipDuration, stayDuration, fadeOutDuration));
        }

        /// <summary>
        /// 화톳불 연출을 즉시 중단하고 이미지를 초기 상태로 되돌립니다.
        /// 휴식 진입, 씬 전환, UI 팝업 등 연출을 끊어야 하는 상황에서 호출하세요.
        /// </summary>
        public void CancelBonfireDiscovery()
        {
            if (_bonfireUiCoroutine != null)
            {
                StopCoroutine(_bonfireUiCoroutine);
                _bonfireUiCoroutine = null;
            }

            if (_bonfireDiscoveryImage != null)
            {
                _bonfireDiscoveryImage.color = new Color(1f, 1f, 1f, 0f);
                _bonfireDiscoveryImage.gameObject.SetActive(false);
            }
        }

        private IEnumerator BonfireDiscoveryRoutine(
            AudioSource audioSource,
            AudioClip discoverSound,
            Color flashColor,
            float fadeInDuration,
            float dipDuration,
            float stayDuration,
            float fadeOutDuration)
        {
            if (audioSource != null && discoverSound != null)
                audioSource.PlayOneShot(discoverSound);

            _bonfireDiscoveryImage.gameObject.SetActive(true);

            // 1. 페이드 인
            float timer = 0f;
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                _bonfireDiscoveryImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                yield return null;
            }
            _bonfireDiscoveryImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 1f);

            // 2. 딥
            timer = 0f;
            float targetDipAlpha = 230f / 255f;
            while (timer < dipDuration)
            {
                timer += Time.deltaTime;
                float t = timer / dipDuration;
                float alpha = Mathf.Lerp(1f, targetDipAlpha, t);
                Color currentColor = Color.Lerp(flashColor, Color.white, t);
                _bonfireDiscoveryImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
                yield return null;
            }
            _bonfireDiscoveryImage.color = new Color(1f, 1f, 1f, targetDipAlpha);

            // 3. 대기
            yield return new WaitForSeconds(stayDuration);

            // 4. 페이드 아웃
            timer = 0f;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(targetDipAlpha, 0f, timer / fadeOutDuration);
                _bonfireDiscoveryImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            // ✅ 코루틴이 정상 완료된 경우에도 CancelBonfireDiscovery()로 일관되게 정리
            CancelBonfireDiscovery();
        }

        // ─────────────────────────────────────────
        // 패널 관리
        // ─────────────────────────────────────────

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
                currentOpenPanel = null;
            else
                currentOpenPanel = panelToToggle;

            UpdateAllPanelViews();
            OnAnyUiStateChanged?.Invoke(currentOpenPanel != null);
        }

        private void UpdateAllPanelViews()
        {
            bool isAnyUiOpen = (currentOpenPanel != null);

            Time.timeScale = isAnyUiOpen ? 0.0001f : 1f;

            if (ingameUi != null) ingameUi.SetActive(!isAnyUiOpen);
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
                TogglePanel(panelToOpen);
        }

        private UiPanel FindPanelUsingFunction(string nameToFind)
        {
            foreach (var panel in uiPanels)
                if (panel.name == nameToFind) return panel;
            return null;
        }

        // ─────────────────────────────────────────
        // 휴식 / 부활 / 페이드
        // ─────────────────────────────────────────

        public void StartRestMode()
        {
            // ✅ 휴식 진입 시 화톳불 연출이 재생 중이라면 즉시 정리
            CancelBonfireDiscovery();

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

            DoFadeOut(() => { OnAnyUiStateChanged?.Invoke(false); });
        }

        public void StartResponse()
        {
            // ✅ 부활 시에도 화톳불 연출 정리
            CancelBonfireDiscovery();

            isResponseMode = true;

            if (systemCanvas != null) systemCanvas.SetActive(isResponseMode);
            if (systemCanvasUIScript != null) systemCanvasUIScript.ToggleGameMode(!isResponseMode);

            DoFadeIn(() => { Invoke(nameof(EndResponse), 0.5f); });
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
                        SaveManager.Instance.playerHealth.SetHealth(SaveManager.Instance.playerHealth.maxHp);

                    DoFadeOut(() => { OnAnyUiStateChanged?.Invoke(false); });
                }
            }
        }

        public void DoFadeIn(Action onComplete)
        {
            StopAllCoroutines();
            // ✅ StopAllCoroutines()가 bonfire 코루틴도 죽이므로 이미지 상태를 즉시 정리
            _bonfireUiCoroutine = null;
            if (_bonfireDiscoveryImage != null)
            {
                _bonfireDiscoveryImage.color = new Color(1f, 1f, 1f, 0f);
                _bonfireDiscoveryImage.gameObject.SetActive(false);
            }
            StartCoroutine(FadeRoutine(0, 1, onComplete));
        }

        public void DoFadeOut(Action onComplete)
        {
            StopAllCoroutines();
            // ✅ 동일하게 정리
            _bonfireUiCoroutine = null;
            if (_bonfireDiscoveryImage != null)
            {
                _bonfireDiscoveryImage.color = new Color(1f, 1f, 1f, 0f);
                _bonfireDiscoveryImage.gameObject.SetActive(false);
            }
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