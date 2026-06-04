using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    public class PlayerSceneConnector : MonoBehaviour
    {
        public static PlayerSceneConnector Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 🛡️ 가짜라면 즉시 게임 오브젝트를 비활성화해서 
                // 하위나 동료 스크립트의 Start()가 단 1프레임도 실행되지 못하게 만듭니다.
                gameObject.SetActive(false);

                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 게임 처음 시작할 때도 연결
            ConnectToSceneComponents();
        }

        private void OnDestroy()
        {
            // 🛠️ 핵심 수정: 오직 진짜 인스턴스가 파괴될 때만 static 변수를 비웁니다.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ConnectToSceneComponents()
        {
            // 1. 현재 씬에 있는 가상 카메라를 새로 탐색
            CinemachineVirtualCamera vCam = FindObjectOfType<CinemachineVirtualCamera>();

            if (vCam != null)
            {
                // 원본 플레이어(this)를 타겟으로 지정
                vCam.Follow = this.transform;
                vCam.LookAt = this.transform;

                // 🛠️ 시네머신 튕김/급회전 방지 핵심 로직 보안:
                // 가상 카메라에게 "이전 프레임 정보는 가짜니까 추적 연산하지 마라"고 명령합니다.
                vCam.PreviousStateIsValid = false;

                // 워프 연산 시 씬 로드 시점에는 순간 이동 거리가 0에 수렴하므로 Vector3.zero가 안전합니다.
                vCam.OnTargetObjectWarped(transform, Vector3.zero);

                Debug.Log("[PlayerSceneConnector] Cinemachine VC 타겟 연결 및 워프 안정화 성공!");
            }
            else
            {
                Debug.LogWarning("[PlayerSceneConnector] 이 씬에는 CinemachineVirtualCamera가 없습니다. (타이틀 혹은 로딩 씬)");
            }

            // 2. 메인 카메라 디더링 연결
            if (Camera.main != null)
            {
                DitherTransparency dither = Camera.main.GetComponent<DitherTransparency>();
                if (dither != null)
                {
                    dither.player = this.transform;
                    Debug.Log("[PlayerSceneConnector] Dither 연결 성공!");
                }
            }

            // 3. 컴포넌트 내부 캐싱 및 예외 처리 보완
            PlayerMovement player = this.transform.GetComponent<PlayerMovement>();
            if (player != null)
            {
                PlayerHealth playerHealth = GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    // 꼼꼼한 방어 코드: 내 겉에 없다면 player 내부에서 찾아옵니다.
                    playerHealth = player.GetComponent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    player.playerHealth = playerHealth;
                    // Debug.Log("[PlayerSceneConnector] Player과 PlayerHealth 상호 연결 완료");
                }
                else
                {
                    Debug.LogError("[PlayerSceneConnector] PlayerHealth 컴포넌트를 플레이어에게서 찾을 수 없습니다!");
                }
            }
        }
    }
}