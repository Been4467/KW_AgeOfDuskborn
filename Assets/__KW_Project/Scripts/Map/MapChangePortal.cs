using UnityEngine;

namespace KW
{
    public class MapChangePortal : MonoBehaviour
    {
        [Header("이동할 씬 이름")]
        [SerializeField] private string sceneToLoad;

        [SerializeField] private int spawnPointID;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (DuskbornSceneManager.Instance != null)
                {
                    DuskbornSceneManager.Instance.LoadScene(sceneToLoad, spawnPointID);
                }
                else
                {
                    Debug.LogError("[MapChangePortal] DuskbornSceneManager 인스턴스를 찾을 수 없습니다! 씬에 매니저 오브젝트가 있는지 확인하세요.");
                }
            }
        }
    }
}