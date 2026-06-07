using System.Collections;
using UnityEngine;

namespace KW
{
    public class Bonfire : MonoBehaviour, IInteractable
    {
        [Header("정보")]
        public string BonfireID;

        [Header("연결된 오브젝트")]
        [Tooltip("화톳불의 불꽃 역할을 하는 자식 Quad 오브젝트를 넣어주세요.")]
        public GameObject fireQuad;

        [Header("다크소울 스타일 UI 연출 설정")]
        [Tooltip("처음 번쩍할 때 이미지 전체에 입힐 글자 색상(주황색/금색)을 지정하세요.")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.55f, 0.12f, 1f);

        [Header("사운드 추가 설정")]
        [Tooltip("소리를 출력할 오디오 소스를 연결하세요. (비어있으면 자동으로 이 오브젝트에서 찾습니다)")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("화톳불이 활성화될 때 재생할 웅장한 효과음(WAV/MP3)을 넣어주세요.")]
        [SerializeField] private AudioClip _discoverSound;

        [Header("연출 세부 시간(초)")]
        [SerializeField] private float _fadeInDuration = 0.15f;
        [SerializeField] private float _dipDuration = 0.12f;
        [SerializeField] private float _stayDuration = 1.5f;
        [SerializeField] private float _fadeOutDuration = 0.6f;

        private bool _isActivated = false;
        public bool isActivated
        {
            get { return _isActivated; }
            set
            {
                if (_isActivated != value)
                {
                    Debug.Log($"[감시] isActivated 값이 변경됨: {_isActivated} -> {value}\n{System.Environment.StackTrace}");
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
                _audioSource = GetComponent<AudioSource>();
        }

        private void UpdateFireState(bool isActive)
        {
            if (fireQuad != null)
                fireQuad.SetActive(isActive);
            else
                Debug.LogWarning($"[{BonfireID}] Bonfire 스크립트에 Fire Quad가 할당되지 않았습니다!");
        }

        public void Interact(GameObject player)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement == null || movement.isSitting) return;

            if (_isActivated == false)
            {
                // 1. 상태 변경
                isActivated = true;

                // ✅ 2. SaveManager의 리스트에 이 화톳불 ID 등록 및 자동 저장
                if (SaveManager.Instance != null)
                {
                    if (!SaveManager.Instance.activatedBonfireIDs.Contains(BonfireID))
                    {
                        SaveManager.Instance.activatedBonfireIDs.Add(BonfireID);
                    }

                    // 최초 활성화 시점에 게임을 자동 저장하여 씬을 나갔다 와도 유지되도록 함
                    SaveManager.Instance.SaveGame();
                }

                // 3. UI 연출 완전히 위임
                if (UiManager.Instance != null)
                {
                    UiManager.Instance.ShowBonfireDiscovery(
                        _audioSource, _discoverSound, _flashColor,
                        _fadeInDuration, _dipDuration, _stayDuration, _fadeOutDuration
                    );
                }
                else
                {
                    Debug.LogWarning($"[{BonfireID}] UiManager를 찾을 수 없어 연출을 재생할 수 없습니다.");
                }
            }
            else
            {
                // ✅ 이미 활성화된 상태라면 휴식 모드 진입 및 체력 회복 (이후 이벤트 연출)
                EnterRestMode(player, movement);
            }
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
    }
}