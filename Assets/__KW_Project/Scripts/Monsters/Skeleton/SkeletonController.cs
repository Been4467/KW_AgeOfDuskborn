using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KW
{
    public class SkeletonController : MonoBehaviour
    {
        private MonsterBaseState<SkeletonController> currentState;
        private Coroutine slowDebuffCoroutine;

        #region 스켈레톤 fsm

        public readonly SkeletonIdleState idleState = new SkeletonIdleState();
        public readonly SkeletonPatrolState patrolState = new SkeletonPatrolState();
        public readonly SkeletonAttackState attackState = new SkeletonAttackState();
        public readonly SkeletonChaseState chaseState = new SkeletonChaseState();
        public readonly SkeletonCoolDownState coolDownState = new SkeletonCoolDownState();
        public readonly SkeletonDamagedState damagedState = new SkeletonDamagedState();
        public readonly SkeletonDeathState deathState = new SkeletonDeathState();

        #endregion

        #region 컴포넌트
        [TextArea(10, 2)]
        public string DataParsingFrom;

        [Header("정보")]
        public string monsterId = "Skeleton";

        [Header("이동")]
        private float _basePatrolSpeed;
        private float _baseChaseSpeed;
        public float patrolSpeed = 1;
        public float chaseSpeed = 3;
        private float slowModifier = 1f;
        public float CurrentAgentSpeed
        {
            get
            {
                if (currentState == chaseState) return chaseSpeed * slowModifier;
                if (currentState == patrolState) return patrolSpeed * slowModifier;
                return patrolSpeed * slowModifier;
            }
        }

        [Header("추격,공격 범위")]
        [HideInInspector] public float detectRange = 5f;
        [HideInInspector] public float attackRange = 0.8f;
        [HideInInspector] public float idleWaitTime = 3f;
        [HideInInspector] public float minSuspiciousTime = 2f;                        // 두리번거리는 시간
        [HideInInspector] public float maxSuspiciousTime = 6f;
        [HideInInspector] public float suspiciousTime = 3f;
        [HideInInspector] public float timeSinceLastSawPlayer;

        public float idleWaittimeSerialized = 0f;

        public LayerMask playerLayer;
        public Transform wayPoints;                                 // 목적지
        public int wayPointIndex;                             // 목적지 인덱스 ( 증가시킬거임 ) 

        public Transform target;

        [Header("공격")]
        protected float _baseDamage;            //  데이터 원본 값 (수정안함)
        public float damage = 25;
        public float lungeForce = 2f;                              // 공격 후 반동
        public float lungeDuration = 0.2f;
        public bool isDoingLunge = false;
        public float coolDownDuration = 1f;
        [HideInInspector] public Vector3 lastAttackDirection;
        public Vector2 lastDirection;                               // coolDownState 에서 방향을 기억하기 위한 변수
        public float stunTimerSerialized;
        public GameObject hitBox;
        public GameObject indicator;

        [Header("피격")]
        [HideInInspector] public Vector3 lastDamagedDirection;

        [Header("컴포넌트")]
        [HideInInspector] public Animator anim;
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public SpriteRenderer sr;
        [HideInInspector] public MonsterHealth health;
        public GameObject healthCanvas;

        #endregion

        private void Awake()
        {
            anim = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            sr = GetComponentInChildren<SpriteRenderer>();

            health = GetComponent<MonsterHealth>();

            if (health == null)
            {
                Debug.LogError("Skeleton 의 SkeletonHealth 참조가 없음");
            }

            // 스클레톤마다 랜덤한 시간 부여
            suspiciousTime = Random.Range(minSuspiciousTime, maxSuspiciousTime);
        }

        private void Start()
        {
            LoadStatsParsing();

            if (health != null)
            {
                health.OnHit += HandleHit;
                health.OnDeath += HandleDeath;
            }

            _basePatrolSpeed = patrolSpeed;
            _baseChaseSpeed = chaseSpeed;

            _baseDamage = damage;
            agent.updateRotation = false;
            rb.isKinematic = true;
            SwitchState(idleState);
        }

        private void LoadStatsParsing()
        {
            if (MonsterDataParsing.Instance != null)
            {
                // DataManager에서 이 몬스터의 ID에 맞는 데이터를 가져옴
                MonsterData data = MonsterDataParsing.Instance.GetMonsterData(monsterId);

                // 가져온 데이터로 변수 값 초기화
                _basePatrolSpeed = data.patrolSpeed;
                _baseChaseSpeed = data.chaseSpeed;

                patrolSpeed = data.patrolSpeed;
                chaseSpeed = data.chaseSpeed;

                detectRange = data.detectRange;
                coolDownDuration = data.coolDownDuration;
                damage = data.damage;

                if (health != null)
                {
                    health.InitializeHealth(data.maxHp);
                }
            }
        }

        public virtual void UpdateDamage(int level, float multiplier)
        {
            damage = _baseDamage + (_baseDamage * level * multiplier);

            Debug.Log($"SkeletonController.damage : {damage}");
        }

        private void Update()
        {
            currentState.UpdateState(this);

            stunTimerSerialized = damagedState.stunTimer;
            idleWaittimeSerialized = idleState.idleTimer;

            if (agent != null && agent.enabled && !isDoingLunge)
            {
                agent.speed = CurrentAgentSpeed;
            }
        }

        public void SwitchState(MonsterBaseState<SkeletonController> monsterState)
        {
            currentState?.ExitState(this);

            currentState = monsterState;
            currentState.EnterState(this);
        }

        public void PerformAttackLunge()
        {
            ResetSlowDebuff();

            StopAllCoroutines();
            StartCoroutine(LungeRoutine());
        }

        private IEnumerator LungeRoutine()
        {
            isDoingLunge = true;
            agent.enabled = false;
            rb.isKinematic = false;

            rb.AddForce(lastAttackDirection * lungeForce, ForceMode.Impulse);

            yield return new WaitForSeconds(lungeDuration);

            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
            agent.enabled = true;

            // 돌진 직후 현재 상태에 맞는 속도로 재연산
            if (agent.enabled) agent.speed = CurrentAgentSpeed;

            isDoingLunge = false;
        }


        public void StartCoolDown()
        {
            ResetSlowDebuff();

            SwitchState(coolDownState);

            StopAllCoroutines();
            StartCoroutine(CoolDownRoutine());
        }

        private IEnumerator CoolDownRoutine()
        {
            // Debug.Log("스켈레톤 : 쿨다운 시작");

            anim.SetBool("isCoolDown", true);

            yield return new WaitForSeconds(coolDownDuration);

            // Debug.Log("스켈레톤 : 쿨다운 종료!");

            anim.SetBool("isCoolDown", false);

            SwitchState(chaseState);
        }

        public void HandleHit(Vector3 hitDirection)
        {
            lastDamagedDirection = hitDirection;

            SwitchState(damagedState);
        }

        public void HandleDeath()
        {
            hitBox.SetActive(false);

            ResetSlowDebuff();

            // 몬스터 사망 시, 사망 지역을 전달
            string currentScene = SceneManager.GetActiveScene().name;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnMonsterKilled(this.monsterId, currentScene);
            }
            else
            {
                Debug.LogError("[SkeletonController] QuestManager.cs를 찾을 수 없음");
            }

            // 사망 사실 기록 SaveManager에
            SkeletonPersistence skeletonPersistence = GetComponent<SkeletonPersistence>();
            if (skeletonPersistence != null)
            {
                skeletonPersistence.RecordDeath();
            }
            else
            {
                Debug.LogWarning($"[SkeletonController] {gameObject.name}에 SkeletonPersistence 컴포넌트를 찾을 수 없음");
            }

            Debug.Log($"[SkeletonController] 스켈레톹 사망 : {this.monsterId} / {currentScene}");

            SwitchState(deathState);
        }

        public void ApplySlowDebuff(float duration, float slowPercent)
        {
            if (slowDebuffCoroutine != null)
            {
                StopCoroutine(slowDebuffCoroutine);
            }
            slowDebuffCoroutine = StartCoroutine(SlowDebuffRoutine(duration, slowPercent));
        }

        private IEnumerator SlowDebuffRoutine(float duration, float slowPercent)
        {
            slowModifier = 1f - (slowPercent / 100f);

            if (agent != null && agent.enabled) agent.speed = CurrentAgentSpeed;
            if (anim != null) anim.speed = slowModifier;

            yield return new WaitForSeconds(duration);

            ResetSlowDebuff();
        }

        public void ResetSlowDebuff()
        {
            slowModifier = 1f;

            if (agent != null && agent.enabled) agent.speed = CurrentAgentSpeed;
            if (anim != null) anim.speed = 1f;

            slowDebuffCoroutine = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
