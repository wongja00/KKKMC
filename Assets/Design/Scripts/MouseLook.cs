using UnityEngine;
using Unity.Cinemachine;
using Mirror;

public class MouseLook : NetworkBehaviour
{
    float mouseSensitivity = 100f;
    public float customMouseSensitivity = 1.0f;

    public Transform playerBody;

    float xRotation = 0f;
    float yRotation = 0f;
    
    [SerializeField]
    private CinemachineCamera playerCamera;

    [SerializeField]
    private Transform cameraTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;

        Cursor.lockState = CursorLockMode.Locked;
        playerCamera = CameraManager.Instance.GetPlayerCemera();
        playerCamera.Follow = cameraTarget;
        playerCamera.LookAt = cameraTarget;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!isLocalPlayer) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY * customMouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX * customMouseSensitivity;

        cameraTarget.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
