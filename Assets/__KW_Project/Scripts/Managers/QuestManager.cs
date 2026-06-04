using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 👈 🛠️ 씬 로드 이벤트를 위해 추가

namespace KW
{
    [System.Serializable]
    public class Quest
    {
        public QuestSO data;            // 원본 데이터
        public int currentCount;        // 현재 잡은 수 
        public bool isCompleted;        // 완료 여부

        public Quest(QuestSO questData)
        {
            this.data = questData;
            this.currentCount = 0;
            this.isCompleted = false;
        }
    }

    [System.Serializable]
    public class NpcData
    {
        public bool hasMetPlayer;
        public QuestState questState;

        public NpcData(bool met, QuestState state)
        {
            this.hasMetPlayer = met;
            this.questState = state;
        }
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public List<Quest> activeQuests = new List<Quest>();            // 모든 퀘스트를 담을 리스트
        public List<string> completedQuests = new List<string>();         // 완료된 퀘스트들

        // NPC 상태 저장소 (Key : NpcID, Value : 상태 데이터)
        public Dictionary<string, NpcData> npcStateDict = new Dictionary<string, NpcData>();

        public List<Quest> trackedQuests = new List<Quest>();

        public event Action OnTrackListUpdated;                         // 추적 상태 변경 시 UI에게 알림 
        public event Action OnQuestListUpdated;

        private Inventory playerInventory;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 🛠️ 버그 수정: 컴포넌트(this)만 지우면 빈 게임오브젝트 껍데기가 남으므로 gameObject를 파괴
                Destroy(gameObject);
                return; // 👈 가짜 오브젝트의 오작동을 차단하는 브레이크
            }

            // 🛠️ 안전장치: 데이터 로딩 시점 꼬임을 막기 위해 Awake에서 미리 SaveManager에 연결 시도
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.questManager = this;
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return; // 🛠️ 진짜 인스턴스만 실행하도록 필터링
            if (scene.name == "LoadingScene") return;

            // 🛠️ 핵심 수정: 씬이 바뀔 때마다 플레이어 인벤토리 참조를 확실하게 갱신 (유령 참조 방지)
            FindPlayerInventory();
        }

        private void Start()
        {
            // 백업 등록용
            if (SaveManager.Instance != null && SaveManager.Instance.questManager == null)
            {
                SaveManager.Instance.questManager = this;
            }

            FindPlayerInventory();
        }

        // 🛠️ 인벤토리 탐색 로직 분리 및 안정화
        private void FindPlayerInventory()
        {
            playerInventory = null; // 기존 구형 찌꺼기 제거

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<Inventory>();
            }
            else
            {
                Debug.LogWarning("[QuestManager] 'Player' 태그를 가진 오브젝트를 현재 씬에서 찾을 수 없습니다. (인게임 진입 후 재탐색)");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnTrackListUpdated = null;
                OnQuestListUpdated = null;

                Instance = null;
            }
        }

        // NPC 상태 로드 (start에서 호출)
        public NpcData GetNpcState(string npcID)
        {
            if (npcStateDict.ContainsKey(npcID))
            {
                return npcStateDict[npcID];
            }
            return null;
        }

        // NPC 상태 저장(NPC가 상태 변할 때 호출)
        public void SaveNpcState(string npcId, bool hasMet, QuestState state)
        {
            if (npcStateDict.ContainsKey(npcId))
            {
                npcStateDict[npcId].hasMetPlayer = hasMet;
                npcStateDict[npcId].questState = state;
            }
            else
            {
                npcStateDict.Add(npcId, new NpcData(hasMet, state));
            }
        }

        // 퀘스트 수락 함수 (NPC에서 호출)
        public void AcceptQuest(QuestSO questData)
        {
            if (activeQuests.Exists(q => q.data == questData))
            {
                Debug.LogWarning("[QuestManager] 이미 수행중인 퀘스트입니다");
                return;
            }

            Quest newQuest = new Quest(questData);
            activeQuests.Add(newQuest);

            Debug.Log($"[QuestManager] 퀘스트 수락됨 : {questData.questTitle}");

            OnQuestListUpdated?.Invoke();
        }

        // 몬스터 처치 시 호출
        public void OnMonsterKilled(string monsterId, string region)
        {
            bool isUpdated = false;

            foreach (var quest in activeQuests)
            {
                if (quest.isCompleted) continue;
                if (quest.data.type != QuestType.Kill) continue;

                bool isTargetMatch = (quest.data.targetName == monsterId);
                bool isRegionMatch = string.IsNullOrEmpty(quest.data.targetRegion) || (quest.data.targetRegion == region);

                if (isTargetMatch && isRegionMatch)
                {
                    quest.currentCount++;
                    isUpdated = true;

                    Debug.Log($"[QuestManager] 퀘스트 진행중 : {quest.data.questTitle} {quest.currentCount} / {quest.data.targetCount}");

                    if (quest.currentCount >= quest.data.targetCount)
                    {
                        CompleteQuest(quest);
                    }
                }
            }

            if (isUpdated)
            {
                OnTrackListUpdated?.Invoke();
                OnQuestListUpdated?.Invoke();
            }
        }

        public bool IsQuestConditionMet(QuestSO questData)
        {
            Quest activeQuest = activeQuests.Find(q => q.data == questData);
            if (activeQuest == null) return false;

            if (activeQuest.isCompleted) return true;

            if (questData.type == QuestType.Kill)
            {
                return activeQuest.currentCount >= questData.targetCount;
            }
            else if (questData.type == QuestType.Collect)
            {
                // 🛠️ 아이템 카운트 검사 전 인벤토리가 유실되었을 경우를 대비한 2차 안전장치
                if (playerInventory == null) FindPlayerInventory();
                if (playerInventory == null) return false;

                return playerInventory.GetItemCount(questData.requiredItem) >= questData.targetCount;
            }
            return false;
        }

        // 미션 물건 제출
        public void SubmitQuestItems(QuestSO questData)
        {
            if (questData.type == QuestType.Collect && questData.requiredItem != null)
            {
                if (playerInventory == null) FindPlayerInventory();
                if (playerInventory != null)
                {
                    playerInventory.RemoveItemQuantity(questData.requiredItem, questData.targetCount);
                }
            }
        }

        public void CompleteQuest(Quest quest)
        {
            quest.isCompleted = true;
            quest.currentCount = quest.data.targetCount;

            Debug.Log($"[QuestManager] {quest.data.questTitle} 퀘스트 조건 달성!");

            OnQuestListUpdated?.Invoke();
            OnTrackListUpdated?.Invoke();
        }

        public void FinishQuest(QuestSO questData)
        {
            Quest questToRemove = activeQuests.Find(q => q.data == questData);
            if (questToRemove != null)
            {
                if (trackedQuests.Contains(questToRemove))
                {
                    trackedQuests.Remove(questToRemove);
                    OnTrackListUpdated?.Invoke();
                }
                if (!completedQuests.Contains(questData.questTitle))
                {
                    completedQuests.Add(questData.questTitle);
                }
            }

            activeQuests.Remove(questToRemove);
            OnQuestListUpdated?.Invoke();
        }

        public void ToggleQuestTracking(Quest quest)
        {
            if (trackedQuests.Contains(quest))
            {
                trackedQuests.Remove(quest);
            }
            else
            {
                trackedQuests.Clear();
                trackedQuests.Add(quest);
            }

            OnQuestListUpdated?.Invoke();
            OnTrackListUpdated?.Invoke();
        }

        public void ForceUpdateUI()
        {
            OnQuestListUpdated?.Invoke();
            OnTrackListUpdated?.Invoke();
        }
    }
}