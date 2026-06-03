using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KW
{
    public class DuskbornSceneManager : MonoBehaviour
    {
        public static DuskbornSceneManager Instance { get; private set; }

        [HideInInspector] public int nextSpawnPointID = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // 씬이 로드될 때마다 실행될 함수 등록
                //SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 외부(포탈)에서 이 함수를 호출합니다.
        public void LoadScene(string sceneName, int spawnID)
        {
            nextSpawnPointID = spawnID;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            // 비동기 씬 로드 시작
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

            while (!op.isDone)
            {
                yield return null;
            }
            yield return null; // 씬 로드 직후 안정화 대기

            // 1. 새 씬에 있는 가짜 복제본 플레이어 제거
            CleanUpDuplicatePlayer();

            // 2. 가짜 플레이어가 완전히 Destroy될 수 있도록 한 프레임 더 대기 (매우 중요)
            yield return null;

            // 3. 이제 안전하게 원본 플레이어 이동 및 카메라 컴포넌트 재연결
            MovePlayerToSpawnPoint();
        }

        private void CleanUpDuplicatePlayer()
        {
            // 씬에 존재하는 모든 "Player" 태그 오브젝트를 찾습니다.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length > 1)
            {
                foreach (var p in players)
                {
                    // PlayerSceneConnector가 인스턴스 싱글톤 구조이므로, 
                    // 현재 static Instance로 지정된 원본이 '아닌' 오브젝트가 새로 생성된 복제본입니다.
                    if (p.GetComponent<PlayerSceneConnector>() != PlayerSceneConnector.Instance)
                    {
                        Debug.Log($"[DuskbornSceneManager] 새 씬에서 중복 생성된 플레이어({p.name})를 제거했습니다.");
                        Destroy(p);
                    }
                }
            }
        }


        private void MovePlayerToSpawnPoint()
        {
            // 현재 씬에 있는 모든 SpawnPoint 찾기
            PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
            Transform spawnTransform = null;

            // ID가 일치하는 스폰 포인트 찾기
            foreach (var point in spawnPoints)
            {
                if (point.spawnID == nextSpawnPointID)
                {
                    spawnTransform = point.transform;
                    break;
                }
            }

            if (spawnTransform != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                {
                    CharacterController cc = player.GetComponent<CharacterController>();

                    // CC가 켜져 있으면 transform.position 강제 변경이 씹힐 수 있으므로 잠시 끔
                    if (cc != null) cc.enabled = false;

                    player.transform.position = spawnTransform.position;
                    // 필요하다면 회전도 적용: player.transform.rotation = spawnTransform.rotation;

                    // Physics.SyncTransforms();

                    if (cc != null) cc.enabled = true;

                    PlayerSceneConnector sceneConnector = player.GetComponent<PlayerSceneConnector>();

                    sceneConnector.ConnectToSceneComponents();
                }
            }
            else
            {
                Debug.LogWarning($"ID {nextSpawnPointID}에 해당하는 스폰 포인트를 찾지 못했습니다.");
            }

        }


        public void GoToTitleScene()
        {
            Time.timeScale = 1f;

            // 플레이어 파괴
            if (SaveManager.Instance != null && SaveManager.Instance.player != null)
            {
                Destroy(SaveManager.Instance.player.gameObject);
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Destroy(playerObj);

            // 매니저들 파괴

        }

    }
}
