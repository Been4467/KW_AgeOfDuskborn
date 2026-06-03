using System.Collections;
using UnityEngine;

public class BossIntroDirectCamera : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bossTransform;
    [SerializeField] private Transform mainCameraTransform;

    [Header("UI Objects")]
    [Tooltip("BossHPBar를 제외한 나머지 일반 인게임 UI들이 담긴 부모 오브젝트")]
    [SerializeField] private GameObject generalInGameUI;

    [Tooltip("BossHPBar 게임 오브젝트")]
    [SerializeField] private GameObject bossHPBarObject;

    [Header("Camera Script Setting (해결방법 2번용)")]
    [Tooltip("메인 카메라에 붙어있는 기존 플레이어 추적 스크립트의 정확한 클래스 이름 (예: CameraFollow)")]
    [SerializeField] private string cameraScriptClassName;

    // 내부 연출용으로 사용할 컴포넌트 변수들
    private CanvasGroup bossHPBarCanvasGroup;
    private MonoBehaviour originalCameraScript; // 기존 카메라 스크립트를 담아둘 변수

    [Header("Settings")]
    [SerializeField] private float cameraMoveSpeed = 2f;
    [SerializeField] private float uiFadeSpeed = 1f; // 보스 체력바 페이드인 속도

    private void Start()
    {
        // 🌟 [해결방법 2] 메인 카메라에서 기존 추적 스크립트를 찾아서 연출 동안 잠시 끕니다.
        if (mainCameraTransform != null && !string.IsNullOrEmpty(cameraScriptClassName))
        {
            originalCameraScript = mainCameraTransform.GetComponent(cameraScriptClassName) as MonoBehaviour;
            if (originalCameraScript != null)
            {
                originalCameraScript.enabled = false; // 기존 추적 일시정지
                Debug.Log($"[보스 연출] {cameraScriptClassName} 스크립트를 일시 중지했습니다.");
            }
            else
            {
                Debug.LogWarning($"[보스 연출 경고] 메인 카메라에서 '{cameraScriptClassName}' 스크립트를 찾을 수 없습니다. 이름을 확인해주세요.");
            }
        }

        // 할당된 게임 오브젝트에서 CanvasGroup 컴포넌트를 동적으로 가져옵니다.
        if (bossHPBarObject != null)
        {
            bossHPBarCanvasGroup = bossHPBarObject.GetComponent<CanvasGroup>();

            if (bossHPBarCanvasGroup == null)
            {
                bossHPBarCanvasGroup = bossHPBarObject.AddComponent<CanvasGroup>();
            }
        }

        // 1. 시작하자마자 보스 체력바는 투명하게 초기화
        if (bossHPBarCanvasGroup != null)
        {
            bossHPBarCanvasGroup.alpha = 0f;
        }

        // 2. 보스전 진입 몰입감을 위해 일반 인게임 UI (Status, 퀘스트창 등) 전체 비활성화
        if (generalInGameUI != null)
        {
            generalInGameUI.SetActive(false);
        }

        StartCoroutine(CameraRoutine());
    }

    private IEnumerator CameraRoutine()
    {
        // 1. 보스방 진입 후 1초 대기
        yield return new WaitForSeconds(1f);

        // 2. 카메라를 움직여서 보스 비추기
        yield return StartCoroutine(MoveCameraTo(bossTransform.position));

        // 3. 보스를 비춘 상태로 3초 대기
        yield return new WaitForSeconds(3f);

        // 4. [3초 대기 직후] 보스 체력바 페이드인 시작!
        if (bossHPBarCanvasGroup != null)
        {
            StartCoroutine(FadeInBossUI());
        }

        // 5. 다시 카메라 움직여서 플레이어에게로 돌아가기
        yield return StartCoroutine(MoveCameraTo(playerTransform.position));

        // 6. 카메라가 플레이어에게 완전히 돌아온 후, 기존 인게임 UI 다시 활성화
        if (generalInGameUI != null)
        {
            generalInGameUI.SetActive(true);
        }

        // 🌟 [해결방법 2] 연출이 완벽히 끝났으므로 기존 플레이어 추적 스크립트를 다시 켜줍니다.
        if (originalCameraScript != null)
        {
            originalCameraScript.enabled = true;
            Debug.Log($"[보스 연출] {cameraScriptClassName} 스크립트를 다시 활성화했습니다.");
        }
    }

    // 부드러운 카메라 이동 코루틴 (기존 기능 유지)
    private IEnumerator MoveCameraTo(Vector3 targetPosition)
    {
        Vector3 startPos = mainCameraTransform.position;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, startPos.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * cameraMoveSpeed;
            mainCameraTransform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        mainCameraTransform.position = endPos;
    }

    // 보스 UI를 부드럽게 등장시키는 페이드인 코루틴 (기존 기능 유지)
    private IEnumerator FadeInBossUI()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * uiFadeSpeed;
            bossHPBarCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        bossHPBarCanvasGroup.alpha = 1f;
    }
}