using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;

namespace KW
{
    public class PlayerMovement : MonoBehaviour
    {
        #region 변수
        [Header("데이터파싱")]
        [TextArea(10, 1)]
        public string DataParsingFrom;

        [Header("세이브")]
        public int saveCount = 0;                         // 세이브 가능한 횟수               
        public int limitedSaveCount = 5;                  // 세이브 유효 횟수               

        [Header("움직임")]
        public float walkSpeed = 3f;                         // 걷는 속도                    
        public float runSpeed = 5f;                          // 달리는 속도
        public float airSpeed = 4f;                          // 공중에서의 속도 (필요없음)
        public float xInput, zInput;
        public float lastMoveX, lastMoveZ;
        public bool isAttacking = false;

        private float baseWalkSpeed = 3f;
        private float baseRunSpeed = 5f;

        private bool _isSitting = false;
        public bool isSitting
        {
            get { return _isSitting; }
            set
            {
                // 값이 바뀔 때만 로그 찍기
                if (_isSitting != value)
                {
                    // 누가 바꿨는지 추적하기 위해 스택 트레이스 출력
                    Debug.Log($"[감시] isSitting 값이 변경됨: {_isSitting} -> {value}\n{System.Environment.StackTrace}");
                }
                _isSitting = value;
            }
        }

        // 인스펙터에서 개별 항목으로 확인 및 디버깅이 가능하도록 직렬화 구조체 정의
        [System.Serializable]
        public struct FatigueTriggerFlags
        {
            public bool over20;
            public bool over50;
            public bool over80;
            public bool over100;

            public void Reset()
            {
                over20 = false;
                over50 = false;
                over80 = false;
                over100 = false;
            }
        }

        [Header("피로도")]
        private bool _beingChased = false;
        public bool beingChased
        {
            get { return _beingChased; }
            set
            {
                // 값이 바뀔 때만 로그 찍기
                if (_beingChased != value)
                {
                    // 누가 바꿨는지 추적하기 위해 스택 트레이스 출력
                    Debug.Log($"[감시] beingChased 값이 변경됨: {_beingChased} -> {value}\n{System.Environment.StackTrace}");
                }
                _beingChased = value;
            }
        }

        private int chasingMonsterCount = 0;

        [SerializeField] private FatigueTriggerFlags fatigueTriggers; // ◀ 이제 인스펙터창에서 체크박스로 확인 가능합니다.
        public float currentStatModifier { get; private set; } = 1.0f;

        [SerializeField] private int maxFatigue = 100;
        [SerializeField] private int _fatigue = 0;
        public int Fatigue
        {
            get => _fatigue;
            set
            {
                int clampedValue = Mathf.Clamp(value, 0, maxFatigue);
                if (_fatigue == clampedValue) return;

                _fatigue = clampedValue;
                Debug.Log($"[피로도 변경] 현재 피로도: {_fatigue}");

                HandleFatigueEvents(_fatigue);
            }
        }

        [Header("회피 시스템")]
        private List<SkeletonController> activeAttackingMonsters = new List<SkeletonController>();

        [SerializeField]
        public bool canMove = true;                         // 플레이어 움직임 허용
        public float currentSpeed;                           // 현재 속도
        public Vector3 dir;

        [Header("대쉬")]
        public float dashSpeed = 15f;
        public float dashDuration = 0.2f;
        [HideInInspector] public bool isDashing = false;

        [Header("점프")]
        [SerializeField] private float groundYOffset;
        [SerializeField] private LayerMask _groundLayer;
        public virtual LayerMask groundLayer => _groundLayer;

        [SerializeField]
        private float sphereRadius = 0.05f;
        Vector3 spherePos;                                      // 플레이어 지면 확인용 구체

        [Header("중력")]
        [SerializeField] private float gravity = -9.81f;        // 중력값
        private Vector3 velocity;
        [SerializeField] private bool _isGrounded;


        [Header("컴포넌트 참조")]
        public Animator anim;
        public SpriteRenderer sr;
        public CharacterController cController;
        public PlayerHealth playerHealth;

        [Header("전투")]
        [SerializeField] private GameObject attackHitBox;       // 히트박스
        [HideInInspector] public Vector3 lastHisPos;            // 맞은 위치값
        public Transform vfxPos;

        [Tooltip("전투 시 반동")]
        private float attackLungeSpeed = 5f;                    // 공격 반동 속도
        private float attackLungeDuration = 0.2f;               // 공격 반동 지속시간

        #endregion

        #region 플레이어 FSM
        public MovementBaseState previousState;
        public MovementBaseState currentState;

        public PlayerIdleState playerIdle = new PlayerIdleState();
        public PlayerWalkState playerWalk = new PlayerWalkState();
        public PlayerRunState playerRun = new PlayerRunState();
        public PlayerAttackState playerAttack = new PlayerAttackState();
        public PlayerDashState dashState = new PlayerDashState();
        public PlayerDamagedState damagedState = new PlayerDamagedState();
        public PlayerDeathState deathState = new PlayerDeathState();
        public PlayerSitState sitState = new PlayerSitState();
        public PlayerHealState healState = new PlayerHealState();

        #endregion

        #region Ui 에 따른 상태
        void OnEnable()
        {
            UiManager.OnAnyUiStateChanged += HandleUiStateChanged;
            UiManager.OnDeathed += HandleDeathed;

            SaveManager.OnLoadGame += HandleLoadGame;
        }

        void OnDisable()
        {
            UiManager.OnAnyUiStateChanged -= HandleUiStateChanged;
            UiManager.OnDeathed -= HandleDeathed;

            SaveManager.OnLoadGame -= HandleLoadGame;

            if (playerHealth != null)
            {
                playerHealth.OnPlayerHit -= HandleHit;
                playerHealth.OnPlayerDied -= HandleDeath;
            }
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            cController = GetComponent<CharacterController>();
            anim = GetComponent<Animator>();

            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogError("[PlayerMovement] playeHealth 를 찾지 못함");
            }
            else
            {
                Debug.Log("[PlayerMovement] playeHealth 를 찾음");
            }

            LoadStatsFromJson();

            if (attackHitBox != null)
            {
                attackHitBox.SetActive(false);
            }
        }

        private void Start()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.player = this;
            }

            DontDestroyOnLoad(this);

            SwitchState(playerIdle);

            if (playerHealth != null)
            {
                playerHealth.OnPlayerHit += HandleHit;
                playerHealth.OnPlayerDied += HandleDeath;
            }

            //beingChased = true;
        }

        private void Update()
        {
            if (DialogueManager.isDialogueActive) { return; }
            if (!canMove) { return; }
            if (isDashing) { return; }

            _isGrounded = IsGrounded();
            Gravity();

            if (!isAttacking && !isSitting)
            {
                PlayerMove();
                HandleSpriteFlip();
            }

            if (Input.GetMouseButtonDown(0) && IsGrounded() && !isAttacking)
            {
                if (IsPointerOverUIObject())
                {
                    Debug.Log("UI 위에 마우스가 있어서 공격 입력을 무시합니다.");
                    return;
                }
                if (playerHealth == null)
                {
                    Debug.LogError("[PlayerMovement] PlayerHealth 를 찾을 수 없음");
                }
                if (playerHealth.hasWeapon == false)
                {
                    Debug.Log("플레이어가 무기가 없음");
                    return;
                }

                previousState = currentState;
                SwitchState(playerAttack);
                return;
            }
            anim.SetFloat("lastMoveX", lastMoveX);
            anim.SetFloat("lastMoveZ", lastMoveZ);

            if (Input.GetKeyDown(KeyCode.R) && !isAttacking && !isDashing)
            {
                TryUsePotion();
            }
            currentState.UpdateState(this);
        }

        private void HandleUiStateChanged(bool isAnyUiOpen)
        {
            canMove = !isAnyUiOpen;
        }

        // 기존의 enum 배열 판정 로직을 새 구조체 플래그에 맞게 수정했습니다.
        private void HandleFatigueEvents(int value)
        {
            if (value >= 100 && !fatigueTriggers.over100)
            {
                fatigueTriggers.over100 = true;
                currentStatModifier = 0.0f;
                Debug.Log("피로도 100 도달! 플레이어가 쓰러집니다.");
                ApplyStatModifier();
                HandleDeath();
                return;
            }

            if (value >= 80 && !fatigueTriggers.over80)
            {
                fatigueTriggers.over80 = true;
                currentStatModifier = 0.5f;
                Debug.Log("피로도 80 돌파! 공격력/이동속도 50% 하락");
                ApplyStatModifier();
            }
            else if (value >= 50 && value < 80 && !fatigueTriggers.over50)
            {
                fatigueTriggers.over50 = true;
                currentStatModifier = 0.75f;
                Debug.Log("피로도 50 돌파! 공격력/이동속도 25% 하락");
                ApplyStatModifier();
            }
            else if (value >= 20 && value < 50 && !fatigueTriggers.over20)
            {
                fatigueTriggers.over20 = true;
                currentStatModifier = 0.9f;
                Debug.Log("피로도 20 돌파! 공격력/이동속도 10% 하락");
                ApplyStatModifier();
            }
        }

        private void ApplyStatModifier()
        {
            // 1. 이동 속도 차감 적용
            walkSpeed = baseWalkSpeed * currentStatModifier;
            runSpeed = baseRunSpeed * currentStatModifier;

            // FSM 상태 스크립트가 실시간으로 변경된 속도를 인지할 수 있도록 현재 속도 갱신 유도
            if (currentState == playerWalk) currentSpeed = walkSpeed;
            else if (currentState == playerRun) currentSpeed = runSpeed;

            // 2. 공격력 차감 적용 (PlayerHealth 컴포넌트 연동)
            if (playerHealth != null)
            {
                // PlayerHealth 측에 배율(currentStatModifier)을 넘겨주어 공격력을 계산하게 합니다.
                playerHealth.UpdateDamageModifier(currentStatModifier);
            }

            Debug.Log($"[스탯 조정 완료] 현재 속도 배율: {currentStatModifier * 100}% | Walk: {walkSpeed}, Run: {runSpeed}");
        }

        public void ResetFatigueSystem()
        {
            _fatigue = 0;
            currentStatModifier = 1.0f;

            fatigueTriggers.Reset();

            ApplyStatModifier();
        }

        public void RegisterChaser()
        {
            chasingMonsterCount++;
            beingChased = (chasingMonsterCount > 0);
            Debug.Log($"[추격 등록] 현재 추격 중인 몬스터 수: {chasingMonsterCount} | beingChased: {beingChased}");
        }

        public void UnregisterChaser()
        {
            chasingMonsterCount--;
            if (chasingMonsterCount < 0) chasingMonsterCount = 0;

            beingChased = (chasingMonsterCount > 0);
            Debug.Log($"[추격 해제] 현재 추격 중인 몬스터 수: {chasingMonsterCount} | beingChased: {beingChased}");
        }

        public void RegisterDodgeableMonster(SkeletonController monster)
        {
            if (!activeAttackingMonsters.Contains(monster))
                activeAttackingMonsters.Add(monster);
        }

        public void UnregisterDodgeableMonster(SkeletonController monster)
        {
            if (activeAttackingMonsters.Contains(monster))
                activeAttackingMonsters.Remove(monster);
        }

        public void CheckPerfectDodge()
        {
            if (activeAttackingMonsters.Count > 0)
            {
                SkeletonController targetMonster = activeAttackingMonsters[0];
                OnPerfectDodgeSuccess(targetMonster);
            }
        }

        private void OnPerfectDodgeSuccess(SkeletonController monster)
        {
            Debug.Log($"⚡ 저스트 회피 성공! 대상: {monster.name}");

            float slowRadius = 10.0f;
            //int monsterLayerMask = LayerMask.GetMask("Monster");

            Collider[] surroundingMonsters = Physics.OverlapSphere(transform.position, slowRadius);
            foreach (var col in surroundingMonsters)
            {
                SkeletonController targetMonster = col.GetComponent<SkeletonController>();
                if (targetMonster != null)
                {
                    targetMonster.ApplySlowDebuff(1.0f, 90f);
                    Debug.Log($"[회피 보너스] {targetMonster.name}에게 90% 슬로우 디버프 적용!");
                }
            }

            activeAttackingMonsters.Clear();
        }

        private void HandleDeathed()
        {
            Debug.Log("[PlayerMovement] HandleDeathed");

            SaveManager manager = SaveManager.Instance;
            if (manager != null)
            {
                Debug.Log($"[감시] saveCount 현재 값: {saveCount}");

                if (saveCount >= limitedSaveCount)
                {
                    Debug.Log("[PlayerMovement] Limit reached. Resetting Game.");
                    manager.ResetGame();
                    return;
                }

                saveCount++;
                manager.LoadGame();
            }
            else
                Debug.LogError("[PlayerMovement] Critical Error: SaveManager object is missing in the scene!");
        }

        private void HandleLoadGame()
        {
            SwitchState(playerIdle);
        }

        private bool IsPointerOverUIObject()
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            var results = new System.Collections.Generic.List<RaycastResult>();

            EventSystem.current.RaycastAll(eventData, results);

            return results.Count > 0;
        }
        #endregion

        [Tooltip("데이터 파싱")]
        private void LoadStatsFromJson()
        {
            TextAsset playerStatFile = Resources.Load<TextAsset>("playerStats");

            if (playerStatFile != null)
            {
                PlayerStats stats = JsonUtility.FromJson<PlayerStats>(playerStatFile.text);

                // ◀ [수정] JSON에서 로드한 순수 기본 속도를 원본 변수에 백업
                this.baseWalkSpeed = stats.walkSpeed;
                this.baseRunSpeed = stats.runSpeed;

                // 인스펙터 및 이동 처리에 사용될 변수 초기화
                this.walkSpeed = baseWalkSpeed;
                this.runSpeed = baseRunSpeed;

                if (playerHealth != null)
                {
                    playerHealth.InitializeHealth(stats.maxHp, stats.baseDamage);
                }
            }
            else
            {
                Debug.Log("플레이어 데이터를 담은 json 파일을 찾을 수 없습니다");
            }
        }

        private void TryUsePotion()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("SaveManager가 없습니다.");
                return;
            }

            if (SaveManager.Instance.potionSlot == null)
            {
                Debug.LogError("SaveManager에 PotionSlot이 등록되지 않았습니다.");
                return;
            }

            QuickSlot_Ui potionSlot = SaveManager.Instance.potionSlot;
            Item item = potionSlot.equippedItem;

            if (item == null)
            {
                Debug.Log("포션 슬롯이 비어있습니다.");
                return;
            }

            Debug.Log($"슬롯 아이템: {item.name}, 타입: {item.GetType()}");

            if (item is Potion potion)
            {
                if (playerHealth.currentHp >= playerHealth.maxHp)
                {
                    Debug.Log("플레이어의 체력을 더 이상 회복할 수 없습니다.");
                    return;
                }

                Debug.Log("포션 타입 확인 완료! 힐 상태로 전환합니다.");
                healState.SetPotion(potion);
                SwitchState(healState);
            }
            else
            {
                Debug.LogError($"아이템이 Potion 클래스가 아닙니다! (현재 타입: {item.GetType()})");
            }
        }

        public void ConsumePotionFromQuickSlot()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.potionSlot != null)
            {
                SaveManager.Instance.potionSlot.UseItem();
            }
        }


        private IEnumerator AttackLungeCoroutine()
        {
            float startTime = Time.time;
            Vector3 lungeDir;

            if (dir.magnitude > 0.1f)
            {
                lungeDir = dir;
            }
            else
            {
                lungeDir = new Vector3(lastMoveX, 0, lastMoveZ);
                if (lungeDir.magnitude < 0.1f)
                {
                    lungeDir = transform.forward;
                }
            }

            while (Time.time < startTime + attackLungeDuration)
            {
                cController.Move(lungeDir.normalized * attackLungeSpeed * Time.deltaTime);
                yield return null;
            }
        }

        #region 애니메이션 이벤트에서 사용할 코루틴 & 히트박스
        [Tooltip("애니메이션 이벤트")]
        public void AnimationEvent_StartAttackLunge()
        {
            StartCoroutine(AttackLungeCoroutine());
        }

        [Tooltip("히트박스")]
        public void AnimationEvent_EnableHitBox()
        {
            if (attackHitBox != null)
                attackHitBox.SetActive(true);
        }
        public void AnimationEvent_DisableHitBox()
        {
            if (attackHitBox != null)
                attackHitBox.SetActive(false);
        }
        public void AnimationEvent_DisalbeFlipX()
        {
            if (isAttacking)
            {
                sr.flipX = false;
            }
        }

        [Tooltip("애니메이션 이벤트 : 달리기 애니메이션 종료")]
        public void AnimationEvent_WalkFinished()
        {
            if (beingChased)
                Fatigue += 1;
        }

        [Tooltip("애니메이션 이벤트 : 회피 애니메이션 시작")]
        public void AnimationEvent_DashStarted()
        {
            if (beingChased)
                Fatigue += 2;

            CheckPerfectDodge();
        }

        [Tooltip("애니메이션 이벤트 : 공격 애니메이션 시작")]
        public void AnimationEvent_AttackStarted()
        {
            if (beingChased)
                Fatigue += 2;
        }

        [Tooltip("애니메이션 이벤트 : 공격 애니메이션 종료")]
        public void AnimationEvent_AttackFinished()
        {
            if (!isAttacking) return;

            float xInput = Input.GetAxisRaw("Horizontal");
            float zInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(xInput) > 0.1f || Mathf.Abs(zInput) > 0.1f)
            {
                SwitchState(playerWalk);
            }
            else
            {
                SwitchState(playerIdle);
            }
        }

        public void AnimationEvent_SaveGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                ResetFatigueSystem();
            }
        }

        [Tooltip("애니메이션 이벤트 : 사망 애니메이션 종료")]
        public void AnimationEvent_DeathFinished()
        {
            if (UiManager.Instance != null)
            {
                UiManager.Instance.StartResponse();
                ResetFatigueSystem();
            }
        }
        #endregion  


        private void PlayerMove()
        {
            xInput = Input.GetAxisRaw("Horizontal");
            zInput = Input.GetAxisRaw("Vertical");

            dir = transform.forward * zInput + transform.right * xInput;

            if (dir.magnitude > 0.01f)
            {
                lastMoveX = xInput;
                lastMoveZ = zInput;

                anim.SetFloat("xInput", xInput);
                anim.SetFloat("zInput", zInput);
            }
            Vector3 finalVelocity = dir.normalized * currentSpeed;

            cController.Move(finalVelocity * Time.deltaTime);
        }
        private void HandleSpriteFlip()
        {
            if (xInput != 0)
            {
                sr.flipX = (xInput < 0);
            }
            else
                sr.flipX = (lastMoveX < 0);
        }

        public void HandleHit(Vector3 dir)
        {
            lastHisPos = dir;
            SwitchState(damagedState);
        }

        public void HandleDeath()
        {
            if (currentState == deathState) return;
            Debug.Log("[PlayerMovement] 플레이어 죽음");

            SwitchState(deathState);
        }

        public void SwitchState(MovementBaseState state)
        {
            currentState?.ExitState(this);
            currentState = state;
            currentState.EnterState(this);
        }

        private bool IsGrounded()
        {
            spherePos = new Vector3(transform.position.x, transform.position.y - groundYOffset, transform.position.z);
            if (Physics.CheckSphere(spherePos, cController.radius - sphereRadius, _groundLayer))
            {
                return true;
            }
            return false;
        }

        private void Gravity()
        {
            if (isAttacking)
            {
                velocity = Vector3.zero;
                return;
            }
            if (!IsGrounded())
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else if (velocity.y < 0)
            {
                velocity.y = -2;
            }
            cController.Move(velocity * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spherePos, cController.radius - sphereRadius);
        }
    }
}