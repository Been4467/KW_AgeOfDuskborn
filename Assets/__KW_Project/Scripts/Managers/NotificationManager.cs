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
            if (scene.name == "LoadingScene") return;

            // InGameUI가 등록될 때까지 기다리거나, 여기서 직접 찾기
            // (가장 확실한 건 InGameUI가 등록해주는 거지만, 늦을 수 있으니 찾기 시도)
            if (ingameUi == null)
            {
                ingameUi = FindObjectOfType<InGameUI>();
            }

            FindPopupHolder();
        }

        // 팝업 홀더 찾는 함수 분리
        public void FindPopupHolder()
        {
            if (ingameUi != null)
            {
                // "PopupHolder"라는 이름의 자식을 찾음
                Transform holder = ingameUi.transform.Find("PopupHolder");

                // 만약 바로 아래 자식이 아니라면 전체 검색
                if (holder == null)
                {
                    // (InGameUI 스크립트에 public Transform popupHolder 변수를 만들어서 연결해두는 게 가장 좋음!)
                    // 임시: 이름으로 재귀 검색 (비추천하지만 작동은 함)
                    foreach (Transform t in ingameUi.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "PopupHolder")
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
                Debug.LogWarning("[NotificationManager] PopupHolder를 찾지 못했습니다.");
            }
        }
        void Start()
        {
            FindPopupHolder();

            GameObject popupHolderObj = popupHolder.transform.gameObject;

            if (popupHolderObj != null)
            {
                Debug.Log("[NotificationManager] 팝업을 담을 오브젝트를 찾음");
            }
            else
            {
                Debug.LogError("[NotificationManager] 팝업을 담을 오브젝트를 찾지 못함");
            }
        }


        #region [Chest] 아이템을 획득했을 때 뜨는 팝업
        public void ShowLootPopup(List<ChestSlot> items)
        {
            Time.timeScale = 0.00001f;

            // 팝업 창 생성
            GameObject popupObj = CreatePopupBase();

            if (popupObj != null) Debug.Log("Notification Manager : 팝업 생성됨");
            else Debug.LogWarning("Notification Manager : 팝업 생성 안됨");

            // 팝업 창 안에서 아이템 1줄이 생성될 위치 찾기
            Transform lineHolder = popupObj.transform.Find("LineHolder");

            foreach (ChestSlot slot in items)
            {
                // 아이템 1 줄 프리팹을 LineHolder의 자식으로 생성함
                GameObject lineObj = Instantiate(itemLootLinePrefab, lineHolder);

                Image itemIcon = lineObj.transform.Find("ItemSprite").GetComponent<Image>();
                TextMeshProUGUI text = lineObj.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();

                if (itemIcon != null)
                    itemIcon.sprite = slot.item.itemSprite;                     // Item 의 itemSprite

                if (text != null)
                    text.text = $"{slot.item.itemName}이(가) x{slot.quantity}개 추가됐다.";
            }


        }
        #endregion

        #region 일반 팝업
        public void ShowMessage(string message, Action onConfirm = null)
        {
            // 껍데기 생성
            GameObject popupObj = CreatePopupBase(onConfirm);
            Transform lineHolder = popupObj.transform.Find("LineHolder");

            // 내용물 채우기
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
            Time.timeScale = 0.00001f; // 일시정지



            GameObject popupObj = Instantiate(itemLootPopupPrefab, popupHolder);

            // 닫기 버튼 공통 로직
            Button confirmBtn = popupObj.transform.Find("ConfirmBtn").GetComponent<Button>();
            if (confirmBtn != null)
            {
                confirmBtn.onClick.AddListener(() =>
                {
                    Time.timeScale = 1f; // 재개

                    onExternalConfirm?.Invoke();

                    Destroy(popupObj);
                });
            }

            return popupObj;
        }
    }
}
