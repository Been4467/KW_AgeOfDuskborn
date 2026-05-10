using System.Collections.Generic;
using System.Data.Common;
using Cinemachine.Utility;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace KW
{
    public enum VfxType
    {
        Heal,
        Hit,
        Slash,
        StepDust
    }
    public class VfxManager : MonoBehaviour
    {
        public static VfxManager Instance { get; private set; }

        [System.Serializable]
        public class VfxData
        {
            public VfxType type;
            public GameObject prefab;
            public int poolSize = 10;
        }

        [Header("동록할 이펙트 목록")]
        [SerializeField] private List<VfxData> vfxList;

        // 실제 풀 (Key: 타입, Value: 큐)
        private Dictionary<VfxType, Queue<GameObject>> pools = new Dictionary<VfxType, Queue<GameObject>>();

        // 부모 딕셔너리
        private Dictionary<VfxType, Transform> parents = new Dictionary<VfxType, Transform>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void InitializePools()
        {
            foreach (var data in vfxList)
            {
                // 종류별 폴더(부모) 생성
                GameObject parentObj = new GameObject(data.type.ToString() + "_Pool");

                parentObj.transform.SetParent(this.transform);
                parents.Add(data.type, parentObj.transform);

                // 큐 생성
                Queue<GameObject> queue = new Queue<GameObject>();

                // 미리 생성해서 채워넣기
                for (int i = 0; i < data.poolSize; i++)
                {
                    GameObject obj = Instantiate(data.prefab, parentObj.transform);
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }

                pools.Add(data.type, queue);
            }
        }

        public void PlayVfx(VfxType type, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(type))
            {
                Debug.LogWarning($"[VfxManager] {type} 이펙트가 등록되지 않음");

                return;
            }

            Queue<GameObject> queue = pools[type];
            GameObject obj;

            // 풀에 남은 게 없으면 새로 만들어서 씀 (유연하게 확장)
            if (queue.Count == 0 || queue.Peek().activeSelf)
            {
                VfxData data = vfxList.Find(x => x.type == type);
                obj = Instantiate(data.prefab, parents[type]);
            }
            else
            {
                obj = queue.Dequeue();
            }

            // 위치 설정 및 활성화
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // 다시 큐에 넣어서 재사용 준비
            queue.Enqueue(obj);
        }

    }
}
