using UnityEngine;
using System;

namespace KW
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("체력 설정")]
        [SerializeField] private float _maxHp;
        [SerializeField] private float _currentHp;

        [Header("공격 관련")]
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private float _weaponDamage = 0f;

        private float _originalBaseDamage = 10f;

        [Header("방어 관련")]
        [SerializeField] private float _defencePercentage = 0f;
                
        public bool hasWeapon => _weaponDamage > 0f;                    // 현재 무기가 장착되어 있는 지 확인
        public float TotalDamage { get { return _baseDamage + _weaponDamage; } }

        public virtual float currentHp => _currentHp;
        public virtual float maxHp => _maxHp;

        private PlayerMovement player;

        public event Action<float, float> OnHealthChanged;

        // 나중에 PlayerMovement가 사용
        public event Action OnPlayerDied;                               // 사망 이벤트
        public event Action<Vector3> OnPlayerHit;                       // 피격 이벤트

        private void Awake()
        {
            if (player != null)
            {
                player.playerHealth = this;
                Debug.Log("[PlayerHealth] 플레이어에 등록 완료!");
            }
        }
        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.playerHealth = this;
            }
           

            _currentHp = _maxHp;

            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        private void OnDestroy()
        {
            OnHealthChanged = null;
        }

        // 방어구 장착 시 호출될 함수
        public void SetEquippedArmour(Armour armourData)
        {
            if (armourData != null)
            {
                _defencePercentage = armourData.defenceRate;
                Debug.Log($"[PlayerHealth] 방어구 장착 : 방어율은 {_defencePercentage * 100}%");
            }
            else
            {
                _defencePercentage = 0f;
            }
        }

        public void SetEquippedWeapon(Weapon weaponData)
        {
            if (weaponData != null)
            {
                // 무기가 장착되어 있을 경우
                _weaponDamage = weaponData.damage;
            }
            else
            {
                // 무기없음. 맨손
                _weaponDamage = 0f;
            }
        }

        public void InitializeHealth(float parsedMaxHp, float parsedBaseDamage)
        {
            _maxHp = parsedMaxHp;
            _currentHp = _maxHp;

            _originalBaseDamage = parsedBaseDamage;
            _baseDamage = parsedBaseDamage;

            OnHealthChanged?.Invoke(_currentHp, parsedMaxHp);
        }

        public void UpdateDamageModifier(float modifier)
        {
            _baseDamage = _originalBaseDamage * modifier;

            Debug.Log($"[PlayerHealth] 피로도 반영 완료. 원본 기본뎀: {_originalBaseDamage} -> 조정된 기본뎀: {_baseDamage} (최종 공격력 TotalDamage: {TotalDamage})");
        }

        // 몬스터가 타격 시 호출
        public void TakeDamage(float damage, Transform attacker)
        {
            if (_currentHp <= 0) return;

            float reducedDamage = damage - (damage * _defencePercentage);

            // 체력 깎기
            _currentHp -= reducedDamage;
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);

            Debug.Log($"피격! 원본: {damage} -> 최종: {reducedDamage} (방어율: {_defencePercentage})");

            // UI에 변경된 체력 '방송' (가장 중요!)
            OnHealthChanged?.Invoke(_currentHp, _maxHp);

            // 체력이 0 이하가 되면 '사망' 이벤트 방송
            if (_currentHp <= 0)
            {
                OnPlayerDied?.Invoke();
            }
            else
            {
                Vector3 hitDirection = (transform.position - attacker.position).normalized;
                hitDirection.y = 0;

                OnPlayerHit?.Invoke(hitDirection);
            }
        }

        public void SetHealth(float health)
        {
            // 불러온 체력 적용
            _currentHp = Mathf.Clamp(health, 0, _maxHp);

            OnHealthChanged?.Invoke(_currentHp, _maxHp);


            // 체력이 0 이하일 시 
            if (_currentHp <= 0)
            {
                OnPlayerDied?.Invoke();
            }
        }
                
        public void Heal(float amount)
        {
            // 체력 회복 (maxHp를 넘지 않게)
            _currentHp = Mathf.Clamp(_currentHp + amount, 0, _maxHp);

            // UI에 체력 '방송' (OnHealthChanged?.Invoke)
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }
    }
}