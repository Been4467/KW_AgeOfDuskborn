using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class ItemDataBase : MonoBehaviour
    {
        public static ItemDataBase Instance { get; private set; }

        // ID로 아이템을 찾기 위한 사전
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
                Destroy(gameObject);
            } 

            // Resources/Item 폴더에 있는 모든 Item 을 불러옴
            Item[] allItems = Resources.LoadAll<Item>("Items");

            foreach (var item in allItems)
            {
                // Dictionary 에 해당 item 이 없으면
                if (!_itemDictionary.ContainsKey(item.itemId))
                {
                    // 해당 item 을 Dictionary 에 추가
                    _itemDictionary.Add(item.itemId, item);
                }
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // Item의 Id 를 불러오는 함수
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
