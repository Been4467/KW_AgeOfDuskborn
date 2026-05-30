using UnityEngine;
using Cinemachine;

public class CameraBlock : CinemachineExtension
{
    private CinemachineVirtualCamera vcam;
    private CinemachineCollider cmCollider;

    private bool isStopped = false;

    // 정지했을 때 고정할 위치와 회전값
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;

    protected override void Awake()
    {
        base.Awake();
        vcam = GetComponent<CinemachineVirtualCamera>();
        cmCollider = GetComponent<CinemachineCollider>();
    }

    void LateUpdate()
    {
        if (vcam == null || cmCollider == null) return;

        if (cmCollider.IsTargetObscured(vcam))
        {
            if (!isStopped)
            {
                // 장애물 감지 시작 순간의 카메라 위치를 박제
                lockedPosition = vcam.transform.position;
                lockedRotation = vcam.transform.rotation;

                isStopped = true;
            }
        }
        else
        {
            if (isStopped)
            {
                isStopped = false;
            }
        }
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcamBase,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            if (isStopped)
            {
                state.RawPosition = lockedPosition;
                state.RawOrientation = lockedRotation;
            }
        }
    }
}