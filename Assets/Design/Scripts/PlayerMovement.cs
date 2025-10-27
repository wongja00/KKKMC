using System.Timers;
using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder.Shapes;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController controller;

    public float speed = 12f;
    public float gravity = 9.81f;
    public float jumpHight = 3.0f;
    public float customAirDrag = 4.0f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    [SyncVar] private Vector3 networkPosition;
    [SyncVar] private Quaternion networkRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer)
        {
            controller.enabled = false;
        }
        
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

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * (speed-airDrag) * Time.deltaTime);

        if(Input.GetButton("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHight * -2f * -gravity);
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity *Time.deltaTime);

        CmdUpdatePosition(transform.position, transform.rotation);
    }

        [Command]
        void CmdUpdatePosition(Vector3 pos, Quaternion rot)
        {
            networkPosition = pos;
            networkRotation = rot;
        }

        void LateUpdate()
        {
            if(!isLocalPlayer)
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 15f);
            }
        }
}
