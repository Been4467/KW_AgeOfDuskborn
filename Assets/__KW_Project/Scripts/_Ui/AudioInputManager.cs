using UnityEngine;

namespace KW
{
    public class AudioInputManager : MonoBehaviour
    {
        [Header("오디오 소스 연결")]
        [Tooltip("소리를 출력할 오디오 소스를 넣어주세요.")]
        [SerializeField] private AudioSource _audioSource;

        [Header("키별 오디오 클립 설정")]
        [Tooltip("I 키를 누를 때 재생할 사운드")]
        [SerializeField] private AudioClip _soundI;

        [Tooltip("M 키를 누를 때 재생할 사운드")]
        [SerializeField] private AudioClip _soundM;

        [Tooltip("Q 키를 누를 때 재생할 사운드")]
        [SerializeField] private AudioClip _soundQ;

        [Tooltip("Esc 키를 누를 때 재생할 사운드")]
        [SerializeField] private AudioClip _soundEscape;

        private void Start()
        {
            // 만약 인스펙터에서 AudioSource를 연결하지 않았다면 자신에게서 찾기
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            // AudioSource 세팅 확인 방어 코드
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
            }
        }

        private void Update()
        {
            // I 키 입력 체크
            if (Input.GetKeyDown(KeyCode.I))
            {
                PlaySound(_soundI, "I");
            }

            // M 키 입력 체크
            if (Input.GetKeyDown(KeyCode.M))
            {
                PlaySound(_soundM, "M");
            }

            // Q 키 입력 체크
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PlaySound(_soundQ, "Q");
            }

            // Esc 키 입력 체크
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PlaySound(_soundEscape, "Escape");
            }
        }

        /// <summary>
        /// 지정된 오디오 클립을 중첩 재생이 가능한 PlayOneShot으로 출력합니다.
        /// </summary>
        private void PlaySound(AudioClip clip, string keyName)
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("[AudioInputManager] AudioSource가 할당되지 않았습니다.");
                return;
            }

            if (clip != null)
            {
                // PlayOneShot을 사용하면 연타를 해도 소리가 끊기지 않고 자연스럽게 겹쳐서 납니다.
                _audioSource.PlayOneShot(clip);
                Debug.Log($"[AudioInputManager] {keyName} 키 입력 -> 사운드 재생 완료");
            }
            else
            {
                Debug.LogWarning($"[AudioInputManager] {keyName} 키에 연결된 오디오 클립이 없습니다.");
            }
        }
    }
}