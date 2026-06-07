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

        public List<string> activatedBonfireIDs = new List<string>();

        private void Awake()
        {
            // 이미 할당된 instance가 내가 아니라 '다른' 오브젝트일 때만 파괴합니다.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return; // 아래 로직을 타지 않도록 리턴
            }

            // _instance가 null이거나, 이미 나로 지정되어 있다면 정상적인 초기화를 진행합니다.
            _instance = this;

            // 부모 오브젝트가 있다면 DontDestroyOnLoad가 작동하지 않으므로 부모 해제
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            savePath = Application.persistentDataPath + "/saveGame.json"; // 저장될 주소 지정
        }

        private void OnDestroy()
        {
            // 프로퍼티(Instance) 대신 private 변수(_instance)를 직접 비교하세요.
            if (_instance == this)
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

            // ✅ 활성화된 화톳불 명단 저장 데이터에 복사
            data.activatedBonfireIDs = new List<string>(activatedBonfireIDs);

            // 파일 쓰기
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
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

        public void ReApplyEquippedItems()
        {
            // 퀵슬롯 UI를 새로 검색해서 붙잡음
            FindQuickSlots();

            if (playerHealth == null)
            {
                playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                // 1. 무기 슬롯에 아이템이 있다면 데미지 재적용
                if (weaponSlot != null && weaponSlot.equippedItem is Weapon weapon)
                {
                    playerHealth.SetEquippedWeapon(weapon);
                    Debug.Log($"[SaveManager] 씬 이동 후 무기 데미지({weapon.damage}) 재적용 완료");
                }
                else
                {
                    playerHealth.SetEquippedWeapon(null);
                }

                // 2. 방어구 슬롯에 아이템이 있다면 방어율 재적용
                if (armourSlot != null && armourSlot.equippedItem is Armour armour)
                {
                    playerHealth.SetEquippedArmour(armour);
                }
                else
                {
                    playerHealth.SetEquippedArmour(null);
                }
            }
        }

        public void HandlePlayerRespawn(int currentSaveCount)
        {
            bool hasSaveFile = System.IO.File.Exists(savePath);

            // 🌟 [안전장치] 혹시라도 player 참조가 null이 되었다면 새로 찾아줍니다.
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.GetComponent<PlayerMovement>();
            }

            if (hasSaveFile)
            {
                Debug.Log("[SaveManager] 세이브 파일이 존재하므로, 기존 데이터를 로드하여 부활시킵니다.");
                LoadGame();
            }
            else
            {
                Debug.LogWarning("[SaveManager] 저장된 파일이 없습니다! 현재 씬의 기본 리스폰 자리에서 플레이어를 부활시킵니다.");

                if (player != null)
                {
                    // 🌟 대피소: 플레이어에게 직접 타겟 ID(0)를 넘겨 리스폰 메서드를 호출합니다.
                    player.RespawnPlayerAtSpawnPoint(player.defaultRespawnSubID);
                }
                else
                {
                    Debug.LogError("[SaveManager] 플레이어 인스턴스를 끝내 찾을 수 없어 기본 부활에 실패했습니다.");
                }
            }
        }


        // 1. LoadGame 메서드에 매개변수 추가 (isPortalMoving 기본값 false)
        public void LoadGame(bool isRespawn = false, bool isPortalMoving = false)
        {
            if (!File.Exists(savePath))
            {
                Debug.LogError("저장된 파일이 없습니다");
                ResetGame();
                return;
            }

            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 코루틴으로 부활 여부와 구역 이동 여부를 명확하게 넘겨줍니다.
            StartCoroutine(LoadGameCoroutine(data, isRespawn, isPortalMoving));
        }

        // 2. LoadGameCoroutine 메서드 수정
        private IEnumerator LoadGameCoroutine(SaveData data, bool isRespawn, bool isPortalMoving)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            Debug.Log($"[SaveManager] 검증 완료 -> 현재씬: {currentScene} | 세이브씬: {data.sceneName} | 구역이동: {isPortalMoving} | 부활: {isRespawn}");

            // 💡 [완벽 차단] 부활도 아니고, '구역 이동도 아닐 때'만 세이브 파일의 씬으로 이동합니다.
            if (!isRespawn && !isPortalMoving)
            {
                if (data.sceneName != currentScene)
                {
                    Debug.Log($"[SaveManager] 🟢 [일반 로드] 세이브 파일에 기록된 옛날 씬({data.sceneName})으로 이동합니다.");
                    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.sceneName);
                    while (!asyncLoad.isDone)
                    {
                        yield return null;
                    }
                    yield return null;
                }
            }
            else
            {
                // 💡 문을 통한 이동이거나 부활일 때는 파일 내부의 sceneName을 철저히 무시하고 현재 씬을 유지합니다!
                Debug.Log($"[SaveManager] 🔵 [구역이동/부활] 파일의 씬 전환을 무시하고 현재 씬({currentScene})을 유지합니다.");
            }

            // 씬 전환/유지 후 컴포넌트 새롭게 캐싱
            GameObject newPlayerObj = GameObject.FindGameObjectWithTag("Player");
            if (newPlayerObj != null)
            {
                player = newPlayerObj.GetComponent<PlayerMovement>();
                playerHealth = newPlayerObj.GetComponent<PlayerHealth>();
            }

            inventory = FindFirstObjectByType<Inventory>();
            questManager = FindFirstObjectByType<QuestManager>();
            FindQuickSlots();

            // 플레이어 위치 복구 제어
            if (player != null)
            {
                if (player.cController != null) player.cController.enabled = false;

                if (isRespawn)
                {
                    int targetID = 0;
                    player.RespawnPlayerAtSpawnPoint(targetID);
                }
                else if (isPortalMoving)
                {
                    // 💡 문을 통해 이동 중일 때는 이미 DuskbornSceneManager가 플레이어를 배치했으므로 
                    // 💡 세이브 파일의 옛날 좌표(playerPos)를 대입하지 않고 스킵합니다!
                    Debug.Log($"[SaveManager] 구역 이동이 명시되어 세이브 파일의 좌표 대입을 스킵합니다.");
                }
                else
                {
                    // 타이틀에서 이어하기 할 때만 옛날 좌표 복구
                    player.transform.position = data.playerPos;
                }

                if (player.cController != null) player.cController.enabled = true;
            }

            // 체력 복구
            if (playerHealth != null)
            {
                if (isRespawn) playerHealth.SetHealth(playerHealth.maxHp);
                else playerHealth.SetHealth(data.currentHp);
            }

            // 인벤토리 복구 (검 증발 방지)
            if (inventory != null)
            {
                inventory.slots.Clear();
                foreach (var itemData in data.inventoryItems)
                {
                    Item item = ItemDataBase.Instance.GetItemId(itemData.itemId);
                    if (item != null) inventory.AddItem(item, itemData.amount);
                }
                inventory.ForceUpdateUI();
            }

            // 퀵슬롯 복구
            if (data.quickSlotItems != null)
            {
                if (data.quickSlotItems.Count > 0 && weaponSlot != null) LoadQuickSlot(weaponSlot, data.quickSlotItems[0]);
                if (data.quickSlotItems.Count > 1 && armourSlot != null) LoadQuickSlot(armourSlot, data.quickSlotItems[1]);
                if (data.quickSlotItems.Count > 2 && potionSlot != null) LoadQuickSlot(potionSlot, data.quickSlotItems[2]);
            }

            // 퀘스트 & NPC 복구 (기존 로직 유지)
            if (questManager != null)
            {
                questManager.activeQuests.Clear();
                questManager.completedQuests = data.completedQuestNames;
                foreach (var qData in data.activeQuests)
                {
                    QuestSO so = Resources.Load<QuestSO>("Quests/" + qData.questName);
                    if (so != null)
                    {
                        Quest restoredQuest = new Quest(so) { currentCount = qData.currentCount };
                        questManager.activeQuests.Add(restoredQuest);
                    }
                }
                questManager.ForceUpdateUI();

                questManager.npcStateDict.Clear();
                foreach (var npcData in data.npcDataList)
                {
                    QuestState state = (QuestState)npcData.questStateIndex;
                    questManager.SaveNpcState(npcData.npcID, npcData.hasMet, state);
                }
            }

            deadMonsterIDs = new List<string>(data.deadMonsterIDs);

            // ✅ 활성화된 화톳불 명단 복구
            activatedBonfireIDs = new List<string>(data.activatedBonfireIDs);

            // ✅ 현재 씬에 배치된 화톳불들의 상태를 세이브 데이터 기준으로 새로고침
            RefreshBonfiresInScene();

            ReApplyEquippedItems();

            if (player != null)
            {
                PlayerSceneConnector sceneConnector = player.GetComponent<PlayerSceneConnector>();
                if (sceneConnector != null) sceneConnector.ConnectToSceneComponents();
            }

            if (isRespawn && UiManager.Instance != null)
            {
                UiManager.Instance.DoFadeOut(() =>
                {
                    UiManager.Instance.inGameUIScript?.gameObject.SetActive(true);
                    UiManager.Instance.systemCanvasUIScript?.gameObject.SetActive(false);
                });
            }

            OnLoadGame?.Invoke();
        }

        public void RefreshBonfiresInScene()
        {
            Bonfire[] bonfires = FindObjectsOfType<Bonfire>(true);
            foreach (var bonfire in bonfires)
            {
                if (string.IsNullOrEmpty(bonfire.BonfireID)) continue;

                // 세이브된 명단에 내 ID가 있다면 활성화 상태로 만듦
                if (activatedBonfireIDs.Contains(bonfire.BonfireID))
                {
                    bonfire.isActivated = true;
                }
                else
                {
                    bonfire.isActivated = false;
                }
            }
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

            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                Destroy(existingPlayer);
            }

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
