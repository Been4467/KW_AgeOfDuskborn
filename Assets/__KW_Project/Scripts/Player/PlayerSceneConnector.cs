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
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 게임 처음 시작할 때도 연결
            ConnectToSceneComponents();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        /*   private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
          {
              if (scene.name == "LoadingScene") return;

              // ConnectToSceneComponents();
          } */

        public void ConnectToSceneComponents()
        {
            // 1. 현재 씬에 있는 가상 카메라를 새로 탐색
            CinemachineVirtualCamera vCam = FindObjectOfType<CinemachineVirtualCamera>();

            if (vCam != null)
            {
                // 원본 플레이어(this)를 타겟으로 지정
                vCam.Follow = this.transform;
                vCam.LookAt = this.transform;

                // [중요] 카메라 팅김 방지 워프 처리를 한 프레임 쉬고 하거나, 
                // 포지션이 결정된 직후에 확실히 먹여야 합니다.
                vCam.OnTargetObjectWarped(transform, transform.position - vCam.transform.position);

                Debug.Log("[PlayerSceneConnector] Cinemachine VC 타겟 연결 성공!");
            }
            else
            {
                Debug.LogError("[PlayerSceneConnector] 이 씬에는 CinemachineVirtualCamera가 없습니다.");
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

            PlayerMovement player = this.transform.GetComponent<PlayerMovement>();

            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                player.playerHealth = playerHealth;
                Debug.LogError("[PlayerSceneConnection] playerHealth를 찾지 못함");
            }
            else
            {
                Debug.Log("[PlayerSceneConnector] playerHealth 를 찾음");
            }
        }
    }
}