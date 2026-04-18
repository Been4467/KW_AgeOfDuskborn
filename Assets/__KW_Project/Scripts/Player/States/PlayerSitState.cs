
using UnityEngine;

namespace KW
{
    public class PlayerSitState : MovementBaseState
    {
        public override void EnterState(PlayerMovement movement)
        {
            movement.isSitting = true;
            movement.cController.Move(Vector3.zero);
            // movement.canMove = false;

            movement.anim.SetBool("isSit", true);
            movement.sr.flipX = (movement.lastMoveX > 0);

            /*  if (SaveManager.Instance != null)
             {
                 SaveManager.Instance.SaveGame();
                 Debug.Log("화톳불에 앉아 게임을 저장했습니다.");
             }
             if (NotificationManager.Instance != null)
             {
                 string message = "게임이 저장되었습니다.\n\n" + "체력이 회복되었습니다.";
                 NotificationManager.Instance.ShowMessage(message);
             } */
        }

        public override void UpdateState(PlayerMovement movement)
        {
            // E키를 다시 누르거나, 이동 키를 누르면 일어남
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            if (Input.GetMouseButtonDown(0) || Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f)
            {
                // [수정] 여기서 변수 바꾸지 말고 바로 상태 전환만 호출
                movement.SwitchState(movement.playerIdle);
                movement.anim.SetBool("isSit", false);
            }
        }

        public override void ExitState(PlayerMovement movement)
        {
            // [수정] 나갈 때 확실하게 초기화
            movement.isSitting = false;
            movement.canMove = true; // 이동 허용
            movement.anim.SetBool("isSit", false);
        }
    }
}
