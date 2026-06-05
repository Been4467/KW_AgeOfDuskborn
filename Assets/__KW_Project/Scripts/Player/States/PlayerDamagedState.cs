using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class PlayerDamagedState : MovementBaseState
    {
        // 변수
        private float _stunDuration = 0.5f;
        private float _stunTime;
        public float stunTime => _stunTime;

        private Color originalColor;

        private float knockBackForce = 10f;
        private Vector3 knockBackDir;

        public override void EnterState(PlayerMovement movement)
        {
            Debug.Log("PlayerState : DamagedState");

            _stunTime = _stunDuration;

            // 모든 행동 정지
            movement.isAttacking = false;
            movement.isDashing = false;
            movement.anim.SetBool("isAttacking", false);
            movement.anim.SetBool("isWalking", false);
            movement.anim.SetBool("isRunning", false);

            // 붉고 반투명하게
            originalColor = movement.sr.color;
            movement.sr.color = new Color(1f, 0.5f, 0.5f, 0.7f);                // 빨강

            // Vector3 hitDir = movement.lastHisPos;

            Vector3 lastMoveDir = new Vector3(movement.lastMoveX, 0, movement.lastMoveZ);

            // 보고있던 방향을 보면서 피격 반동
            movement.anim.SetFloat("HitX", lastMoveDir.x);
            movement.anim.SetFloat("HitZ", lastMoveDir.z);


            // 피격 애니메이션
            movement.anim.SetTrigger("isHit");

            // 넉백 방향 설정
            knockBackDir = movement.lastHisPos;

            // 🌟 [여기에 추가] 맞았을 때 화면 일그러짐 이펙트를 실행하라고 명령합니다!
            PlayerHitEffect.Instance.TriggerHitEffect();

            // Debug.Log($"[피격 로그] FlipX: {movement.sr.flipX} | 원본 방향: {movement.lastHisPos} | 넉백 적용 방향: {knockBackDir}");
        }

        public override void ExitState(PlayerMovement movement)
        {
            // 색상 원상복구
            movement.sr.color = originalColor;
        }

        public override void UpdateState(PlayerMovement movement)
        {
            _stunTime -= Time.deltaTime;

            // cController 넉백 이동
            if (_stunTime > 0.2f)                                               // 처음 0.3초동안만 밀려남
            {
                movement.cController.Move(knockBackDir * knockBackForce * Time.deltaTime);                
            }
            // 복귀
            if (_stunTime <= 0)
            {               
                movement.SwitchState(movement.playerIdle);
            }
        }
    }
}
