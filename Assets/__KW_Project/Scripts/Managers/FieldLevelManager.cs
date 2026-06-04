using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace KW
{
    public class FieldLevelManager : MonoBehaviour
    {
        public static FieldLevelManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private int _fieldLevel;        // 필드 레벨
        [SerializeField] private float _multiplier;        // 보정 수치

        [Header("참조")]
        // ⚠️ 주의: 이 리스트는 씬이 바뀔 때마다 새로 갱신(Clear 및 재등록)되어야 합니다!
        public List<GameObject> fieldMonters = new List<GameObject>(); // 필드 몬스터들

        public int FieldLevel
        {
            get => _fieldLevel;
            set
            {
                if (_fieldLevel == value) return;
                _fieldLevel = value;
            }
        }

        public float Multiplier
        {
            get => _multiplier;
            set
            {
                if (_multiplier == value) return;
                _multiplier = value;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 🛠️ 중요: 중괄호를 열고 Destroy 후 즉시 return; 하여 함수를 탈출합니다.
                Destroy(gameObject);
                return;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) OnFieldLevelChanged(_fieldLevel);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnFieldLevelChanged(int newFieldLevel)
        {
            if (newFieldLevel < 0 || _multiplier < 0f) return;

            Debug.Log($"FieldLevelManager.FieldLevel : {newFieldLevel}");

            foreach (GameObject fieldMonster in fieldMonters)
            {
                if (fieldMonster == null) continue;
                UpdateFieldMonsterStatus(fieldMonster);
            }
        }

        private void UpdateFieldMonsterStatus(GameObject fieldMonster)
        {
            if (fieldMonster.TryGetComponent(out MonsterHealth monsterHealth) &&
                fieldMonster.TryGetComponent(out SkeletonController skeletonController))
            {
                // 필드 레벨이 1 오를 수록 몬스터들의 체력과 공격력 값이 0.5배 씩 오릅니다.
                monsterHealth.UpdateHealth(_fieldLevel, _multiplier);
                skeletonController.UpdateDamage(_fieldLevel, _multiplier);
            }
        }
    }
}