using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KW
{
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindAnyObjectByType<SaveManager>();

                    if (_instance == null)
                    {
                        Debug.LogError("씬에 SaveManager가 존재하지 않습니다!");
                    }
                }
                return _instance;
            }
        }

        public static event Action OnLoadGame;           // 게임이 로드되었을 때 호출 되는 이벤트 (PlayerMovement에서구독함)

        [Header("참조")]
        public PlayerMovement player;
        public PlayerHealth playerHealth;
        public Inventory inventory;
        public QuestManager questManager;

        // 퀵슬롯 UI 들의 참조
        public QuickSlot_Ui weaponSlot;
        public QuickSlot_Ui armourSlot;
        public QuickSlot_Ui potionSlot;

        // 저장될 경로
        private string savePath;

        // 죽은 몬스터ID 를 저장할 리스트
        public List<string> deadMonsterIDs = new List<string>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            savePath = Application.persistentDataPath + "/saveGame.json";                   // 저장될 주소 지정
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnLoadGame = null;

                _instance = null;
            }
        }

        public void SaveGame()
        {
            // 퀵슬롯 데이터
            if (weaponSlot == null || armourSlot == null || potionSlot == null)
            {
                FindQuickSlots();
            }

            SaveData data = new SaveData();

            // 현재 씬 정보 저장
            data.sceneName = SceneManager.GetActiveScene().name;

            // 플레이어 정보 저장
            data.playerPos = player.transform.position;         // 플레이어의 transform
            data.currentHp = playerHealth.currentHp;            // 플레이어의 hp 

            // 인벤토리 저장
            foreach (var slot in inventory.slots)
            {
                // 각 인벤토리에 아이템이 존재한다면
                if (slot.item != null)
                {
                    // 아이템의 정보를 불러옴
                    ItemSaveData itemData = new ItemSaveData
                    {
                        itemId = slot.item.itemId,
                        amount = slot.stack,
                    };
                    // SaveData.cs 의 inventoryItems List 에 추가
                    data.inventoryItems.Add(itemData);
                }
            }

            // 퀵슬롯 저장
            SaveQuickSlot(weaponSlot, data.quickSlotItems);
            SaveQuickSlot(armourSlot, data.quickSlotItems);
            SaveQuickSlot(potionSlot, data.quickSlotItems);

            // 퀘스트 저장
            data.completedQuestNames = new List<string>(questManager.completedQuests);
            foreach (var q in questManager.activeQuests)
            {
                QuestSaveData qData = new QuestSaveData
                {
                    questName = q.data.questTitle,                  // 고유한 식별자여야 함, 그러려면 QuestSO에 퀘스트Id 를 추가하는게 좋지 않나?
                    currentCount = q.currentCount
                };
                data.activeQuests.Add(qData);
            }

            // NPC 상태 저장 (Dictionary -> List 변환)
            if (questManager != null)
            {
                foreach (var kvp in questManager.npcStateDict)
                {
                    NpcSaveData npcData = new NpcSaveData
                    {
                        npcID = kvp.Key,
                        hasMet = kvp.Value.hasMetPlayer,
                        questStateIndex = (int)kvp.Value.questState
                    };
                    data.npcDataList.Add(npcData);
                }
            }

            // 죽은 몬스터들의 List 상태 저장
            // 현재 메모리에 있는 사망자 명단을 저장 데이터에 복사
            data.deadMonsterIDs = new List<string>(deadMonsterIDs);

            // 파일 쓰기
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);

            /*   if(NotificationManager.Instance != null)
              {
                  string message = "게임이 저장되었습니다.";

                  NotificationManager.Instance.ShowMessage(message);
              } */
            Debug.Log("게임저장됨 : " + savePath);
        }

        private void SaveQuickSlot(QuickSlot_Ui slot, List<ItemSaveData> list)
        {
            if (slot.equippedItem != null)
            {
                list.Add(new ItemSaveData { itemId = slot.equippedItem.itemId, amount = 1 });
            }
            else
            {
                list.Add(new ItemSaveData { itemId = "", amount = 0 });
            }
        }

        private void FindQuickSlots()
        {
            QuickSlot_Ui[] allSlots = FindObjectsOfType<QuickSlot_Ui>(true);

            foreach (var slot in allSlots)
            {
                // 퀵슬롯에 지정된 아이템에 맞춰서 각 변수에 지정
                switch (slot.acceptedItemType)
                {
                    case ItemType.Weapon:
                        weaponSlot = slot;
                        break;
                    case ItemType.Armour:
                        armourSlot = slot;
                        break;
                    case ItemType.Consumable:
                        potionSlot = slot;
                        break;
                }
            }
        }


        public void LoadGame()
        {
            if (!File.Exists(savePath))
            {
                Debug.LogError("저장된 파일이 없습니다");
                ResetGame();
                return;
            }

            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            StartCoroutine(LoadGameCoroutine(data));
        }

        private IEnumerator LoadGameCoroutine(SaveData data)
        {
            // 씬 확인
            string currentScene = SceneManager.GetActiveScene().name;
            if (data.sceneName != currentScene)
            {
                // 씬이 다르면 로드하고 기다림
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.sceneName);

                // 로딩이 끝날 때 까지 대기
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }

                // 씬 로드 후 1프레임을 더 쉬어 다른 스크립트들의 Awake() 함수가 호출될 동안 더 기다려줌
                yield return null;
            }

            // 데이터 복구
            // 플레이어 복구
            // CharacterController 가 있으면 transform.position 직접 수정이 안 먹힐 수 있어서 꺼야함
            if (player != null)
            {
                player.cController.enabled = false;
                player.transform.position = data.playerPos;
                player.cController.enabled = true;
            }

            // 체력 복구 
            if (playerHealth != null)
            {
                playerHealth.SetHealth(data.currentHp); ;
            }

            // 인벤토리 복구
            if (inventory != null)
            {
                inventory.slots.Clear();

                foreach (var itemData in data.inventoryItems)
                {
                    Item item = ItemDataBase.Instance.GetItemId(itemData.itemId);
                    if (item != null)
                    {
                        inventory.AddItem(item, itemData.amount);
                    }
                }
                inventory.ForceUpdateUI();
            }
            // 퀵슬롯 복구
            if (weaponSlot != null) LoadQuickSlot(weaponSlot, data.quickSlotItems[0]);
            if (armourSlot != null) LoadQuickSlot(armourSlot, data.quickSlotItems[1]);
            if (potionSlot != null) LoadQuickSlot(potionSlot, data.quickSlotItems[2]);

            if (questManager != null)
            {
                questManager.activeQuests.Clear();
                questManager.completedQuests = data.completedQuestNames;

                foreach (var qData in data.activeQuests)
                {
                    QuestSO so = Resources.Load<QuestSO>("Quests/" + qData.questName);          // Resources 안에 Quests 안에 questName 을 기준으로 찾음

                    if (so != null)
                    {
                        Quest restoredQuest = new Quest(so);
                        restoredQuest.currentCount = qData.currentCount;

                        questManager.activeQuests.Add(restoredQuest);
                    }
                    else
                    {
                        Debug.LogError("저장된 퀘스트 Scriptable Object 를 찾을 수 없습니다");
                    }
                }
                questManager.ForceUpdateUI();
            }

            // NPC 상태 복구 
            if (questManager != null)
            {
                questManager.npcStateDict.Clear();
                foreach (var npcData in data.npcDataList)
                {
                    QuestState state = (QuestState)npcData.questStateIndex;
                    questManager.SaveNpcState(npcData.npcID, npcData.hasMet, state);
                }
            }

            // 저장된 파일에서 사망자 명단 복구
            deadMonsterIDs = new List<string>(data.deadMonsterIDs);


            PlayerSceneConnector sceneConnector = player.GetComponent<PlayerSceneConnector>();

            sceneConnector.ConnectToSceneComponents();

            Debug.Log("Game Loaded , Scene" + data.sceneName);

            OnLoadGame.Invoke();
        }

        public void DisplayBtn()
        {
            Debug.Log("Display Button is Pressed");
        }
        public void SoundBtn()
        {
            Debug.Log("Sound Button is Pressed");
        }

        // 퀵슬롯 아이템 로드하기
        private void LoadQuickSlot(QuickSlot_Ui slot, ItemSaveData data)
        {
            slot.ClearSlot();

            if (!string.IsNullOrEmpty(data.itemId))
            {
                Item item = ItemDataBase.Instance.GetItemId(data.itemId);
                if (item != null) slot.EquipItem(item);
            }
        }

        private void DeleteGame()
        {
            if (File.Exists(savePath))
            {
                try
                {
                    File.Delete(savePath);
                    Debug.Log("세이브 파일이 성공적으로 삭제되었습니다: " + savePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("파일 삭제 중 오류 발생: " + e.Message);
                }
            }
            else
            {
                Debug.LogWarning("삭제할 세이브 파일이 존재하지 않습니다.");
            }
        }


        public void ResetGame()
        {
            DeleteGame();

            if (Instance != null)
                Destroy(Instance.gameObject);

            if (FieldLevelManager.Instance != null)
                Destroy(FieldLevelManager.Instance.gameObject);

            if (InGameUI.Instance != null)
                Destroy(InGameUI.Instance.gameObject);

            if (VfxManager.Instance != null)
                Destroy(VfxManager.Instance.gameObject);

            if (UiManager.Instance != null)
                Destroy(UiManager.Instance.gameObject);

            if (DialogueManager.Instance != null)
                Destroy(DialogueManager.Instance.gameObject);

            if (DuskbornSceneManager.Instance != null)
                Destroy(DuskbornSceneManager.Instance.gameObject);

            if (ItemDataBase.Instance != null)
                Destroy(ItemDataBase.Instance.gameObject);

            if (MonsterDataParsing.Instance != null)
                Destroy(MonsterDataParsing.Instance.gameObject);

            if (NotificationManager.Instance != null)
                Destroy(NotificationManager.Instance.gameObject);

            if (NpcCanvasUI.Instance != null)
                Destroy(NpcCanvasUI.Instance.gameObject);

            if (PlayerSceneConnector.Instance != null)
                Destroy(PlayerSceneConnector.Instance.gameObject);

            if (QuestManager.Instance != null)
                Destroy(QuestManager.Instance.gameObject);

            if (SystemCanvasUI.Instance != null)
                Destroy(SystemCanvasUI.Instance.gameObject);

            SceneManager.LoadScene(0);
        }

        public void ExitBtn()
        {
            Debug.Log("Exit Button is Pressed");

            // --- 게임 종료 로직 ---

            // 1. 실제 빌드된 게임(PC, Mac 등)에서 종료할 때 사용
            Application.Quit();

            // 2. 유니티 에디터에서 플레이 모드를 중지할 때 사용
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            // 빌드된 게임 종료
            Application.Quit();
#endif
        }
    }
}
