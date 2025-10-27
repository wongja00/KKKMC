using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//플레이어가 차에 타서 조작하기 위한 클래스
//자동차 상호작용 클래스
public class VehicleDriveController : NetworkBehaviour
{
    private List<MobileFortress> mobileFortressList = new List<MobileFortress>();
    private MobileFortress targetMobileFortress;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private VehicleNetworkSync vehicleNetworkSync;

    [Header("카메라 설정")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MouseLook playerMouseLook;
    [SerializeField] private GameObject cameraArmParent;
    [SerializeField] private float cameraFollowDistance = 10f;
    [SerializeField] private float cameraFollowHeight = 5f;

    [SerializeField] private float cameraRotationSpeed = 1f;
    
    //조작 카메라
    [SerializeField] private GameObject driverCamera;

    [SerializeField] private CharacterController playerController;

    [Header("자동차 탐지 설정")]
    //자동차 탐지 거리
    [SerializeField] private float vehicleDetectionDistance = 3f;

    //자동차 탐지 콜라이더
    [SerializeField] private Collider vehicleDetectionCollider;

    //타거나 내릴때 입력키
    [SerializeField] private KeyCode enterVehicleKey = KeyCode.F;

    //차 가까지 갔을떄 뜨는 UI
    [SerializeField] private GameObject DriveUI;

    //현재 탑승중인지 여부
    [SerializeField]
    [SyncVar] private bool isDriverInside = false;

    [SerializeField] private LayerMask obstacleLayerMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isDriverInside = false;
        
        
        if(playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }

        if(playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if(vehicleDetectionCollider == null)
        {
            vehicleDetectionCollider = GetComponent<Collider>();
        }

        if(playerController == null)
        {
            playerController = GetComponentInParent<CharacterController>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(isDriverInside == false)
        {
            OnCloseToVehicle();
        }
        else
        {
            if(Input.GetKeyDown(enterVehicleKey))
            {
                targetMobileFortress.GetVehicleSeatSystem().ExitSeat(transform);

                OnDriverExit();
            }

            RotateCamera();
        }
    }

    //자동차 가까이 가면 실행
    void OnCloseToVehicle()
    {  
        if(isDriverInside) return;
        
        targetMobileFortress = GetClosestMobileFortress();
        if(targetMobileFortress == null) return;

        Vector3 directionToVehicle = (targetMobileFortress.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(driverCamera.transform.forward, directionToVehicle);

        if(dotProduct > 0.5f)
        {
            if(IsVehicleVisible(targetMobileFortress))
            {
            EnableDriveUI();
            if(Input.GetKeyDown(enterVehicleKey))
            {
                Debug.Log("Enter Driver Seat");

                OnDriverEnter();

                
                if(playerController.enabled == true)
                {
                    playerController.enabled = false;

                    Debug.Log("Player Controller Disabled");
                }

                targetMobileFortress.GetVehicleSeatSystem().EnterSeat(ESeatType.Driver, transform);
            }
            }
            else
            {
                DisableDriveUI();
            }
        }
        else
        {
            DisableDriveUI();
        }
        
    }

    //차에 탔을떄
    public void OnDriverEnter()
    {
        if(isDriverInside || targetMobileFortress == null) return;

        if(vehicleNetworkSync != null)
        {
            vehicleNetworkSync.CmdEnterVehicle(NetworkClient.connection.identity);
        }
        else
        {
            OnDriverEnterLocal();
        }
        
    }

    private void OnDriverEnterLocal()
    {        
        targetMobileFortress.SetPlayerControl(true);
        isDriverInside = true;

        playerController.enabled = false;
        playerMovement.enabled = false;

        if(playerMouseLook != null)
        {
            playerMouseLook.enabled = false;
        }
        DisableDriveUI();
        SetFollowVehicleCameraPosition();

    }

    //차에서 내릴떄
    public void OnDriverExit()
    {
        if(!isDriverInside || targetMobileFortress == null) return;

        if(vehicleNetworkSync !=null)
        {
            vehicleNetworkSync.CmdExitVehicle();
        }
        else
        {
            OnDriverExitLocal();
        }

    }

    public void OnDriverExitLocal()
    {
        targetMobileFortress.SetPlayerControl(false);
        isDriverInside = false;

        playerMovement.enabled = true;
        playerController.enabled = true;
        
        if(playerMouseLook != null)
        {
            playerMouseLook.enabled = true;
        }

        //카메라 원래대로 변경
        SetCameraOrigin();
    }

    public bool IsDriverInside()
    {
        return isDriverInside;
    }

    private void EnableDriveUI()
    {
        if(DriveUI != null)
        {
            DriveUI.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if(isDriverInside) return;


        if(other.CompareTag("Vehicle") && !mobileFortressList.Contains(other.GetComponent<MobileFortress>()))
        {

            MobileFortress mobileFortress = other.GetComponent<MobileFortress>();

            if(mobileFortress == null) return;

            mobileFortressList.Add(mobileFortress);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Vehicle") && mobileFortressList.Contains(other.GetComponent<MobileFortress>()))
        {
            MobileFortress mobileFortress = other.GetComponent<MobileFortress>();

            if(mobileFortress == null) return;

            mobileFortressList.Remove(other.GetComponent<MobileFortress>());

            if(targetMobileFortress == mobileFortress)
                targetMobileFortress = null;

            DisableDriveUI();
        }
    }

    private void DisableDriveUI()
    {
        if(DriveUI != null)
        {
            DriveUI.SetActive(false);
        }
    }

    private MobileFortress GetClosestMobileFortress()
    {
        MobileFortress closestMobileFortress = null;
        
        //null 체크
        mobileFortressList.RemoveAll(x => x == null);

        float closestDistance = float.MaxValue;

        foreach(MobileFortress mobileFortress in mobileFortressList)
        {
            float distance = Vector3.Distance(transform.position, mobileFortress.transform.position);

            if(distance < closestDistance)
            {
                closestMobileFortress = mobileFortress;
                closestDistance = distance;
            }
        }

        return closestMobileFortress;
    }


    private bool IsVehicleVisible(MobileFortress mobileFortress)
    {
        Vector3 direction = (mobileFortress.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(driverCamera.transform.position, mobileFortress.transform.position);
    
        if(Physics.Raycast(driverCamera.transform.position, direction, out RaycastHit hit, distance, obstacleLayerMask))
        {
            return false; 
        }
        return true;
    }

    //자동차에 맞춰 카메라가 자연스럽게 따라오는 함수
    private void SetFollowVehicleCameraPosition()
    {
        if(playerCamera == null) return;

        if(targetMobileFortress == null) return;

        if(cameraArmParent == null)
        {
            cameraArmParent = new GameObject("CameraArmParent");

            cameraArmParent.transform.SetParent(targetMobileFortress.transform);
            cameraArmParent.transform.localPosition = Vector3.zero;
            cameraArmParent.transform.localRotation = Quaternion.identity;
            cameraArmParent.transform.localScale = Vector3.one;

            playerCamera.transform.SetParent(cameraArmParent.transform);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
            playerCamera.transform.localScale = Vector3.one;
            
            Vector3 targetPosition = cameraArmParent.transform.localPosition + new Vector3(0, cameraFollowHeight, -cameraFollowDistance);
            playerCamera.transform.localPosition = targetPosition;
        }

    }

    private void RotateCamera()
    {
        if(cameraArmParent == null) return;

        //마우스 회전에 따라 cameraArmParent 회전
        cameraArmParent.transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * cameraRotationSpeed);
        //cameraArmParent.transform.Rotate(Vector3.right * Input.GetAxis("Mouse Y") * cameraRotationSpeed);
    }

    private void SetCameraOrigin()
    {
        if(playerCamera == null) return;

        playerCamera.transform.localPosition = Vector3.zero;

        if(cameraArmParent != null)
        {
            playerCamera.transform.SetParent(transform);
            playerCamera.transform.localPosition = Vector3.zero;
            playerCamera.transform.localRotation = Quaternion.identity;
           
            Destroy(cameraArmParent);
            cameraArmParent = null;
        }
    }
}
