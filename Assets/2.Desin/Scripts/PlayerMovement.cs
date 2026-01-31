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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer)
        {
            controller.enabled = false;
        }
        originSpeed = speed;
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
            Debug.Log("이동");

            Quaternion targetRotation = Quaternion.LookRotation(move);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            //playerCharacter.transform.rotation = Quaternion.Slerp(playerCharacter.transform.rotation, targetRotation, Time.deltaTime * 10);
            playerCharacter.transform.rotation = targetRotation;
        }

        // 속도에 따라 animator에 Speed 변수 변경
        if (animator != null)
        {
            animator.SetFloat("velocityX", animVelocity.x);
            animator.SetFloat("velocityZ", animVelocity.y);
        }

        if(combat.canMoveDuringAttack || !combat.isAttacking)
        {
            controller.Move(move * (currentSpeed - airDrag) * Time.deltaTime);

        }

        //점프
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHight * 2f * gravity);
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity *Time.deltaTime);

        Vector3 finalMove = move * (currentSpeed - airDrag);
        //CmdMove(finalMove, velocity, playerCharacter.transform.rotation, animVelocity);
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
    void RotateToAttack(Vector3 move)
    {
        if(move.magnitude > 0.01f && !isAiming)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            playerCharacter.transform.rotation = targetRotation;
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
