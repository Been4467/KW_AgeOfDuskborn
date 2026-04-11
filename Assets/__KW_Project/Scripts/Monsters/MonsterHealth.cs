using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KW
{
    public class MonsterHealth : MonoBehaviour
    {
        [TextArea(2, 3)]
        [SerializeField] private string scriptPurpose;

        protected float _baseHp;            // 파싱된 데이터 원본 값 (수정안함)
        protected float _currentHp;
        protected float _maxHp;             // 보정에 의해 수정될 값  

        // 읽기 전용
        public float currentHp => _currentHp;
        public float maxHp => _maxHp;

        public event Action<Vector3> OnHit;                         // 피격 시 플레이어 위치를 전달받기 위함
        public event Action OnDeath;

        public event Action<float, float> OnHealthChanged;

        void Start()
        {
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        public virtual void InitializeHealth(float parsedMaxHp)
        {
            _baseHp = parsedMaxHp;
            _maxHp = _baseHp;
            _currentHp = _maxHp;

            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        public virtual void UpdateHealth(int level, float multiplier)
        {
            _maxHp = _baseHp + (_baseHp * level * multiplier);
            _currentHp = _maxHp;

            OnHealthChanged?.Invoke(_currentHp, _maxHp);

            Debug.Log($"MonsterHealth._maxHp : {_maxHp}");
        }

        public virtual void TakeDamage(float damage, Transform attacker)
        {
            if (_currentHp <= 0) return;

            // 데미지 만큼 체력 감소
            _currentHp -= damage;
            
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);

            OnHealthChanged?.Invoke(_currentHp, _maxHp);

            Debug.Log($"몬스터 체력 감소 : {damage} 만큼 감소됨");
            Debug.Log($"몬스터 남은 체력 : {_currentHp} 만큼 남음");

            if (_currentHp <= 0)
            {
                OnDeath?.Invoke();
            }
            else
            {
                Vector3 onHitDirection = (transform.position - attacker.position).normalized;
                
                // 방향을 실어서 방송
                OnHit?.Invoke(onHitDirection);
            }
        }
    }
}
