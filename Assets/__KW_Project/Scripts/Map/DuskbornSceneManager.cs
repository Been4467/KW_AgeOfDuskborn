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
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadScene(string sceneName, int spawnID)
        {
            nextSpawnPointID = spawnID;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            // ✅ 추가: 씬 로드 전 페이드 아웃
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeOut());

            // 1. 씬 변경 직전 현재 데이터를 세이브 파일에 백업
            if (SaveManager.Instance != null)
            {
                Debug.Log($"[DuskbornSceneManager] 씬 변경 직전 현재 데이터를 저장합니다.");
                SaveManager.Instance.SaveGame();
            }

            // 2. 새 씬 비동기 로드
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }
            yield return null;

            // 3. 중복 플레이어 정리
            CleanUpDuplicatePlayer();
            yield return null;

            // 4. 문(Portal)에 설정된 올바른 스폰 포인트 위치로 플레이어 우선 배치
            MovePlayerToSpawnPoint();
            yield return null;

            // ===================================================================
            // ✅ [여기에 추가] 새 씬의 오브젝트 로드가 끝났으므로 화톳불 상태를 먼저 복구합니다.
            if (SaveManager.Instance != null)
            {
                Debug.Log($"[DuskbornSceneManager] 새 씬의 화톳불 상태를 세이브 데이터 기준으로 갱신합니다.");
                SaveManager.Instance.RefreshBonfiresInScene();
            }
            // ===================================================================

            // 5. 💡 [핵심 수정] SaveManager에 데이터 복구를 요청할 때 매개변수를 직접 지정합니다.
            if (SaveManager.Instance != null)
            {
                Debug.Log($"[DuskbornSceneManager] 새 씬 배치 완료. 인벤토리 및 상태 데이터를 안전하게 동기화합니다.");

                // 💡 부활은 false, 문을 통한 구역 이동이므로 'isPortalMoving'에 true를 보냅니다!
                SaveManager.Instance.LoadGame(isRespawn: false, isPortalMoving: true);
            }

            // ✅ 추가: 모든 배치 완료 후 페이드 인
            if (FadeManager.Instance != null)
                yield return StartCoroutine(FadeManager.Instance.FadeIn());
        }

        private void CleanUpDuplicatePlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length > 1)
            {
                foreach (var p in players)
                {
                    if (p.GetComponent<PlayerSceneConnector>() != PlayerSceneConnector.Instance)
                    {
                        Debug.Log($"[DuskbornSceneManager] 새 씬에서 중복 생성된 플레이어({p.name})를 제거했습니다.");
                        Destroy(p.gameObject);
                    }
                }
            }
        }

        public void MovePlayerToSpawnPoint()
        {
            PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
            Transform spawnTransform = null;

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
                    if (cc != null) cc.enabled = false;

                    player.transform.position = spawnTransform.position;

                    if (cc != null) cc.enabled = true;

                    PlayerSceneConnector sceneConnector = player.GetComponent<PlayerSceneConnector>();
                    if (sceneConnector != null)
                    {
                        sceneConnector.ConnectToSceneComponents();
                    }
                }

                // 💡 [추가] 플레이어 배치가 성공적으로 끝났으므로, 여기서 ID를 -1로 안전하게 리셋합니다.
                nextSpawnPointID = -1;
            }
            else
            {
                // 💡 nextSpawnPointID가 -1일 때는 정상적인 리셋 상태이므로 굳이 에러 경고를 띄우지 않도록 예외 처리합니다.
                if (nextSpawnPointID != -1)
                {
                    Debug.LogWarning($"ID {nextSpawnPointID}에 해당하는 스폰 포인트를 찾지 못했습니다.");
                }
            }
        }

        public void GoToTitleScene(string titleSceneName = "TitleScene")
        {
            Time.timeScale = 1f;

            if (SaveManager.Instance != null && SaveManager.Instance.player != null)
            {
                Destroy(SaveManager.Instance.player.gameObject);
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Destroy(playerObj);

            if (UiManager.Instance != null) Destroy(UiManager.Instance.gameObject);
            if (DialogueManager.Instance != null) Destroy(DialogueManager.Instance.gameObject);
            if (VfxManager.Instance != null) Destroy(VfxManager.Instance.gameObject);
            if (NotificationManager.Instance != null) Destroy(NotificationManager.Instance.gameObject);
            if (SaveManager.Instance != null) Destroy(SaveManager.Instance.gameObject);

            SceneManager.LoadScene(titleSceneName);
        }
    }
}