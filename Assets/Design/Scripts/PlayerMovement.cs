using System.Timers;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder.Shapes;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using Unity.Cinemachine;
using System.Collections;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController controller;

    [SerializeField]
    private CinemachineCamera playerCamera;

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private KeyCode dashKey = KeyCode.LeftShift;

    public float speed = 12f;
    public float originSpeed = 12f;
    public float maxSpeed = 24f;
    public float gravity = 9.81f;
    public float jumpHight = 3.0f;
    public float customAirDrag = 4.0f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;

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
    }

    // Update is called once per frame
    void Update()
    {
        if(isLocalPlayer == false) return;

        HandleMovement();
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

        float currentSpeed = originSpeed;
        if(Input.GetKey(dashKey))
        {
            currentSpeed = originSpeed * 2;
        }


        Vector3 move = transform.right * x + transform.forward * z;

        // 속도에 따라 animator에 Speed 변수 변경
        if (animator != null)
        {
            float animSpeed = move.magnitude * (currentSpeed - airDrag);

            animator.SetFloat("Speed", animSpeed / maxSpeed);
        }

        controller.Move(move * (currentSpeed - airDrag) * Time.deltaTime);

        if(Input.GetButton("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHight * 2f * gravity);
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity *Time.deltaTime);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        brain.enabled = false;
        brain.enabled = true;
    }
}
