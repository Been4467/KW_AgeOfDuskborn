using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class PlayerDashState : MovementBaseState
    {
        private Coroutine dashCoroutine;

        public override void EnterState(PlayerMovement movement)
        {
            movement.isDashing = true;

            movement.anim.SetTrigger("isDashing");
            movement.anim.SetFloat("lastMoveX", movement.lastMoveX);
            movement.anim.SetFloat("lastMoveZ", movement.lastMoveZ);

            // 코루틴 참조를 저장해둡니다.
            dashCoroutine = movement.StartCoroutine(DashCoroutine(movement));
        }

        public override void UpdateState(PlayerMovement movement)
        {

        }

        public override void ExitState(PlayerMovement movement)
        {
            movement.isDashing = false;
            movement.anim.ResetTrigger("isDashing");

            // 상태를 빠져나갈 때 대시 코루틴이 여전히 돌고 있다면 안전하게 중지시킵니다.
            if (dashCoroutine != null)
            {
                movement.StopCoroutine(dashCoroutine);
                dashCoroutine = null;
            }
        }

        private IEnumerator DashCoroutine(PlayerMovement movement)
        {
            float startTime = Time.time;
            Vector3 dashDir;

            if (movement.dir.magnitude > 0.1f)
            {
                dashDir = movement.dir;
            }
            else
            {
                dashDir = new Vector3(movement.lastMoveX, 0, movement.lastMoveZ);
                if (dashDir.magnitude < 0.1f) dashDir = movement.transform.forward;
            }

            while (Time.time < startTime + movement.dashDuration)
            {
                // 캐릭터 컨트롤러가 존재할 때만 이동 처리를 하여 에러를 방지합니다.
                if (movement.cController != null)
                {
                    movement.cController.Move(movement.dashSpeed * dashDir.normalized * Time.deltaTime);
                }

                yield return null;
            }

            movement.isDashing = false;

            if (movement.currentState == movement.deathState || movement.Fatigue >= 100)
            {
                yield break; // 코루틴을 여기서 즉시 종료합니다.
            }

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f)
            {
                movement.SwitchState(movement.playerWalk);
            }
            else
            {
                movement.SwitchState(movement.playerIdle);
            }
        }
    }
}