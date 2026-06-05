using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace KW
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [Header("파밍")]
        public List<ChestSlot> itemsInChest = new List<ChestSlot>();

        [Header("연결")]
        [SerializeField] private NotificationManager _notificationManager;

        [Header("오디오 설정")]
        [Tooltip("상자가 열릴 때 재생할 오디오 소스를 연결해 주세요.")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("상자가 열리는 소리 오디오 클립(끼익 소리)을 넣어주세요.")]
        [SerializeField] private AudioClip _openSound;

        [HideInInspector] private Animator _anim;

        private bool _isOpen = false;                                                // 상자가 열려있는지

        public void Start()
        {
            _anim = GetComponentInChildren<Animator>();

            // 만약 인스펙터에서 AudioSource를 연결하지 않았다면 자신에게서 찾아보기 (방어 코드)
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        public void Interact(GameObject player)
        {
            if (_isOpen) return;

            // 플레이어에서 <Inventory>() 찾기
            Inventory playerInventory = player.GetComponent<Inventory>();

            _notificationManager = FindObjectOfType<NotificationManager>();

            if (playerInventory != null)
            {
                List<ChestSlot> itemSuccessfullyAdded = new List<ChestSlot>();          // 성공적으로 가져간 아이템을 담는 임시 리스트

                foreach (ChestSlot chestSlot in itemsInChest)
                {
                    bool success = playerInventory.AddItem(chestSlot.item, chestSlot.quantity);

                    if (success)
                    {
                        // 아이템 추가 성공
                        itemSuccessfullyAdded.Add(chestSlot);
                    }
                    else
                    {
                        // 인벤토리가 꽉 찼을 경우
                        Debug.LogWarning($"{chestSlot.item.itemName}을(를) 추가하지 못했습니다. (공간 부족)");
                    }
                }

                // 성공적으로 추가된 아이템들만 상자 리스트에서 제거함
                foreach (ChestSlot addedSlot in itemSuccessfullyAdded)
                {
                    itemsInChest.Remove(addedSlot);
                }

                if (itemsInChest.Count == 0)
                {
                    _isOpen = true;

                    if (_anim != null) _anim.SetTrigger("isOpen");

                    // 상자 열리는 사운드 1회 재생
                    PlayOpenSound();
                }

                if (itemSuccessfullyAdded.Count > 0 && _notificationManager != null)
                {
                    _notificationManager.ShowLootPopup(itemSuccessfullyAdded);
                }
            }
        }

        /// <summary>
        /// 상자가 열리는 오디오를 출력합니다.
        /// </summary>
        private void PlayOpenSound()
        {
            if (_audioSource != null && _openSound != null)
            {
                // PlayOneShot은 소리가 겹치거나 끊기지 않고 깔끔하게 한 번 재생할 때 가장 좋습니다.
                _audioSource.PlayOneShot(_openSound);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] AudioSource 또는 OpenSound 클립이 할당되지 않았습니다.");
            }
        }
    }
}