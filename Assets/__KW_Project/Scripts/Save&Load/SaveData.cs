using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    [System.Serializable]
    public class SaveData
    {
        [Header("지정된 씬(Scene)) 이름")]
        public string sceneName;

        [Header("플레이어 정보")]
        public Vector3 playerPos;
        public float currentHp;

        [Header("인벤토리 아이템 목록")]
        public List<ItemSaveData> inventoryItems = new List<ItemSaveData>();         // 인벤토리 아이템        
        public List<ItemSaveData> quickSlotItems = new List<ItemSaveData>();         // 퀵슬롯 아이템

        [Header("퀘스트 정보")]
        public List<string> completedQuestNames = new List<string>();                   // 완료한 퀘스트
        public List<QuestSaveData> activeQuests = new List<QuestSaveData>();            // 진행중인 퀘스트

        // Dictionary는 JSON 저장이 안 되므로 리스트로 변환해서 저장
        public List<NpcSaveData> npcDataList = new List<NpcSaveData>();

        [Header("몬스터 사망 후 보관 리스트")]
        public List<string> deadMonsterIDs = new List<string>();

        [Header("활성화된 화톳불 ID 리스트")]
        public List<string> activatedBonfireIDs = new List<string>();
    }

    [System.Serializable]
    public class NpcSaveData
    {
        public string npcID;
        public bool hasMet;
        public int questStateIndex; // Enum을 정수로 저장
    }

    [System.Serializable]
    public class ItemSaveData
    {
        public string itemId;                           // 아이템 Id
        public int amount;                              // 수량
        public int slotIndex;                           // 몇 번째 (인벤토리) 칸인지
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public string questName;                        // 퀘스트 식별자
        public int currentCount;                        // 현재 진행도
    }

  
}
