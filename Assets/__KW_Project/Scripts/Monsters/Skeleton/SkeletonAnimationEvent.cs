using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class SkeletonAnimationEvent : MonoBehaviour
    {
        public SkeletonController skeletonController;

        private void Awake()
        {
            skeletonController = GetComponentInParent<SkeletonController>();
        }

        public void PerformAttackLunge()
        {
            skeletonController.PerformAttackLunge();
        }

        public void EnableHitBox()
        {
            if (skeletonController.hitBox != null)
            {
                skeletonController.hitBox.SetActive(true);
            }

            TriggerDodgeWindow(false);
        }

        public void DisableHitBox()
        {
            if (skeletonController.hitBox != null)
            {
                skeletonController.hitBox.SetActive(false);
            }
        }

        public void EnableIndicator()
        {
            if (skeletonController.indicator != null)
            {
                skeletonController.indicator.SetActive(true);
            }

            TriggerDodgeWindow(true);
        }

        public void DisableIndicator()
        {
            if (skeletonController.indicator != null)
            {
                skeletonController.indicator.SetActive(false);
            }

            TriggerDodgeWindow(false);
        }

        private void TriggerDodgeWindow(bool isOpen)
        {
            if (skeletonController == null) return;

            Collider[] hitPlayers = Physics.OverlapSphere(
                skeletonController.transform.position,
                skeletonController.detectRange,
                skeletonController.playerLayer
            );

            foreach (var playerCollider in hitPlayers)
            {
                PlayerMovement player = playerCollider.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    if (isOpen)
                        player.RegisterDodgeableMonster(skeletonController);
                    else
                        player.UnregisterDodgeableMonster(skeletonController);
                }
            }
        }
    }
}