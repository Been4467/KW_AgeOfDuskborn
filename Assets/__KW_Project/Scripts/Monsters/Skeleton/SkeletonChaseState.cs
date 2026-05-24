using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KW
{
    public class SkeletonChaseState : MonsterBaseState<SkeletonController>
    {
        public override void EnterState(SkeletonController controller)
        {
            // Debug.Log("스켈레톤 상태 진입 : Chasing");

            controller.agent.isStopped = false;

            controller.anim.SetBool("isChase", true);
           
            controller.agent.speed = controller.chaseSpeed;

            controller.agent.stoppingDistance = 1;                                          // 플레이어한테 너무 붙지 않게끔 1로 설정(patrol에서 다시 0 으로 해줌)

            if (controller.target != null)
            {
                PlayerMovement player = controller.target.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    player.RegisterChaser();
                }
            }
        }   

        public override void UpdateState(SkeletonController controller)
        {
            if (controller.isDoingLunge) return;

            // 타겟이 없으면 patrol로 다시 돌아감 (여기서 SwitchState 시 ExitState가 자동 호출됨)
            if (controller.target == null)
            {
                controller.SwitchState(controller.patrolState);
                return;
            }

            controller.agent.SetDestination(controller.target.position);
            float distanceToTarget = Vector3.Distance(controller.transform.position, controller.target.position);

            if (distanceToTarget <= controller.attackRange)
            {
                controller.SwitchState(controller.attackState);
                return;
            }

            if (distanceToTarget > controller.detectRange)
            {
                // ◀ target을 null로 밀기 전에 ExitState에서 조회가 가능하도록 순서 유지
                controller.SwitchState(controller.patrolState);
                controller.target = null;
                return;
            }

            if (controller.agent.velocity.sqrMagnitude < 0.01f) return;

            Vector3 normalizedVelocity = controller.agent.velocity.normalized;
            controller.lastDirection = new Vector2(normalizedVelocity.x, normalizedVelocity.z);

            if (controller.agent.velocity.x > 0.1f) controller.sr.flipX = false;
            else if (controller.agent.velocity.x < -0.1f) controller.sr.flipX = true;

            float blendTreeMoveX = Mathf.Abs(normalizedVelocity.x);
            controller.anim.SetFloat("xInput", blendTreeMoveX);
            controller.anim.SetFloat("zInput", normalizedVelocity.z);

            if (controller.target != null)
            {
                if (controller.target.gameObject.layer == LayerMask.NameToLayer("Default"))
                {
                    // ◀ 플레이어가 죽었을 때도 상태 전환을 통해 ExitState가 실행되도록 유도
                    controller.SwitchState(controller.patrolState);
                    controller.target = null;
                    return;
                }
            }
        }

        public override void ExitState(SkeletonController controller)
        {
            controller.anim.SetBool("isChase", false);
            controller.anim.ResetTrigger("isAttack");

            if (controller.target != null)
            {
                PlayerMovement player = controller.target.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    player.UnregisterChaser();
                }
            }
        }
    }
}
