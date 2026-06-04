using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace KW
{
    public class PlayerMovement : MonoBehaviour
    {
        // 진짜 플레이어를 전역에서 유일하게 보존하기 위한 싱글톤 인스턴스
        public static PlayerMovement Instance { get; private set; }

        #region 변수
        [Header("데이터파싱")]
        [TextArea(10, 1)]
        public string DataParsingFrom;

        [Header("세이브 및 리스폰")]
        public int saveCount = 0;                     // 세이브 가능한 횟수               
        public int limitedSaveCount = 5;              // 세이브 유효 횟수               

        [Tooltip("세이브 파일이 없을 때 기본적으로 부활할 스폰 포인트 ID")]
        public int defaultRespawnSubID = 0;

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
                if (_isSitting != value)
                {
                    Debug.Log($"[감시] isSitting 값이 변경됨: {_isSitting} -> {value}\n{System.Environment.StackTrace}");
                }
                _isSitting = value;
            }
        }

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
                if (_beingChased != value)
                {
                    Debug.Log($"[감시] beingChased 값이 변경됨: {_beingChased} -> {value}\n{System.Environment.StackTrace}");
                }
                _beingChased = value;
            }
        }

        private int chasingMonsterCount = 0;

        [SerializeField] private FatigueTriggerFlags fatigueTriggers;
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

        #region UI 및 매니저 이벤트 연동
        void OnEnable()
        {
            UiManager.OnAnyUiStateChanged += HandleUiStateChanged;
            UiManager.OnDeathed += HandleDeathed;
            SaveManager.OnLoadGame += HandleLoadGame;

            if (playerHealth != null)
            {
                playerHealth.OnPlayerHit -= HandleHit;
                playerHealth.OnPlayerHit += HandleHit;

                playerHealth.OnPlayerDied -= HandleDeath;
                playerHealth.OnPlayerDied += HandleDeath;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
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

            SceneManager.sceneLoaded -= OnSceneLoaded; // 🛠️ 메모리 누수 방지를 위한 확실한 해제
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            sr = GetComponent<SpriteRenderer>();
            cController = GetComponent<CharacterController>();
            anim = GetComponent<Animator>();
            playerHealth = GetComponent<PlayerHealth>();

            if (playerHealth == null) Debug.LogError("[PlayerMovement] playerHealth 를 찾지 못함");

            LoadStatsFromJson();

            if (attackHitBox != null) attackHitBox.SetActive(false);
        }

        private void Start()
        {
            RebindManagers();
            SwitchState(playerIdle);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return;

            Debug.Log($"[PlayerMovement] 새로운 씬 로드됨: {scene.name}. 중력 초기화 및 매니저 재연동을 시작합니다.");

            if (cController != null) cController.enabled = false;

            velocity = Vector3.zero;
            _isGrounded = true;

            RebindManagers();
            SwitchState(playerIdle);

            if (cController != null) cController.enabled = true;
        }

        private void RebindManagers()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.player = this;
                Debug.Log("[PlayerMovement] SaveManager.Instance에 진짜 플레이어 등록 완료.");
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] 새로운 씬에 SaveManager가 존재하지 않습니다.");
            }
        }

        private void Update()
        {
            if (DialogueManager.isDialogueActive) { return; }
            if (!canMove) { return; }
            if (isDashing) { return; }

            _isGrounded = IsGrounded();
            UpdateGravity(); // 🛠️ 물리 연산 방식 변경 (계산만 분리)

            if (!isAttacking && !isSitting)
            {
                PlayerMove();
                HandleSpriteFlip();
            }

            // 🛠️ 마우스 왼쪽 클릭 입력 처리 (UI 체크 분기점 안전화)
            if (Input.GetMouseButtonDown(0) && _isGrounded && !isAttacking)
            {
                if (IsPointerOverUIObject())
                {
                    Debug.Log("UI 위에 마우스가 있어서 공격 입력을 무시합니다.");
                    return;
                }
                if (playerHealth == null)
                {
                    Debug.LogError("[PlayerMovement] PlayerHealth 를 찾을 수 없음");
                    return;
                }

                if (playerHealth.hasWeapon == false)
                {
                    Debug.LogError($"[공격 실패] {gameObject.name} 플레이어에게 무기가 없습니다! (Instance ID: {gameObject.GetInstanceID()})");
                    return;
                }

                // 🛠️ 공격 직전 미끄러짐 방지를 위해 속도와 입력을 초기화합니다.
                currentSpeed = 0;
                xInput = 0;
                zInput = 0;

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

        private void HandleFatigueEvents(int value)
        {
            if (value >= 100)
            {
                if (!fatigueTriggers.over100)
                {
                    fatigueTriggers.over100 = true;
                    currentStatModifier = 0.0f;
                    Debug.Log("피로도 100 도달! 플레이어가 쓰러집니다.");
                    ApplyStatModifier();
                    HandleDeath();
                }
                return;
            }
            else fatigueTriggers.over100 = false;

            if (value >= 80)
            {
                if (!fatigueTriggers.over80)
                {
                    fatigueTriggers.over80 = true;
                    currentStatModifier = 0.5f;
                    Debug.Log("피로도 80 돌파! 공격력/이동속도 50% 하락");
                    ApplyStatModifier();
                }
            }
            else fatigueTriggers.over80 = false;

            if (value >= 50)
            {
                if (!fatigueTriggers.over50 && value < 80)
                {
                    fatigueTriggers.over50 = true;
                    currentStatModifier = 0.75f;
                    Debug.Log("피로도 50 돌파! 공격력/이동속도 25% 하락");
                    ApplyStatModifier();
                }
            }
            else fatigueTriggers.over50 = false;

            if (value >= 20)
            {
                if (!fatigueTriggers.over20 && value < 50)
                {
                    fatigueTriggers.over20 = true;
                    currentStatModifier = 0.9f;
                    Debug.Log("피로도 20 돌파! 공격력/이동속도 10% 하락");
                    ApplyStatModifier();
                }
            }
            else fatigueTriggers.over20 = false;

            if (value < 20 && currentStatModifier != 1.0f)
            {
                currentStatModifier = 1.0f;
                fatigueTriggers.Reset();
                Debug.Log("피로도가 안정권으로 회복되었습니다. 모든 스탯 정상화.");
                ApplyStatModifier();
            }
        }

        private void ApplyStatModifier()
        {
            walkSpeed = baseWalkSpeed * currentStatModifier;
            runSpeed = baseRunSpeed * currentStatModifier;

            if (currentState == playerWalk) currentSpeed = walkSpeed;
            else if (currentState == playerRun) currentSpeed = runSpeed;

            if (playerHealth != null)
            {
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
        #endregion

        #region 추격 및 저스트 회피 시스템
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
        #endregion

        #region 세이브 및 데이터 로딩 관련
        private void HandleDeathed()
        {
            if (currentState == deathState)
            {
                Debug.LogWarning("[PlayerMovement] 이미 사망 상태이므로 중복 사망 처리를 무시합니다.");
                return;
            }

            Debug.Log("[PlayerMovement] 플레이어 사망 이벤트 발생");

            SaveManager manager = SaveManager.Instance;
            if (manager != null)
            {
                saveCount++;
                Debug.Log($"[감시] 현재 죽음 횟수: {saveCount} / {limitedSaveCount}");

                if (saveCount >= limitedSaveCount)
                {
                    Debug.LogWarning("[PlayerMovement] 최대 죽음 제한 횟수 도달! 전체 게임을 리셋하고 타이틀로 이동합니다.");
                    saveCount = 0;
                    manager.ResetGame();
                    return;
                }

                SwitchState(deathState);
                manager.HandlePlayerRespawn(saveCount);
            }
            else
            {
                Debug.LogError("[PlayerMovement] Critical Error: 씬에 SaveManager가 없습니다!");
            }
        }

        public void RespawnPlayerAtSpawnPoint(int targetID)
        {
            Debug.Log($"[PlayerMovement] RespawnPlayerAtSpawnPoint({targetID}) 시작");

            if (cController != null) cController.enabled = false;
            velocity = Vector3.zero;

            // 🛠️ FindObjectsByType 최적화 버전 (가장 직관적인 검색 기법 사용)
            PlayerSpawnPoint[] allSpawnPoints = UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            PlayerSpawnPoint targetSpawnPoint = null;

            foreach (var sp in allSpawnPoints)
            {
                if (sp.spawnID == targetID)
                {
                    targetSpawnPoint = sp;
                    break;
                }
            }

            if (targetSpawnPoint != null)
            {
                transform.position = targetSpawnPoint.transform.position;
                transform.rotation = targetSpawnPoint.transform.rotation;
                Debug.Log($"[PlayerMovement] 📍 SpawnID [{targetID}] 좌표로 이동 성공: {transform.position}");
            }
            else if (allSpawnPoints.Length > 0)
            {
                transform.position = allSpawnPoints[0].transform.position;
                transform.rotation = allSpawnPoints[0].transform.rotation;
                Debug.Log($"[PlayerMovement] 📍 백업 스폰포인트 [{allSpawnPoints[0].name}] 좌표로 이동 완료");
            }
            else
            {
                transform.position = Vector3.zero;
                Debug.LogError("[PlayerMovement] ❌ 씬에 스폰 포인트가 없어 원점(0,0,0)으로 보냅니다.");
            }

            if (cController != null) cController.enabled = true;

            if (playerIdle != null)
            {
                SwitchState(playerIdle);
            }

            Debug.Log("[PlayerMovement] 부활 프로세스 완료. 플레이어가 다시 조작 가능한 상태가 되었습니다.");
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

        private void LoadStatsFromJson()
        {
            TextAsset playerStatFile = Resources.Load<TextAsset>("playerStats");

            if (playerStatFile != null)
            {
                PlayerStats stats = JsonUtility.FromJson<PlayerStats>(playerStatFile.text);

                this.baseWalkSpeed = stats.walkSpeed;
                this.baseRunSpeed = stats.runSpeed;

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
            if (SaveManager.Instance == null || SaveManager.Instance.potionSlot == null)
            {
                Debug.LogError("SaveManager 또는 PotionSlot이 누락되었습니다.");
                return;
            }

            QuickSlot_Ui potionSlot = SaveManager.Instance.potionSlot;
            Item item = potionSlot.equippedItem;

            if (item == null)
            {
                Debug.Log("포션 슬롯이 비어있습니다.");
                return;
            }

            if (item is Potion potion)
            {
                if (playerHealth.currentHp >= playerHealth.maxHp)
                {
                    Debug.Log("플레이어의 체력을 더 이상 회복할 수 없습니다.");
                    return;
                }

                Debug.Log("포션 타입 확인 완료! 힐 상태로 전환하고 포션을 소비합니다.");

                ConsumePotionFromQuickSlot();

                healState.SetPotion(potion);
                SwitchState(healState);
            }
        }

        public void ConsumePotionFromQuickSlot()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.potionSlot != null)
            {
                SaveManager.Instance.potionSlot.UseItem();
            }
        }
        #endregion

        #region 애니메이션 이벤트 연동 코루틴
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

        public void AnimationEvent_StartAttackLunge() { StartCoroutine(AttackLungeCoroutine()); }
        public void AnimationEvent_EnableHitBox() { if (attackHitBox != null) attackHitBox.SetActive(true); }
        public void AnimationEvent_DisableHitBox() { if (attackHitBox != null) attackHitBox.SetActive(false); }
        public void AnimationEvent_DisalbeFlipX() { if (isAttacking) sr.flipX = false; }

        public void AnimationEvent_WalkFinished() { if (beingChased) Fatigue += 1; }
        public void AnimationEvent_DashStarted() { if (beingChased) Fatigue += 2; CheckPerfectDodge(); }
        public void AnimationEvent_AttackStarted() { if (beingChased) Fatigue += 2; }

        public void AnimationEvent_AttackFinished()
        {
            if (!isAttacking) return;

            float xInput = Input.GetAxisRaw("Horizontal");
            float zInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(xInput) > 0.1f || Mathf.Abs(zInput) > 0.1f)
                SwitchState(playerWalk);
            else
                SwitchState(playerIdle);
        }

        public void AnimationEvent_SaveGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                ResetFatigueSystem();
            }
        }

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

            // 등각 투영(쿼터뷰) 또는 3D 탑다운의 올바른 벡터 계산 지향
            dir = transform.forward * zInput + transform.right * xInput;

            if (dir.magnitude > 0.01f)
            {
                lastMoveX = xInput;
                lastMoveZ = zInput;

                anim.SetFloat("xInput", xInput);
                anim.SetFloat("zInput", zInput);
            }

            // 🛠️ 수평 이동 벡터와 수직 중력 벡터를 결합하여 하나의 Move 호출로 처리합니다.
            Vector3 finalVelocity = (dir.normalized * currentSpeed) + velocity;
            cController.Move(finalVelocity * Time.deltaTime);
        }

        private void HandleSpriteFlip()
        {
            if (xInput != 0) sr.flipX = (xInput < 0);
            else sr.flipX = (lastMoveX < 0);
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
            return Physics.CheckSphere(spherePos, cController.radius - sphereRadius, _groundLayer);
        }

        // 🛠️ 함수 역할 변경: 물리 적용을 PlayerMove로 일임하고 중력 값(velocity)만 실시간 업데이트합니다.
        private void UpdateGravity()
        {
            if (isAttacking)
            {
                velocity = Vector3.zero;
                return;
            }

            if (!_isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else if (velocity.y < 0)
            {
                velocity.y = -2f; // 땅에 붙어있을 때 최소한의 하방 압력 유지
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spherePos, cController.radius - sphereRadius);
        }
    }
}