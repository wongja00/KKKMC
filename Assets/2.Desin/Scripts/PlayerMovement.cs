using System.Timers;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder.Shapes;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using System.Collections;
using Unity.Mathematics;
using System;
using UnityEngine.Animations.Rigging;
using UnityEngine.Animations;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController controller;
    public GameObject playerCharacter;
    public CharacterBase player;


    [SerializeField]
    private CinemachineCamera playerCamera;

    [SerializeField]
    private Animator animator;
    
    [SerializeField]
    private KeyCode dashKey = KeyCode.LeftShift;

    [SerializeField]
    private Aiming aiming;
    [SerializeField]
    private CombatSystem combat;

    public float speed = 6f;
    public float originSpeed = 6f;
    public float maxSpeed = 12f;
    public float gravity = 9.81f;
    public float jumpHight = 3.0f;
    public float customAirDrag = 4.0f;

    [Header("Upper Body Rotation")]
    public AimConstraint aimRig;
    public float rifTransitionSpeed = 12f;
    public float targetRigweight = 0f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    bool isAiming ;


    Vector3 velocity;
    Vector2 animVelocity;

    bool isGrounded;

    void Awake()
    {
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(animator == null) animator = GetComponentInChildren<PlayerModel>().animator;
        if(playerCharacter == null) playerCharacter = animator.transform.gameObject;
        if(player == null) player = GetComponent<CharacterBase>();

        if (!isLocalPlayer)
        {
            controller.enabled = false;
        }
        originSpeed = player.speed;
        velocity = Vector3.zero;

        playerCamera = CameraManager.Instance.GetPlayerCemera();
    }

    // Update is called once per frame
    void Update()
    {
        if(isLocalPlayer == false) return;
        
        isAiming = aiming != null && (aiming.isAiming || aiming.isADS);

        HandleMovement();
        HandleUpperBodyRotation();
        RotateBodyAiming();
    }   

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        float airDrag;

        if(isGrounded && velocity.y < 0 )
        {
            velocity.y = -1f;
            airDrag = 0f;
        }
        else
        {
            airDrag = customAirDrag;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
                //Y축 제거(수평이동만)
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if(cameraForward.sqrMagnitude < 0.001f)
        {
            cameraForward = playerCharacter.transform.forward;
        }

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 move = cameraRight * x + cameraForward * z;
        
        if(isAiming)
        {
            animVelocity.x = x  != 0 ? x / 2 : 0;
            animVelocity.y = z  != 0 ? z / 2 : 0;
        }
        else
        {
            animVelocity.y = Mathf.Abs(z) <= 0 ?  Mathf.Abs(x) : Mathf.Abs(z);
            animVelocity.x = 0;
        }

        float currentSpeed = originSpeed;
        if(Input.GetKey(dashKey))
        {
            currentSpeed = originSpeed * 2;
            animVelocity *= 4;
        }

        //입력시 카메라 기준으로 회전
        if(move.magnitude > 0.01f && !isAiming && combat.canRotateDuringAttack)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            //playerCharacter.transform.rotation = Quaternion.Slerp(playerCharacter.transform.rotation, targetRotation, Time.deltaTime * 10);
            playerCharacter.transform.rotation = targetRotation;
        }

        // 속도에 따라 animator에 Speed 변수 변경
        if (animator != null && animator.enabled)
        {
            animator.SetFloat("velocityX", animVelocity.x);
            animator.SetFloat("velocityZ", animVelocity.y);
        }

        if((combat.canMoveDuringAttack || !combat.isAttacking) && controller.enabled == true)
        {
            controller.Move(move * (currentSpeed - airDrag) * Time.deltaTime);
        }

        //점프
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHight * 2f * gravity);
        }

        velocity.y -= gravity * Time.deltaTime;
        if(controller.enabled == true)
            controller.Move(velocity *Time.deltaTime);

        if(x != 0 || z != 0)
        {
            IsMoveing(true);
        }
        else
        {
            IsMoveing(false);
        }

        //Vector3 finalMove = move * (currentSpeed - airDrag);
        //CmdMove(finalMove, velocity, playerCharacter.transform.rotation, animVelocity);
    }

    [Command]
    void IsMoveing(bool move)
    {
        player.isMove = move;
    }

    [Command]
    void CmdMove(Vector3 move, Vector3 vel, Quaternion rot, Vector2 animvel)
    {
        controller.Move(move * Time.deltaTime);
        velocity = vel;
        controller.Move(velocity * Time.deltaTime);

        playerCharacter.transform.rotation = rot;
        
        animator.SetFloat("velocityX", animVelocity.x);
        animator.SetFloat("velocityZ", animVelocity.y);
    }

    //즉시 회전하는 함수
    [ClientRpc]
    public void RotateToAttack(Vector3 move)
    {
        //if(move.magnitude > 0.01f && !isAiming)
        {
            // Unity는 언리얼처럼 SafeNormal 함수는 없고, normalized 프로퍼티가 자동으로 정규화된 벡터를 리턴해줌.
            // 만약 move.magnitude가 아주 작으면(거의 0이면) LookRotation에서 에러가 날 수 있으니,
            // 아래처럼 0 벡터 체크 후 사용하는 것이 언리얼의 SafeNormal과 비슷한 방식임.
            Quaternion targetRotation;
            if (move.sqrMagnitude > 0.0001f)
                targetRotation = Quaternion.LookRotation(move.normalized);
            else
                targetRotation = playerCharacter.transform.rotation;
                
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            playerCharacter.transform.rotation = targetRotation;
        }
    }
        //앞으로 감
    [ClientRpc]
    public void StepToAttack(Vector3 move)
    {
        if(controller != null && controller.enabled)
            StartCoroutine(StepRoutine(move, 0.3f));
    }
    IEnumerator StepRoutine(Vector3 move, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = move;
        Vector3 lastPos = startPos;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // 이동 곡선 적용 (선택하신 Ease-Out 공식)
            float curveT = Mathf.Sin(t * Mathf.PI * 0.5f); 

            // 1. 현재 프레임에서 도달해야 할 '절대 좌표' 계산
            Vector3 currentTargetPos = Vector3.Lerp(startPos, targetPos, curveT);

            // 2. '이번 프레임에 움직여야 할 거리(변위)' 계산
            Vector3 delta = currentTargetPos - lastPos;

            // 3. 변위만큼 이동 시키기
            controller.Move(delta);

            // 4. 현재 위치를 다음 프레임의 lastPos로 업데이트
            lastPos = currentTargetPos;

            yield return null;
        }
    }

    

    void RotateBodyAiming()
    {        
        if(!isAiming) return;

        float camYaw = playerCamera.transform.eulerAngles.y;
        float bodyYaw = playerCharacter.transform.eulerAngles.y;
        
        float angleDiff  = Mathf.DeltaAngle(bodyYaw, camYaw);
        float thresHold = 30f;

        if(Mathf.Abs(angleDiff) > thresHold)
        {
            float targetYaw = Mathf.MoveTowardsAngle(bodyYaw, camYaw, Time.deltaTime * 360f);
            playerCharacter.transform.rotation = Quaternion.Euler(0, targetYaw, 0);
        }
    }

    void HandleUpperBodyRotation()
    {
        if(aimRig == null) return;

        aimRig.constraintActive = isAiming;

        targetRigweight = isAiming ?  0.9f : 0f;
        aimRig.weight = Mathf.Lerp(aimRig.weight, targetRigweight, rifTransitionSpeed * Time.deltaTime);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        brain.enabled = false;
        brain.enabled = true;
    }
}
