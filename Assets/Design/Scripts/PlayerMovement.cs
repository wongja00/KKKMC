using System.Timers;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class PlayerMovement : MonoBehaviour
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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
    }
}
