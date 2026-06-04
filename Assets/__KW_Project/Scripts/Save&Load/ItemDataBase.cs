using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class ItemDataBase : MonoBehaviour
    {
        public static ItemDataBase Instance { get; private set; }

        private Dictionary<string, Item> _itemDictionary = new Dictionary<string, Item>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 🛠️ 중요: 새로 생성된 복제본이면 파괴 예약 후 즉시 함수를 빠져나갑니다.
                Destroy(gameObject);
                return;
            }

            // 진짜 인스턴스만 이 아래 초기화 로직을 수행합니다.
            Item[] allItems = Resources.LoadAll<Item>("Items");

            foreach (var item in allItems)
            {
                if (!_itemDictionary.ContainsKey(item.itemId))
                {
                    _itemDictionary.Add(item.itemId, item);
                }
            }
        }

        private void OnDestroy()
        {
            // 🛠️ 중요: 파괴되는 내가 '진짜' 인스턴스일 때만 null로 만듭니다.
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Item GetItemId(string id)
        {
            if (_itemDictionary.TryGetValue(id, out Item item))
            {
                return item;
            }
            return null;
        }

    }
}
