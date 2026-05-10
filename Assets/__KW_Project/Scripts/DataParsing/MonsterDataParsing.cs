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
        public static MonsterDataParsing Instance { get; set; }

        private Dictionary<string, MonsterData> monsterData = new Dictionary<string, MonsterData>();

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;

                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            LoadMonsterData();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void LoadMonsterData()
        {
            TextAsset monsterDataCsv = Resources.Load<TextAsset>("MonsterDataParsing");

            // 줄바꿈을 기준으로 데이터를 나눔 
            string[] lines = monsterDataCsv.text.Split('\n');

            // 첫번째줄은 header 라 건너뛰고부터 인식
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();                      // 혹시 모를 공백 제거
                if (string.IsNullOrEmpty(line)) continue;

                //  (",") 를 기준으로 셀의 데이터를 나눔
                string[] columns = line.Split(',');

                MonsterData data = new MonsterData
                {
                    // float.Parse를 이용해 문자열을 float으로 변환
                    patrolSpeed = float.Parse(columns[1]),
                    chaseSpeed = float.Parse(columns[2]),
                    maxHp = float.Parse(columns[3]),
                    detectRange = float.Parse(columns[4]),
                    coolDownDuration = float.Parse(columns[5]),
                    damage = float.Parse(columns[6])
                };

                string monsterID = columns[0];              
                monsterData.Add(monsterID, data);                  // monsterID, data 를 dictionary값으로 저장
            }
        }

        // 외부에서 몬스터데이터를 요청할 때 사용하는 변수
        public MonsterData GetMonsterData(string monsterId)
        {
            if (monsterData.ContainsKey(monsterId))
            {
                return monsterData[monsterId];
            }
            else
            {
                Debug.LogError("Monster ID not found in CSV: " + monsterId);
                return new MonsterData(); // 기본값 반환
            }
        }
    }
}
