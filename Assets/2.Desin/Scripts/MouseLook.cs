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

    [SerializeField] private KeyCode menuKey = KeyCode.Tab;
    
    bool isMenu = false;

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
    void Update()
    {
        if(!isLocalPlayer) return;

        if(Input.GetKeyDown(menuKey))
        {
            isMenu = !isMenu;
            if(isMenu) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;
        }

        if(isMenu)
        {
            return;
        } 

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        mouseX *= Time.unscaledDeltaTime;
        mouseY *= Time.unscaledDeltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;


    }

    void LateUpdate()
    {
        if(!isLocalPlayer) return;

        cameraTarget.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
