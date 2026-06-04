using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;                                                           // 이벤트 Action 을 위해 필요함


namespace KW
{    
    public class Inventory : MonoBehaviour
    {
        public event Action OnInventoryChanged;
        public event Action<Item, int> OnItemAdded;                     // 팝업 Ui를 위함

        public List<InventorySlot> slots = new List<InventorySlot>();

        public int maxSlots = 50;                                       // 가방 최대 칸 수

        private void Start()
        {
            // 🛡️ 가짜 방어막: 내가 진짜 플레이어 몸통에 붙은 게 아니라면, SaveManager를 오염시키지 말고 즉시 탈출!
            if (PlayerSceneConnector.Instance != null && PlayerSceneConnector.Instance.gameObject != this.gameObject)
            {
                return;
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.inventory = this;
            }
        }

        private void OnDestroy()
        {
            OnInventoryChanged = null;
            OnItemAdded = null;
        }

        // 아이템이 한개일때만 AddItem(itemToAdd, amountToAdd) 호출
        public bool AddItem(Item itemToAdd)
        {
            return AddItem(itemToAdd, 1);
        }

        public bool AddItem(Item itemToAdd, int amountToAdd)
        {
            int totalAddedItem = 0;                                     // 추가 완료된 아이템의 갯수 (Ui 창에서 숫자를 위함)
            int amountRemaining = amountToAdd;                          // 추가해야될 아이템 수 = 추가할 아이템 수

            // 포션 처럼 1개 이상 겹칠 수 있는 지 아이템인 경우
            if (itemToAdd.maxStack > 1)
            {
                foreach (InventorySlot slot in slots)                   // 각 인벤토리 칸에서 같은 아이템을 찾음
                {
                    if (slot.item == itemToAdd && slot.stack < slot.item.maxStack)  // maxStack 보다 적게 있을 시
                    {
                        int spaceAvailable = slot.item.maxStack - slot.stack;
                        int amountToFill = Mathf.Min(amountRemaining, spaceAvailable);

                        slot.stack += amountToFill;
                        amountRemaining -= amountToFill;
                        totalAddedItem += amountToFill;

                        if (amountRemaining <= 0) break;                // 추가할 아이템 수 가 다 채워지면 멈춤
                    }
                }

                // 추가해야될 아이템이 남아있을 경우 (1개 이상)
                while (amountRemaining > 0)
                {
                    // 인벤토리 칸이 남아있을 경우
                    if (slots.Count < maxSlots)
                    {
                        int amountForNewSlot = Mathf.Min(amountRemaining, itemToAdd.maxStack);

                        InventorySlot newSlot = new InventorySlot(itemToAdd, amountForNewSlot);
                        slots.Add(newSlot);
                        amountRemaining -= amountForNewSlot;
                        totalAddedItem += amountForNewSlot;
                    }
                    // 인벤토리 꽉 참 ( 추후에 인벤토리에 자리가 없음 팝업Ui 로직 추가 )
                    else
                    {
                        break;
                    }
                }
            }

            else        // 스택 불가능한 아이템 (무기, 방어구)
            {
                // 겹칠 수 없는 아이템은 요청한 수량(amountToAdd)만큼 1개씩 추가
                for (int i = 0; i < amountToAdd; i++)
                {
                    // 겹칠 수 없는 아이템인 경우
                    if (slots.Count < maxSlots)                         // 인벤토리의 여유 공간 확인
                    {
                        InventorySlot newSlot = new InventorySlot(itemToAdd, 1);
                        slots.Add(newSlot);
                        amountRemaining--;
                        totalAddedItem++;
                    }
                    else
                    {
                        break;                                          // 인벤토리 꽉참
                    }
                }
            }
            if(totalAddedItem > 0)
            {
                Debug.Log($"{itemToAdd} 가 {totalAddedItem}개 추가됨");

                OnItemAdded?.Invoke(itemToAdd, totalAddedItem);         // 팝업 Ui 에서 사용

                OnInventoryChanged?.Invoke();                           // 인벤토리 Ui 에서 사용
            }
          
            if(amountRemaining <= 0)
            {
                return true;                                            // 위의 로직이 다 성공됨
            }
            else
            {
                Debug.LogWarning($"인벤토리 공간 부족. {itemToAdd} {amountRemaining} 개를 추가하지 못함.");
                return false;
            }
        }

        // 인벤토리에서 아이템을 제거하는 함수
        public void RemoveItem(Item itemToRemove)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == itemToRemove)
                {
                    // 아이템이 1개 이상이면 한 개 감소
                    if (slots[i].stack > 1)
                    {
                        slots[i].stack--;
                    }
                    // 1개면 슬롯 삭제
                    else
                    {
                        slots.RemoveAt(i);
                    }

                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        // 특정 아이템의 소지 갯수를 반환하는 함수
        public int GetItemCount(Item itemToCheck)
        {
            int count = 0;                          // 아이템을 세기 위한 함수

            // 모든 인벤토리 슬롯을 검사함
            foreach (var slot in slots)
            {
                // 체크할 아이템이 매개변수로 받아온 아이템이면
                if (slot.item == itemToCheck)
                {
                    count += slot.stack;
                }
            }

            return count;
        }

        // 특정 아이템을 N개 삭제하는 함수
        public void RemoveItemQuantity(Item itemToRemove, int amountToRemove)
        {
            // 모든 슬롯을 검사
            for (int i = 0; i < slots.Count; i++)
            {
                if (amountToRemove <= 0) break;

                // 제거할 아이템인지 확인
                if (slots[i].item == itemToRemove)
                {
                    // 현재 소지갯수가 제거해야될 양 보다 많으면
                    if (slots[i].stack > amountToRemove)
                    {
                        // 제거해야되는 만큼 제거해줌
                        slots[i].stack -= amountToRemove;

                        amountToRemove = 0;
                    }
                    else
                    {
                        // 슬롯에 있는 갯수가 똑같거나, 다 제거해도 부족할 때
                        amountToRemove -= slots[i].stack;
                        slots.RemoveAt(i);                          // 슬롯을 제거
                        i--;                                        // 리스트 크기가 줄었으니 인덱스 조절
                    }
                }
            }
            OnInventoryChanged.Invoke();
        }
        
        public void ForceUpdateUI()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}


