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

        private Dictionary<VfxType, Queue<GameObject>> pools = new Dictionary<VfxType, Queue<GameObject>>();
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
                // 🛠️ 중요: 파괴 예약 후 즉시 return하여 InitializePools()가 중복 호출되는 것을 막습니다.
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            // 🛠️ 보완: 파괴되는 내가 '진짜 인스턴스'일 때만 스태틱 참조를 비웁니다.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitializePools()
        {
            foreach (var data in vfxList)
            {
                GameObject parentObj = new GameObject(data.type.ToString() + "_Pool");
                parentObj.transform.SetParent(this.transform);
                parents.Add(data.type, parentObj.transform);

                Queue<GameObject> queue = new Queue<GameObject>();

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

            if (queue.Count == 0 || queue.Peek().activeSelf)
            {
                VfxData data = vfxList.Find(x => x.type == type);
                obj = Instantiate(data.prefab, parents[type]);
            }
            else
            {
                obj = queue.Dequeue();
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            queue.Enqueue(obj);
        }
    }
}