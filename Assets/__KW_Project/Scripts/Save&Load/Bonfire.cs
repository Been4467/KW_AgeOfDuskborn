using System.Collections; // 코루틴 사용을 위해 추가
using UnityEngine;
using UnityEngine.UI;    // UI Image 컴포넌트 제어를 위해 추가

namespace KW
{
    public class Bonfire : MonoBehaviour, IInteractable
    {
        [Header("정보")]
        // 고유 식별자
        public string BonfireID;

        [Header("연결된 오브젝트")]
        [Tooltip("화톳불의 불꽃 역할을 하는 자식 Quad 오브젝트를 넣어주세요.")]
        public GameObject fireQuad;

        [Header("다크소울 스타일 UI 연출 설정")]
        [Tooltip("Canvas에 배치한 'LOST BONFIRE DISCOVERED' Image 오브젝트를 연결해 주세요.")]
        public Image discoveryImage;

        [Tooltip("처음 번쩍할 때 이미지 전체에 입힐 글자 색상(주황색/금색)을 지정하세요.")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.55f, 0.12f, 1f);

        [Header("사운드 추가 설정")]
        [Tooltip("소리를 출력할 오디오 소스를 연결하세요. (비어있으면 자동으로 이 오브젝트에서 찾습니다)")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("화톳불이 활성화될 때 재생할 웅장한 효과음(WAV/MP3)을 넣어주세요.")]
        [SerializeField] private AudioClip _discoverSound;

        [Header("연출 세부 시간(초)")]
        [SerializeField] private float _fadeInDuration = 0.15f;  // 0 -> 255까지 번쩍 뜨는 시간
        [SerializeField] private float _dipDuration = 0.12f;     // 255 -> 230으로 떨어지며 원래 색으로 돌아오는 시간
        [SerializeField] private float _stayDuration = 1.5f;     // 요구사항: 230 상태에서 1.5초 대기
        [SerializeField] private float _fadeOutDuration = 0.6f;  // 230 -> 0으로 서서히 사라지는 시간

        private Coroutine _uiEffectCoroutine;

        private bool _isActivated = false;
        public bool isActivated
        {
            get { return _isActivated; }
            set
            {
                // 값이 바뀔 때만 로직 실행
                if (_isActivated != value)
                {
                    // 누가 바꿨는지 추적하기 위해 스택 트레이스 출력
                    Debug.Log($"[감시] isActivated 값이 변경됨: {_isActivated} -> {value}\n{System.Environment.StackTrace}");

                    // 상태가 변경될 때 불꽃 오브젝트 켜고 끄기
                    UpdateFireState(value);
                }
                _isActivated = value;
            }
        }

        private void Start()
        {
            // 게임 시작 시 현재 활성화 상태에 맞춰 불꽃 상태 초기화
            UpdateFireState(_isActivated);

            // 오디오 소스가 인스펙터에서 비어있다면 컴포넌트 자동 캐싱
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            // 게임 시작 시 UI가 화면에 보이지 않도록 초기화
            if (discoveryImage != null)
            {
                discoveryImage.color = new Color(1f, 1f, 1f, 0f);
                discoveryImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 화톳불 활성화 상태에 따라 자식 Quad의 가시성을 조절합니다.
        /// </summary>
        private void UpdateFireState(bool isActive)
        {
            if (fireQuad != null)
            {
                fireQuad.SetActive(isActive);
            }
            else
            {
                Debug.LogWarning($"[{BonfireID}] Bonfire 스크립트에 Fire Quad가 할당되지 않았습니다!");
            }
        }

        public void Interact(GameObject player)
        {
            // if (DialogueManager.isDialogueActive) return;

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement == null || movement.isSitting) return;

            if (_isActivated == false)
            {
                isActivated = true; // 프로퍼티를 통해 세터(set) 호출 -> 불꽃이 켜짐

                // UI 연출 코루틴 안전하게 시작
                if (discoveryImage != null)
                {
                    if (_uiEffectCoroutine != null) StopCoroutine(_uiEffectCoroutine);
                    _uiEffectCoroutine = StartCoroutine(DiscoveryUiRoutine());
                }

                //if (NotificationManager.Instance != null)
                //    NotificationManager.Instance.ShowMessage($"{BonfireID}\n화톳불이 활성화 되었습니다.");
            }
            else
                EnterRestMode(player, movement);
        }

        private void EnterRestMode(GameObject player, PlayerMovement movement)
        {
            if (UiManager.Instance != null)
                UiManager.Instance.StartRestMode();

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(health.maxHp);
                Debug.Log("화톳불 휴식 : 체력이 모두 회복되었습니다");
            }

            movement.SwitchState(movement.sitState);
        }

        /// <summary>
        /// 요구하신 사양대로 작동하는 투명도 + 색상 변환 페이드 연출 코루틴입니다.
        /// </summary>
        private IEnumerator DiscoveryUiRoutine()
        {
            // [사운드 재생] UI가 활성화되는 첫 프레임에 효과음 1회 출력
            if (_audioSource != null && _discoverSound != null)
            {
                _audioSource.PlayOneShot(_discoverSound);
            }

            discoveryImage.gameObject.SetActive(true);

            // 1. 투명도 0 -> 255 (1.0f) 페이드 인 (전체 색상을 주황색 틴트로 고정하여 '번쩍' 효과)
            float timer = 0f;
            while (timer < _fadeInDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, timer / _fadeInDuration);
                discoveryImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, alpha);
                yield return null;
            }
            discoveryImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, 1f);

            // 2. 투명도 255 -> 230 (약 0.9f) 빠르게 가라앉음 + 색상 복구 (주황색 -> 원본 흰색 틴트)
            // 이 과정에서 배경 바가 원래의 어두운 질감으로 돌아옵니다.
            timer = 0f;
            float targetDipAlpha = 230f / 255f;
            while (timer < _dipDuration)
            {
                timer += Time.deltaTime;
                float t = timer / _dipDuration;

                float alpha = Mathf.Lerp(1f, targetDipAlpha, t);
                Color currentColor = Color.Lerp(_flashColor, Color.white, t); // 색상 원본 복구 보간

                discoveryImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
                yield return null;
            }
            discoveryImage.color = new Color(1f, 1f, 1f, targetDipAlpha);

            // 3. 1.5초 동안 원본 색상(투명도 230) 상태로 선명하게 유지하며 대기
            yield return new WaitForSeconds(_stayDuration);

            // 4. 230 -> 0 최종 페이드 아웃 (색상은 원본 유지)
            timer = 0f;
            while (timer < _fadeOutDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(targetDipAlpha, 0f, timer / _fadeOutDuration);
                discoveryImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            discoveryImage.color = new Color(1f, 1f, 1f, 0f);

            // 연출 완료 후 오브젝트 비활성화
            discoveryImage.gameObject.SetActive(false);
        }
    }
}