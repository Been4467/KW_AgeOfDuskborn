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
                return; // 👈 가짜의 오작동을 막는 완벽한 브레이크!
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
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 씬 로딩이 끝나면 무조건 실행됨 -> 다시 등록!
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 🛠️ 핵심 수정: 파괴되기 직전의 가짜 복제본이 DialogueManager를 오염시키는 것을 절대 차단
            if (Instance != this) return;

            RegisterToDialogueManager();
        }

        // 등록 로직 분리 (재사용을 위해)
        private void RegisterToDialogueManager()
        {
            // Instance 체크는 OnSceneLoaded에서 이미 했으므로 생략 가능
            if (DialogueManager.Instance != null)
            {
                // ✅ 이미 만들어진 완전한 등록 함수를 사용
                DialogueManager.Instance.RegisterNpcCanvas(this);
            }

            // 정리 로직 (패널, 선택지)만 여기서 처리
            if (choiceBtnHolder != null)
                foreach (Transform child in choiceBtnHolder)
                    Destroy(child.gameObject);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // ❌ nextBtn 리스너 조작은 여기서 하지 말 것
            // DialogueManager.RegisterNpcCanvas() 안에서 처리됨
        }
    }
}