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

            // 로딩이 끝날 때까지 대기
            while (!op.isDone)
            {
                yield return null;
            }

            yield return null;
            // yield return new WaitForSeconds(1f); 

            // 이제 안전하게 플레이어 이동
            MovePlayerToSpawnPoint();
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
