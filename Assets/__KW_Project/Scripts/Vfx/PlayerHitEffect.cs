using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHitEffect : MonoBehaviour
{
    public static PlayerHitEffect Instance { get; private set; }

    [SerializeField] private Volume globalVolume;

    private ChromaticAberration chromaticAberration;
    private Vignette vignette;

    // 복구용 기본값 저장 변수
    private Color defaultVignetteColor;
    private float defaultVignetteIntensity;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (globalVolume == null) return;

        // 1. 크로마틱 애버레이션 가져오기
        if (globalVolume.profile.TryGet<ChromaticAberration>(out var ca))
        {
            chromaticAberration = ca;
            chromaticAberration.intensity.value = 0f;
        }

        // 2. 비네트 가져오기 및 초기값 저장
        if (globalVolume.profile.TryGet<Vignette>(out var vig))
        {
            vignette = vig;
            defaultVignetteColor = vignette.color.value;          // 평소 색상 (예: 검은색)
            defaultVignetteIntensity = vignette.intensity.value;  // 평소 강도 (예: 0.3f)
        }
    }

    public void TriggerHitEffect()
    {
        Debug.Log("💥 피격 이펙트 함수 호출 (크로마틱 + 레드 비네트)");

        // 둘 중 하나라도 설정되어 있다면 코루틴 실행
        if (chromaticAberration != null || vignette != null)
        {
            StopAllCoroutines();
            StartCoroutine(HitEffectRoutine());
        }
    }

    private IEnumerator HitEffectRoutine()
    {
        float duration = 0.5f; // 👈 지속 시간을 0.5초 정도로 잡으면 적당합니다.
        float elapsedTime = 0f;

        // [시작점 설정] 
        if (chromaticAberration != null) chromaticAberration.intensity.value = 1f;
        if (vignette != null)
        {
            vignette.color.value = Color.red; // 붉은색(FF0000)으로 변경
            vignette.intensity.value = 0.1f; // 피격 순간 외곽을 더 어둡고 붉게 압박 (수치 조절 가능)
        }

        // [루프] 시간 동안 서서히 원래대로 복구
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 크로마틱 애버레이션 복구 (1 -> 0)
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(1f, 0f, t);
            }

            // 비네트 복구 (붉은색 -> 원래 색상 / 강한 강도 -> 원래 강도)
            if (vignette != null)
            {
                vignette.color.value = Color.Lerp(Color.red, defaultVignetteColor, t);
                vignette.intensity.value = Mathf.Lerp(0.45f, defaultVignetteIntensity, t);
            }

            yield return null;
        }

        // [마무리] 확실하게 원상복구
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
        if (vignette != null)
        {
            vignette.color.value = defaultVignetteColor;
            vignette.intensity.value = defaultVignetteIntensity;
        }
    }
}