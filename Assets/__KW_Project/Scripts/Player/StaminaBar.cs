using UnityEngine;
using UnityEngine.UI;

namespace KW
{
    public class StaminaBar : MonoBehaviour
    {
        [Header("UI 컴포넌트 연결")]
        [Tooltip("Image Type이 'Filled'로 설정된 게이지 이미지를 넣어주세요.")]
        [SerializeField] private Image _fillImage;

        [Header("게이지 모드 설정")]
        [Tooltip("체크하면 스태미나 바(지치면 감소, 회복 시 차오름)\n체크 해제하면 피로도 바(지치면 차오름)")]
        [SerializeField] private bool _isStaminaMode = true;

        [Header("연출 설정")]
        [Tooltip("게이지가 점진적으로 차오르는 속도입니다. (값이 높을수록 빠르게 변함)")]
        [SerializeField] private float _smoothSpeed = 5f;

        private void Start()
        {
            // 인스펙터에서 할당 안 했을 경우 자신에게서 Image 컴포넌트 자동 탐색
            if (_fillImage == null)
            {
                _fillImage = GetComponent<Image>();
            }
        }

        private void Update()
        {
            // 씬에 플레이어가 아직 생성되지 않았거나 파괴되었다면 리턴
            if (PlayerMovement.Instance == null) return;

            // 1. 플레이어의 현재 피로도 가져오기
            float currentFatigue = PlayerMovement.Instance.Fatigue;
            float maxFatigue = 100f; // PlayerMovement의 maxFatigue 기본값인 100에 맞춤

            // 2. 피로도 비율 계산 (0.0 ~ 1.0)
            float fatigueRatio = currentFatigue / maxFatigue;

            // 3. 모드에 따른 목표 Fill Amount 설정
            float targetFill = 0f;
            if (_isStaminaMode)
            {
                // 스태미나 모드: 피로도가 0일 때 게이지 100%(1.0), 피로도가 100일 때 게이지 0%(0.0)
                targetFill = 1f - fatigueRatio;
            }
            else
            {
                // 피로도 모드: 피로도가 0일 때 게이지 0%(0.0), 피로도가 100일 때 게이지 100%(1.0)
                targetFill = fatigueRatio;
            }

            // 4. Mathf.Lerp를 이용해 현재 게이지에서 목표 게이지로 점진적(부드럽게) 이동
            _fillImage.fillAmount = Mathf.Lerp(_fillImage.fillAmount, targetFill, Time.deltaTime * _smoothSpeed);
        }
    }
}