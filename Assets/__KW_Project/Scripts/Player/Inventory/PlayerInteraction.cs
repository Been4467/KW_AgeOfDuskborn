using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class PlayerInteraction : MonoBehaviour
    {
        public float interactionDistance = 2f;
        public KeyCode interactionKey = KeyCode.E;

        private PlayerMovement playerMovement;

        private Color successRayColor = Color.blue;
        private Color failureRayColor = Color.red;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement 를 찾을 수 없음");
            }
        }
        private void Start()
        {
            // Debug.Log("상호작용 ray 색 : Blue, 실패 시 생상 : Red");
        }

        private void Update()
        {
            if (DialogueManager.isDialogueActive)
            {
                return;
            }

            if (playerMovement.isSitting) return;

            // E 키를 누르는 '순간'
            if (Input.GetKeyDown(interactionKey))
            {
                if (playerMovement == null)
                {
                    return;
                }

                RaycastHit hit;
                Vector3 startPos = transform.position;

                Vector3 direction;
                Vector3 lastMoveDir = new Vector3(playerMovement.lastMoveX, 0, playerMovement.lastMoveZ);

                // 기존 움직임이 있었으면
                if (lastMoveDir.magnitude > 0.1f)
                {
                    direction = lastMoveDir.normalized;
                }
                else // 움직임이 없었으면 (게임을 시작한 직후 등과 같은 상황)
                {
                    direction = transform.forward;
                }

                if (Physics.Raycast(startPos, direction, out hit, interactionDistance))
                {
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                    if (interactable != null)
                    {
                        interactable.Interact(this.gameObject);
                    }

                    Debug.DrawRay(startPos, direction * hit.distance, successRayColor, 1f);
                    // Debug.Log("Interact 할 Object 를 찾음");
                }
                else
                {
                    Debug.DrawRay(startPos, direction * interactionDistance, failureRayColor, 1f);
                    // Debug.Log("Interact 할 Object 를 찾지 못함");
                }

            }
        }
    }
}
