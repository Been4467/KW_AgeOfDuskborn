using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public struct MonsterData
    {
        public float patrolSpeed;
        public float chaseSpeed;
        public float maxHp;
        public float detectRange;
        public float coolDownDuration;
        public float damage;
    }

    public class MonsterDataParsing : MonoBehaviour
    {
        // 🛠️ 다른 코드와의 일관성을 위해 캡슐화를 private set으로 고치는 것이 안전합니다.
        public static MonsterDataParsing Instance { get; private set; }

        private Dictionary<string, MonsterData> monsterData = new Dictionary<string, MonsterData>();

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

            // 진짜 인스턴스만 데이터를 로드합니다.
            LoadMonsterData();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void LoadMonsterData()
        {
            TextAsset monsterDataCsv = Resources.Load<TextAsset>("MonsterDataParsing");
            if (monsterDataCsv == null) return;

            string[] lines = monsterDataCsv.text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] columns = line.Split(',');

                // 행 파싱 도중 생길 수 있는 예외 방지 (데이터 부족 등)
                if (columns.Length < 7) continue;

                MonsterData data = new MonsterData
                {
                    patrolSpeed = float.Parse(columns[1]),
                    chaseSpeed = float.Parse(columns[2]),
                    maxHp = float.Parse(columns[3]),
                    detectRange = float.Parse(columns[4]),
                    coolDownDuration = float.Parse(columns[5]),
                    damage = float.Parse(columns[6])
                };

                string monsterID = columns[0];

                // 🛠️ 안전장치: 혹시 모를 중복 ID 파싱 에러 방지
                if (!monsterData.ContainsKey(monsterID))
                {
                    monsterData.Add(monsterID, data);
                }
            }
        }

        public MonsterData GetMonsterData(string monsterId)
        {
            if (monsterData.TryGetValue(monsterId, out MonsterData data))
            {
                return data;
            }
            else
            {
                Debug.LogError("Monster ID not found in CSV: " + monsterId);
                return new MonsterData();
            }
        }

    }
}
