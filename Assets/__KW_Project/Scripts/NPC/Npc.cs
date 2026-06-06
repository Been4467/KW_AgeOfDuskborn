using System.Collections.Generic; // ◀◀ 퀘스트/보상 리스트를 위해
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

namespace KW
{
    [RequireComponent(typeof(IInteractable))] // IInteractable이 필수임을 명시
    public class Npc : MonoBehaviour, IInteractable
    {
        [Header("NPC 데이터")]
        [SerializeField] private string npcID;


        [Header("대화 SO")]        
        [SerializeField] private DialogueSO firstMeetingDialogue;               // 최초 첫 만남 시 대사        
        [SerializeField] private DialogueSO questOfferDialogue;                 // 퀘스트 제안 대사       
        [SerializeField] private DialogueSO acceptedDialogue;                   // 퀘스트 수락 시 대사        
        [SerializeField] private DialogueSO declinedDialogue;                   // 퀘스트 거절했을 때 대사       
        [SerializeField] private DialogueSO acceptedLoopDialogue;               // 퀘스트 진행 중 대사 (미션 미완)      
        [SerializeField] private DialogueSO declinedLoopDialogue;               // 퀘스트 거절 후 재방문 대사        
        [SerializeField] private DialogueSO questCompletionDialogue;            // 퀘스트 완료 대사         
        [SerializeField] private DialogueSO afterQuestDialogue;                 // 퀘스트 완료 후 방문 시 대사


    /*     [Header("퀘스트 데이터")]
        [Tooltip("퀘스트 요구 사항")]
        public List<ChestSlot> questRequirements;

        [Tooltip("퀘스트 완료 시 지급할 보상")]
        public List<ChestSlot> questRewards; */

        public QuestSO questData; 

        // NPC의 현재 상태 저장 
        private bool hasMetPlayer = false;
        [SerializeField] private QuestState currentQuestState = QuestState.NotOffered;    

        [Header("내부 변수")]
        private DialogueManager _dialogueManager;
        private Inventory _playerInventory;      


        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;


        }

        // LoadState의 실행 순서때문에 Interact 안으로 해당 내용을 집어넣음
        private void LoadState()
        {
            if (QuestManager.Instance != null)
            {
                NpcData data = QuestManager.Instance.GetNpcState(npcID);

                if (data != null)
                {
                    hasMetPlayer = data.hasMetPlayer;
                    currentQuestState = data.questState;
                    Debug.Log($"[{npcID}] 기억 복구 완료: {currentQuestState}");
                }
            }
        }

        // 상태 저장 함수
        private void SaveState()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.SaveNpcState(npcID, hasMetPlayer, currentQuestState);
            }
        }

        public void Interact(GameObject player)
        {
            // ✅ 넘어온 player가 Destroyed됐을 경우 대비해 직접 찾기
            GameObject actualPlayer = player;
            if (actualPlayer == null)
            {
                actualPlayer = GameObject.FindWithTag("Player");
                Debug.LogWarning($"[{npcID}] player가 null이어서 태그로 재탐색함");
            }

            if (actualPlayer == null)
            {
                Debug.LogError($"[{npcID}] 플레이어를 찾을 수 없습니다!");
                return;
            }

            if (_playerInventory == null)
                _playerInventory = actualPlayer.GetComponent<Inventory>();


            if (_playerInventory == null)
                _playerInventory = player.GetComponent<Inventory>();

            // ✅ null 체크 강화
            if (_dialogueManager == null)
                _dialogueManager = DialogueManager.Instance;

            if (_dialogueManager == null)
            {
                Debug.LogError($"[{npcID}] DialogueManager를 찾을 수 없습니다!");
                return; // ✅ 명시적으로 차단
            }

            // QuestManager 상태 로드
            if (QuestManager.Instance != null)
            {
                NpcData data = QuestManager.Instance.GetNpcState(npcID);
                if (data != null)
                {
                    hasMetPlayer = data.hasMetPlayer;
                    currentQuestState = data.questState;
                }
            }

            // ✅ 실제 실행할 DialogueSO를 변수에 먼저 담기
            DialogueSO targetDialogue = GetTargetDialogue();

            if (targetDialogue == null)
            {
                Debug.LogError($"[{npcID}] 현재 상태({currentQuestState})에 해당하는 DialogueSO가 null입니다. Inspector를 확인하세요.");
                return;
            }

            _dialogueManager.StartDialogue(targetDialogue, this);

            Debug.Log($"[{npcID}] Interact 진입. _dialogueManager: {(_dialogueManager == null ? "NULL" : "OK")}");
        }

        // ✅ 상태별 대화 선택 로직을 별도 함수로 분리
        private DialogueSO GetTargetDialogue()
        {
            switch (currentQuestState)
            {
                case QuestState.NotOffered:
                    if (!hasMetPlayer)
                    {
                        hasMetPlayer = true;
                        SaveState();
                        return firstMeetingDialogue;
                    }
                    return questOfferDialogue;

                case QuestState.Declined:
                    return declinedLoopDialogue;

                case QuestState.Accepted:
                    bool conditionMet = QuestManager.Instance != null
                                        && QuestManager.Instance.IsQuestConditionMet(questData);
                    return conditionMet ? questCompletionDialogue : acceptedLoopDialogue;

                case QuestState.Completed:
                    return afterQuestDialogue;

                default:
                    return null;
            }
        }

        public void OnChoiceMade(DialogueChoice choice)
        {
            // 보상 지급
            if(choice.rewardItem != null)
            {
                if (_playerInventory != null)
                {
                    _playerInventory.AddItem(choice.rewardItem, choice.rewardItemCount);
                    Debug.Log($"보상 지급 완료 : {choice.rewardItem}, {choice.rewardItemCount}개 추가됨");

                    // 팝업 알림 로직 여기에도 추가

                }
            }

            // 다음 대화 확인
            DialogueSO nextDialogue = choice.nextDialogue;

            // 다음 대화 선택지를 확인하고
            if (nextDialogue == acceptedDialogue)
            {
                // 현재 상태 변경
                currentQuestState = QuestState.Accepted;

                SaveState();

                if (questData != null)
                {
                    QuestManager.Instance.AcceptQuest(questData);
                }
            }
            else if (nextDialogue == declinedDialogue)
            {
                currentQuestState = QuestState.Declined;

                SaveState();
            }
            else if (nextDialogue == afterQuestDialogue)
            {
                // 요구 아이템 가져가기
                QuestManager.Instance.SubmitQuestItems(questData);

                QuestManager.Instance.FinishQuest(questData);

                currentQuestState = QuestState.Completed;

                SaveState();
            }
            if (nextDialogue != null)
            {
                _dialogueManager.StartDialogue(nextDialogue, this);
            }
            else
            {
                
            }
        }
    }

    // NPC의 퀘스트 상태를 관리할 'Enum'
    public enum QuestState
    {
        NotOffered, // 퀘스트 제안 전
        Declined,   // 플레이어가 퀘스트를 거절함
        Accepted,   // 플레이어가 퀘스트를 수락함 (진행 중)
        Completed   // 플레이어가 퀘스트를 완료함
    }
}