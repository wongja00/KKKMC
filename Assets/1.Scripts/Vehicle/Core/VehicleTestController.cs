using UnityEngine;

public class VehicleTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool enableCameraFollow = true;
    [SerializeField] private float cameraFollowDistance = 10f;
    [SerializeField] private float cameraFollowHeight = 5f;
    
    [Header("테스트 차량")]
    [SerializeField] private GameObject testVehicle;
    [SerializeField] private Transform spawnPoint;
    
    private bool isRide = false;

    private Camera mainCamera;
    private MobileFortress currentVehicle;
    
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (testVehicle == null)
        {
            CreateTestVehicle();
        }
        else
        {
            currentVehicle = testVehicle.GetComponent<MobileFortress>();
        }
        
        SetupCamera();
    }
    
    private void Update()
    {
        if(isRide)
        {
            HandleInput();
            UpdateCamera();
        }
    }
    
    private void HandleInput()
    {
        // R키: 차량 리셋
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetVehicle();
        }
        
        // T키: 차량 생성
        if (Input.GetKeyDown(KeyCode.T))
        {
            CreateTestVehicle();
        }
        
        // C키: 카메라 모드 전환
        if (Input.GetKeyDown(KeyCode.C))
        {
            enableCameraFollow = !enableCameraFollow;
        }
        
        // H키: 차량 수리
        if (Input.GetKeyDown(KeyCode.H) && currentVehicle != null)
        {
            currentVehicle.Repair(100f);
        }
        
        // 숫자키 1-4: 데미지 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (currentVehicle != null) currentVehicle.TakeDamage(10f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (currentVehicle != null) currentVehicle.TakeDamage(25f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (currentVehicle != null) currentVehicle.TakeDamage(50f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (currentVehicle != null) currentVehicle.TakeDamage(100f);
        }
    }
    
    private void CreateTestVehicle()
    {
        // 기존 차량 제거
        if (testVehicle != null)
        {
            DestroyImmediate(testVehicle);
        }
        
        // 스폰 포인트 설정
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.up * 2f;
        
        // 테스트 차량 생성
        testVehicle = new GameObject("TestVehicle");
        testVehicle.transform.position = spawnPosition;
        
        // 차량 설정 헬퍼 추가
        VehicleSetupHelper setupHelper = testVehicle.AddComponent<VehicleSetupHelper>();
        
        // 차량 설정 실행
        setupHelper.SetupVehicle();
        
        // 휠 참조 설정
        setupHelper.SetupWheelReferences();
        
        // 현재 차량 참조 업데이트
        currentVehicle = testVehicle.GetComponent<MobileFortress>();
        
        Debug.Log("테스트 차량이 생성되었습니다!");
    }
    
    private void ResetVehicle()
    {
        if (currentVehicle != null)
        {
            // 차량 위치 리셋
            Vector3 resetPosition = spawnPoint != null ? spawnPoint.position : Vector3.up * 2f;
            testVehicle.transform.position = resetPosition;
            testVehicle.transform.rotation = Quaternion.identity;
            
            // 차량 수리
            currentVehicle.Repair(100f);
            
            // 물리 리셋
            Rigidbody rb = testVehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log("차량이 리셋되었습니다!");
        }
    }
    
    private void SetupCamera()
    {
        if (mainCamera == null) return;
        
        // 카메라를 차량 뒤쪽으로 이동
        if (testVehicle != null)
        {
            Vector3 cameraPosition = testVehicle.transform.position - testVehicle.transform.forward * cameraFollowDistance + Vector3.up * cameraFollowHeight;
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(testVehicle.transform);
        }
    }
    
    private void UpdateCamera()
    {
        if (!enableCameraFollow || mainCamera == null || testVehicle == null) return;
        
        // 차량을 따라가는 카메라
        Vector3 targetPosition = testVehicle.transform.position - testVehicle.transform.forward * cameraFollowDistance + Vector3.up * cameraFollowHeight;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 2f);
        mainCamera.transform.LookAt(testVehicle.transform);
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== 차량 테스트 컨트롤러 ===");
        GUILayout.Space(10);
        
        GUILayout.Label("=== 컨트롤 ===");
        GUILayout.Label("WASD: 차량 이동");
        GUILayout.Label("Space: 브레이크");
        GUILayout.Label("H: 경적");
        GUILayout.Space(5);
        GUILayout.Label("R: 차량 리셋");
        GUILayout.Label("T: 새 차량 생성");
        GUILayout.Label("C: 카메라 모드 전환");
        GUILayout.Label("1-4: 데미지 테스트");
        
        GUILayout.Space(10);
        GUILayout.Label("=== 차량 상태 ===");
        if (currentVehicle != null)
        {
            GUILayout.Label($"체력: {currentVehicle.GetCurrentHealth():F1}/{currentVehicle.GetMaxHealth():F1}");
            GUILayout.Label($"속도: {currentVehicle.GetCurrentSpeed():F1} m/s");
            GUILayout.Label($"엔진: {(currentVehicle.IsEngineRunning() ? "가동" : "정지")}");
            GUILayout.Label($"플레이어 제어: {(currentVehicle.IsPlayerControlling() ? "예" : "아니오")}");
        }
        else
        {
            GUILayout.Label("차량이 없습니다!");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("=== 카메라 설정 ===");
        GUILayout.Label($"카메라 추적: {(enableCameraFollow ? "활성화" : "비활성화")}");
        
        GUILayout.EndArea();
    }
}
