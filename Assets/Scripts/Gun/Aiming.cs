using UnityEngine;
using Unity.Cinemachine;

public class Aiming : MonoBehaviour
{
    [SerializeField] private Transform aimTarget;
    [SerializeField] private CinemachineCamera cameraTransform;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private Animator animator;
    [SerializeField] HandHeld handHeld;

    public bool isAiming {get; private set;}
    public bool isADS;

    [SerializeField] private Camera mainCamera;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(aimKey))
        {
            aimTarget.position = cameraTransform.transform.position + cameraTransform.transform.forward * 20;

            handHeld.GunAiming(aimTarget);

            isAiming = true;

            if(InteractUIManager.Instance != null && mainCamera != null)
            {
                InteractUIManager.Instance.SetAimCrosshairWorldPosition(aimTarget.position);
            }
        }
        else
        {
            if(isAiming)
                handHeld.SetOriginAim();
                
            isAiming = false;
        }



        float targetFOV = isAiming ? 40f : 60f;
        cameraTransform.Lens.FieldOfView = Mathf.Lerp(cameraTransform.Lens.FieldOfView, targetFOV, Time.deltaTime * 10);

        animator.SetBool("isAiming",isAiming);
    }
}
