using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Animations.Rigging;
using Mirror;

public class Aiming : NetworkBehaviour
{
    [SerializeField] public Transform aimTarget;
    [SerializeField] private CinemachineCamera cameraTransform;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private Animator animator;
    [SerializeField] HandHeld handHeld;
    [SerializeField] Rig handRig;
    public bool isAiming {get; private set;}
    public bool isADS;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask aimLayerMask;
    public GunBase curGun;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;

        mainCamera = Camera.main;
        cameraTransform = CameraManager.Instance.GetPlayerCemera();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;
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

        if(isADS || isAiming)
        {
            aimTarget.position = aimPoint;
            handRig.weight = 1.0f;
            //handHeld.GunAiming(aimTarget);

            if(InteractUIManager.Instance != null && mainCamera != null)
            {
                InteractUIManager.Instance.SetAimCrosshairWorldPosition(aimTarget.position);
            }
        }
        else
        {            
            handRig.weight = 0.0f;
        }

        if(Input.GetKey(aimKey) && handHeld.curGun != null)
        {
            isAiming = true;  
        }
        else
        {
            isAiming = false;
        }
    
         if(!isAiming && !isADS && handHeld.curGun != null)
         {
             //handHeld.SetOriginAim();
             InteractUIManager.Instance.SetAimCrosshair(false);
         }

        float targetFOV = isAiming ? 40f : 60f;
        cameraTransform.Lens.FieldOfView = Mathf.Lerp(cameraTransform.Lens.FieldOfView, targetFOV, Time.deltaTime * 10);

        animator.SetBool("isAiming",isAiming || isADS);
    }
}
