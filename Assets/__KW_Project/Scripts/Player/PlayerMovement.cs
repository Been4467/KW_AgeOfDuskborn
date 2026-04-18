using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KW
{
    public class PlayerMovement : MonoBehaviour
    {
        #region 변수
        [Header("데이터파싱")]
        [TextArea(10, 1)]
        public string DataParsingFrom;

        [Header("움직임")]
        public float walkSpeed = 3f;                         // 걷는 속도                    
        public float runSpeed = 5f;                          // 달리는 속도
        public float airSpeed = 4f;                          // 공중에서의 속도 (필요없음)
        public float xInput, zInput;
        public float lastMoveX, lastMoveZ;
        public bool isAttacking = false;

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
        }

        void OnDisable()
        {
            UiManager.OnAnyUiStateChanged -= HandleUiStateChanged;

            if (playerHealth != null)
            {
                playerHealth.OnPlayerHit -= HandleHit;
                playerHealth.OnPlayerDied -= HandleDeath;
            }
        }

        private void HandleUiStateChanged(bool isAnyUiOpen)
        {
            canMove = !isAnyUiOpen;
        }

        private bool IsPointerOverUIObject()
        {
            // 현재 활성화된 EventSystem이 없으면 false 반환
            if (EventSystem.current == null) return false;

            // 현재 마우스 포인터의 데이터를 가져옴
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            // UI 레이캐스트를 저장할 리스트
            var results = new System.Collections.Generic.List<RaycastResult>();

            // EventSystem을 통해 UI 요소에 레이캐스트 수행 
            EventSystem.current.RaycastAll(eventData, results);

            // 결과 리스트에 UI 요소가 하나라도 있으면 true
            return results.Count > 0;
        }
        #endregion

        [Tooltip("데이터 파싱")]
        private void LoadStatsFromJson()
        {
            // json 파일을 text 로서 읽어옴
            TextAsset playerStatFile = Resources.Load<TextAsset>("playerStats");

            if (playerStatFile != null)
            {
                // 읽어온 json의 내용을 PlayerStats 에 적용시키고, 현재 스크립트에도 적용
                PlayerStats stats = JsonUtility.FromJson<PlayerStats>(playerStatFile.text);

                this.walkSpeed = stats.walkSpeed;
                this.runSpeed = stats.runSpeed;
                // this.baseDamage = stats.baseDamage; 
                // this.maxHp = stats.maxHp;

                if (playerHealth != null)
                {
                    playerHealth.InitializeHealth(stats.maxHp, stats.baseDamage);
                }

                // Debug.Log("플레이어 기본 스탯 로드 완료 : maxHP(" + stats.maxHp + ")" + " , baseDamage(" + baseDamage + ")");
            }
            else
            {
                Debug.Log("플레이어 데이터를 담은 json 파일을 찾을 수 없습니다");
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

            // 시작 시에는 히트박스를 비활성화
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

            // 씬 시작 시 상태(state) 설정
            SwitchState(playerIdle);

            if (playerHealth != null)
            {
                playerHealth.OnPlayerHit += HandleHit;
                playerHealth.OnPlayerDied += HandleDeath;
            }
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

                // 공격 상태에서는 아래의 로직을 사용하지 않기 때문에 return
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

        private void TryUsePotion()
        {
            // Debug.Log("R키 입력 감지됨. 포션 사용 시도...");

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

            // 반동 방향
            Vector3 lungeDir;

            // 기존의 방향을 lungeDir 로 지정
            if (dir.magnitude > 0.1f)
            {
                lungeDir = dir;
            }
            else
            {
                lungeDir = new Vector3(lastMoveX, 0, lastMoveZ);
                // 기존 입력값이 없으면
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


        [Tooltip("애니메이션 이벤트 : 공격 애니메이션 종료")]
        public void AnimationEvent_AttackFinished()
        {
            if (!isAttacking) return;


            float xInput = Input.GetAxisRaw("Horizontal");
            float zInput = Input.GetAxisRaw("Vertical");

            // 입력 값이 있으면 
            if (MathF.Abs(xInput) > 0.1f || Mathf.Abs(zInput) > 0.1f)
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
                Debug.Log("화톳불에 앉아 게임을 저장했습니다.");

                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowMessage("게임이 저장되었습니다.\n\n체력이 회복되었습니다.");
                }
            }
        }

        #endregion  


        private void PlayerMove()
        {
            xInput = Input.GetAxisRaw("Horizontal");
            zInput = Input.GetAxisRaw("Vertical");

            dir = transform.forward * zInput + transform.right * xInput;

            // 애니메이터에서 0 값을 받지 않도록 (달리다가 IDLE 로 전환될 때 오류 수정)
            if (dir.magnitude > 0.01f)
            {
                lastMoveX = xInput;
                lastMoveZ = zInput;

                anim.SetFloat("xInput", xInput);
                anim.SetFloat("zInput", zInput);
            }
            // 이동
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
            // 플레이어의 바닥 판정
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
            // 점프 했을 때 gravity 만큼 더 빨르게 낙하
            if (!IsGrounded())
            {
                velocity.y += gravity * Time.deltaTime;
            }
            // 오류 방지 & 바닥에 붙여놓기
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