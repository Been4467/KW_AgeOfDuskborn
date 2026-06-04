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
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.npcCanvas = this;
                // Debug.Log("[NpcCanvasUI] DialogueManager에 진짜 인스턴스 재등록 완료!");
            }

            // 🛠️ 안전장치 추가: 씬이 바뀔 때 이전 대화의 버튼 리스너 찌꺼기 제거 (버튼 스킵 버그 방지)
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
            }

            // 🛠️ 안전장치 추가: 생성되어 남아있던 선택지 버튼 찌꺼기 UI 완전 파괴
            if (choiceBtnHolder != null)
            {
                foreach (Transform child in choiceBtnHolder)
                {
                    Destroy(child.gameObject);
                }
            }

            // 씬 바뀌면 대화창 꺼두기
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }
    }
}