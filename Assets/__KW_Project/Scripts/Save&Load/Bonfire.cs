using UnityEngine;

namespace KW
{
    public class Bonfire : MonoBehaviour, IInteractable
    {
        [Header("정보")]
        // 고유 식별자
        public string BonfireID;

        private bool _isActivated = false;
        public bool isActivated
        {
            get { return _isActivated; }
            set
            {
                // 값이 바뀔 때만 로그 찍기
                if (_isActivated != value)
                {
                    // 누가 바꿨는지 추적하기 위해 스택 트레이스 출력
                    Debug.Log($"[감시] isActivated 값이 변경됨: {_isActivated} -> {value}\n{System.Environment.StackTrace}");
                }
                _isActivated = value;
            }
        }

        public void Interact(GameObject player)
        {
            // if (DialogueManager.isDialogueActive) return;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement == null || movement.isSitting) return;

            if (_isActivated == false)
            {
                isActivated = true;
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowMessage($"{BonfireID}\n화톳불이 활성화 되었습니다.", () =>
                   {
                       EnterRestMode(player, movement);
                   });
                }
            }
            else
                EnterRestMode(player, movement);
        }

        private void EnterRestMode(GameObject player, PlayerMovement movement)
        {
            if (UiManager.Instance != null)
                UiManager.Instance.StartEnterRestMode();

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(health.maxHp);
                Debug.Log("화톳불 휴식 : 체력이 모두 회복되었습니다");
            }

            movement.SwitchState(movement.sitState);
        }

    }
}
