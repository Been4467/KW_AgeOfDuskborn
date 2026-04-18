using UnityEngine;

namespace KW
{
    public class Bonfire : MonoBehaviour, IInteractable
    {
        public void Interact(GameObject player)
        {
            // if (DialogueManager.isDialogueActive) return;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();

            if (movement.isSitting) return;

            if (movement != null)
            {
                // 플레이어를 앉기 상태로 전환
                movement.SwitchState(movement.sitState);

                PlayerHealth health = player.GetComponent<PlayerHealth>();

                if (health != null)
                {
                    health.Heal(health.maxHp);
                    Debug.Log("화톳불 휴식 : 체력이 모두 회복되었습니다");
                }

            }
        }
    }
}
