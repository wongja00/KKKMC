using UnityEngine;
using Unity.Cinemachine;

public class Aiming : MonoBehaviour
{
    [SerializeField] public Transform aimTarget;
    [SerializeField] private CinemachineCamera cameraTransform;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private Animator animator;
    [SerializeField] HandHeld handHeld;

    public bool isAiming {get; private set;}
    public bool isADS;

    [SerializeField] private Camera mainCamera;

    [SerializeField] private LayerMask aimLayerMask;

    public GunBase curGun;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        AimingGun();
    }

    public void AimingGun()
    {
        Ray camRay = mainCamera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));

        RaycastHit hit;
        Vector3 aimPoint;

        if (Physics.Raycast(camRay, out hit, 1000f, aimLayerMask))
        {
            aimPoint = hit.point;
        }
        else
        {
            aimPoint = camRay.origin + camRay.direction * 1000f;
        }

        Debug.DrawLine(mainCamera.transform.position, aimPoint);
        

        if(isADS || isAiming)
        {
            aimTarget.position = aimPoint;

            handHeld.GunAiming(aimTarget);

            if(InteractUIManager.Instance != null && mainCamera != null)
            {
                InteractUIManager.Instance.SetAimCrosshairWorldPosition(aimTarget.position);
            }
        }

        if(Input.GetKey(aimKey))
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }

    
            if(!isAiming && !isADS)
            {
                handHeld.SetOriginAim();
                InteractUIManager.Instance.SetAimCrosshair(false);
            }
                


        float targetFOV = isAiming ? 40f : 60f;
        cameraTransform.Lens.FieldOfView = Mathf.Lerp(cameraTransform.Lens.FieldOfView, targetFOV, Time.deltaTime * 10);

        animator.SetBool("isAiming",isAiming);
    }
}
