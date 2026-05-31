using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                // 이미 있으면 갱신
                npcStateDict[npcId].hasMetPlayer = hasMet;
                npcStateDict[npcId].questState = state;
            }
            else
            {
                // 없으면 새로 추가
                npcStateDict.Add(npcId, new NpcData(hasMet, state));
            }
        }

        // 플레이어가 현재 수행 중인 퀘스트 목록
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.questManager = this;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerInventory = player.GetComponent<Inventory>();
            }
            else
            {
                Debug.LogError("[QuestManager] 'Player' 태그를 가진 게임 오브젝트를 찾지 못함");
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

        // 퀘스트 수락 함수 (NPC에서 호출)
        public void AcceptQuest(QuestSO questData)
        {
            // 중복방지
            if (activeQuests.Exists(q => q.data == questData))
            {
                Debug.LogWarning("[QuestManager] 이미 수행중인 퀘스트입니다");
                return;
            }

            Quest newQuest = new Quest(questData);
            activeQuests.Add(newQuest);

            Debug.Log($"[QuestManager]퀘스트 수락됨 : {questData.questTitle}");

            // 퀘스트 UI 업데이트
            OnQuestListUpdated?.Invoke();
        }


        // 몬스터 처치 시 호출
        public void OnMonsterKilled(string monsterId, string region)
        {
            bool isUpdated = false;

            foreach (var quest in activeQuests)
            {
                if (quest.isCompleted) continue;                            // continue 의 조건이 맞을 시, (for, foreach, while등 반복문) 이번 순서의 코드는 무시하고 다음으로 넘어가라
                if (quest.data.type != QuestType.Kill) continue;

                // 이름 확인
                bool isTargetMatch = (quest.data.targetName == monsterId);
                // 지역 확인
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

            // 진행도가 변했으면 questUI 업데이트
            if (isUpdated)
            {
                OnTrackListUpdated?.Invoke();
                OnQuestListUpdated?.Invoke();
            }
        }

        public bool IsQuestConditionMet(QuestSO questData)
        {
            // 활성화된 퀘스트인지 확인
            Quest activeQuest = activeQuests.Find(q => q.data == questData);

            if (activeQuest == null) return false;      // 받지도 않음

            if (activeQuest.isCompleted) return true;

            // 토벌 퀘스트인지 확인
            if (questData.type == QuestType.Kill)
            {
                // 현재 퀘스트의 count 가 요구 count 보다 많으면 true
                return activeQuest.currentCount >= questData.targetCount;
            }

            // 수집 퀘스트인지 확인
            else if (questData.type == QuestType.Collect)
            {
                if (playerInventory == null) return false;

                return playerInventory.GetItemCount(questData.requiredItem) >= questData.targetCount;
            }
            return false;
        }

        // 미션 물건 제출
        public void SubmitQuestItems(QuestSO questData)
        {
            // 퀘스트 타입이 수집퀘스트이고, 요구아이템이 있을 때
            if (questData.type == QuestType.Collect && questData.requiredItem != null)
            {
                // 플레이어 인벤토리에서 해당 아이템, 갯수만큼 제거
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

        /* public bool CheckQuestIsCompleted(QuestSO questData)
        {
            foreach (var quest in activeQuests)
            {
                if (quest.data == questData)
                {
                    return quest.isCompleted;
                }
            }
            return false;
        } */

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
                trackedQuests.Remove(quest);        // 이미 있으면 끄기
            }
            else
            {
                // 추적하는 퀘스트는 하나만 유지
                trackedQuests.Clear();              // 기존 것 삭제

                trackedQuests.Add(quest);           // 지금 받아온 퀘스트 추가
            }

            OnQuestListUpdated?.Invoke();
            OnTrackListUpdated?.Invoke();
        }

        public void ForceUpdateUI()
        {
            // 퀘스트 목록
            OnQuestListUpdated?.Invoke();

            // 추적중인 퀘스트 
            OnTrackListUpdated?.Invoke();
        }
    }
}
