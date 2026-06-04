using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace KW
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("참조")]
        public InGameUI ingameUi;

        [Header("팝업 설정")]
        [SerializeField] private GameObject itemLootPopupPrefab;                // 팝업 창 프리팹
        [SerializeField] private GameObject itemLootLinePrefab;                 // 아이템 1줄 프리팹
        [SerializeField] private GameObject messageLinePrefab;                  // 일반 알림창 프리팹
        [SerializeField] private Transform popupHolder;                         // 팝업이 생성될 위치

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
                return; // 👈 가짜 오브젝트의 하위 실행을 완벽히 차단하는 브레이크
            }
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
            if (Instance != this) return; // 🛠️ 진짜 인스턴스만 실행하도록 필터링
            if (scene.name == "LoadingScene") return;

            // 🛠️ 씬 이동 시 구형 참조 찌꺼기(Missing)를 확실하게 청소
            ingameUi = null;
            popupHolder = null;

            if (ingameUi == null)
            {
                ingameUi = FindObjectOfType<InGameUI>(true); // 비활성화 상태도 고려하여 찾기
            }

            FindPopupHolder();
        }

        public void FindPopupHolder()
        {
            if (ingameUi != null)
            {
                Transform holder = ingameUi.transform.Find("PopupHolder");

                if (holder == null)
                {
                    // 최적화를 위해 GetComponentsInChildren 보다는 변수 직접 연결을 권장하지만, 
                    // 현재 구조 유지를 위해 안전하게 예외처리 추가
                    var allTransforms = ingameUi.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in allTransforms)
                    {
                        if (t != null && t.name == "PopupHolder")
                        {
                            holder = t;
                            break;
                        }
                    }
                }
                popupHolder = holder;
            }

            if (popupHolder == null)
            {
                Debug.LogWarning("[NotificationManager] PopupHolder를 찾지 못했습니다. (UI가 없는 씬일 수 있음)");
            }
        }

        void Start()
        {
            FindPopupHolder();

            // 🛠️ 버그 수정: popupHolder가 null일 때 .transform.gameObject 접근 시 크래시나는 현상 방지
            if (popupHolder != null)
            {
                Debug.Log("[NotificationManager] 팝업을 담을 오브젝트를 찾음");
            }
            else
            {
                Debug.LogWarning("[NotificationManager] 초기 시작 시 팝업 홀더가 없습니다. 인게임 진입 후 재탐색합니다.");
            }
        }

        #region [Chest] 아이템을 획득했을 때 뜨는 팝업
        public void ShowLootPopup(List<ChestSlot> items)
        {
            if (items == null || items.Count == 0) return;

            // 껍데기 생성 (여기에 일시정지 로직 포함됨)
            GameObject popupObj = CreatePopupBase();
            if (popupObj == null) return;

            Debug.Log("Notification Manager : 팝업 생성됨");

            Transform lineHolder = popupObj.transform.Find("LineHolder");
            if (lineHolder == null)
            {
                Debug.LogError("[NotificationManager] 프리팹 내부에 'LineHolder' 자식이 없습니다.");
                return;
            }

            foreach (ChestSlot slot in items)
            {
                if (slot.item == null) continue;

                GameObject lineObj = Instantiate(itemLootLinePrefab, lineHolder);

                Transform iconTransform = lineObj.transform.Find("ItemSprite");
                Transform nameTransform = lineObj.transform.Find("ItemName");

                if (iconTransform != null)
                {
                    Image itemIcon = iconTransform.GetComponent<Image>();
                    if (itemIcon != null) itemIcon.sprite = slot.item.itemSprite;
                }

                if (nameTransform != null)
                {
                    TextMeshProUGUI text = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (text != null) text.text = $"{slot.item.itemName}이(가) x{slot.quantity}개 추가됐다.";
                }
            }
        }
        #endregion

        #region 일반 팝업
        public void ShowMessage(string message, Action onConfirm = null)
        {
            GameObject popupObj = CreatePopupBase(onConfirm);
            if (popupObj == null) return;

            Transform lineHolder = popupObj.transform.Find("LineHolder");
            if (lineHolder == null) return;

            GameObject lineObj = Instantiate(messageLinePrefab, lineHolder);
            TextMeshProUGUI textComp = lineObj.GetComponentInChildren<TextMeshProUGUI>();

            if (textComp != null)
            {
                textComp.text = message;
            }
        }
        #endregion

        private GameObject CreatePopupBase(Action onExternalConfirm = null)
        {
            // 🛠️ 팝업을 띄울 홀더가 유실되었다면 긴급 재탐색
            if (popupHolder == null)
            {
                FindPopupHolder();
                if (popupHolder == null)
                {
                    Debug.LogError("[NotificationManager] 팝업을 생성할 PopupHolder가 하이어라키에 없습니다!");
                    return null;
                }
            }

            Time.timeScale = 0.00001f; // 일시정지

            GameObject popupObj = Instantiate(itemLootPopupPrefab, popupHolder);

            Transform confirmBtnTransform = popupObj.transform.Find("ConfirmBtn");
            if (confirmBtnTransform != null)
            {
                Button confirmBtn = confirmBtnTransform.GetComponent<Button>();
                if (confirmBtn != null)
                {
                    confirmBtn.onClick.RemoveAllListeners();
                    confirmBtn.onClick.AddListener(() =>
                    {
                        onExternalConfirm?.Invoke();
                        Destroy(popupObj);

                        // 🛠️ 중요 안전장치: 현재 팝업을 지우고 나서도 홀더 자식에 다른 팝업이 여전히 남아있다면, 
                        // 시간을 흐르게 하지 않고 일시정지 상태를 유지합니다. (다중 팝업 처리)
                        // 한 프레임 뒤에 자식 개수를 검사하기 위해 람다식 안에서 연산 처리
                        Invoke(nameof(CheckRemainingPopups), 0f);
                    });
                }
            }

            return popupObj;
        }

        private void CheckRemainingPopups()
        {
            if (popupHolder != null && popupHolder.childCount == 0)
            {
                Time.timeScale = 1f; // 남은 팝업이 정말로 없을 때만 게임 시간 재개
            }
        }
    }
}