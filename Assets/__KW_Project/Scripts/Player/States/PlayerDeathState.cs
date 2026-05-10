using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class PlayerDeathState : MovementBaseState
    {
        private int originalLayer;
        
        public override void EnterState(PlayerMovement movement)
        {
            originalLayer = movement.gameObject.layer;

            movement.gameObject.layer = LayerMask.NameToLayer("Default");

            // 플레이어 이동 불가
            movement.canMove = false;
            movement.isAttacking = false;
            movement.isDashing = false;

            movement.cController.enabled = false;

            // 기존 애니메이터 파라미터 초기화
            movement.anim.SetBool("isWalking", false);
            movement.anim.SetBool("isRunning", false);
            movement.anim.SetBool("isAttacking", false);

            movement.anim.SetTrigger("isDead");
        }

        public override void UpdateState(PlayerMovement movement)
        {

        }

        public override void ExitState(PlayerMovement movement)
        {

            movement.gameObject.layer = originalLayer;
            // 부활 시스템을 위한 강제 초기화
            movement.canMove = true;
            //movement.anim.ResetTrigger("Die");
            movement.anim.Play("Idle"); // 강제 복귀            
        }
    }
}
