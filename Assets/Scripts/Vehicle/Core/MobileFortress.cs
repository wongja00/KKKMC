using UnityEngine;

public class MobileFortress : MonoBehaviour
{
    [Header("차량 컴포넌트")]
    [SerializeField] private VehiclePhysics vehiclePhysics;
    [SerializeField] private VehicleInput vehicleInput;
    [SerializeField] private VehicleAudio vehicleAudio;
    
    [Header("차량 상태")]
    [SerializeField] private bool isEngineRunning = false;
    [SerializeField] private bool isPlayerControlling = true;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxHealth = 100f;
    
    [Header("시각적 효과")]
    [SerializeField] private ParticleSystem engineSmoke;
    [SerializeField] private Light[] vehicleLights;
    [SerializeField] private Transform[] wheelMeshes;
    
    [Header("UI 참조")]
    [SerializeField] private GameObject speedometerUI;
    [SerializeField] private GameObject healthUI;
    
    // 이벤트
    public System.Action<float> OnHealthChanged;
    public System.Action<bool> OnEngineStateChanged;
    public System.Action<float> OnSpeedChanged;
    
    private Rigidbody vehicleRigidbody;
    private float lastSpeed;
    
    private void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        StartEngine();
    }
    
    private void Update()
    {
        UpdateVehicleStatus();
        HandleVisualEffects();
    }
    
    private void InitializeComponents()
    {
        // 컴포넌트들이 없으면 자동으로 찾기
        if (vehiclePhysics == null)
            vehiclePhysics = GetComponent<VehiclePhysics>();
        
        if (vehicleInput == null)
            vehicleInput = GetComponent<VehicleInput>();
        
        if (vehicleAudio == null)
            vehicleAudio = GetComponent<VehicleAudio>();
        
        if (vehicleRigidbody == null)
            vehicleRigidbody = GetComponent<Rigidbody>();
        
        // 필수 컴포넌트 체크
        if (vehiclePhysics == null)
        {
            Debug.LogError("VehiclePhysics 컴포넌트가 필요합니다!");
            enabled = false;
            return;
        }
    }
    
    private void SetupEventListeners()
    {
        // 입력 이벤트 구독
        if (vehicleInput != null)
        {
            vehicleInput.OnThrottleChanged += OnThrottleInputChanged;
            vehicleInput.OnBrakeChanged += OnBrakeInputChanged;
            vehicleInput.OnSteeringChanged += OnSteeringInputChanged;
        }
    }
    
    private void UpdateVehicleStatus()
    {
        if (vehiclePhysics == null) return;
        
        // 속도 업데이트
        float currentSpeed = vehiclePhysics.GetCurrentSpeed();
        if (Mathf.Abs(currentSpeed - lastSpeed) > 0.1f)
        {
            OnSpeedChanged?.Invoke(currentSpeed);
            lastSpeed = currentSpeed;
        }
        
        // 엔진 상태 업데이트
        bool shouldEngineRun = currentSpeed > 0.1f || (vehicleInput != null && vehicleInput.GetThrottleInput() > 0.1f);
        if (shouldEngineRun != isEngineRunning)
        {
            SetEngineState(shouldEngineRun);
        }
    }
    
    private void HandleVisualEffects()
    {
        // 엔진 연기 효과
        if (engineSmoke != null)
        {
            if (isEngineRunning && !engineSmoke.isPlaying)
            {
                engineSmoke.Play();
            }
            else if (!isEngineRunning && engineSmoke.isPlaying)
            {
                engineSmoke.Stop();
            }
        }
        
        // 차량 조명
        if (vehicleLights != null)
        {
            foreach (Light light in vehicleLights)
            {
                if (light != null)
                {
                    light.enabled = isEngineRunning;
                }
            }
        }
        
        // 휠 회전 효과
        if (vehiclePhysics != null && wheelMeshes != null)
        {
            float currentSpeed = vehiclePhysics.GetCurrentSpeed();
            float wheelRotationSpeed = currentSpeed * 10f; // 속도에 따른 회전 속도
            
            foreach (Transform wheelMesh in wheelMeshes)
            {
                if (wheelMesh != null)
                {
                    wheelMesh.Rotate(wheelRotationSpeed * Time.deltaTime, 0, 0);
                }
            }
        }
    }
    
    private void StartEngine()
    {
        SetEngineState(true);
    }
    
    private void StopEngine()
    {
        SetEngineState(false);
    }
    
    private void SetEngineState(bool running)
    {
        isEngineRunning = running;
        OnEngineStateChanged?.Invoke(isEngineRunning);
        
        // 오디오 시스템에 엔진 상태 전달
        if (vehicleAudio != null)
        {
            if (!running)
            {
                vehicleAudio.StopAllSounds();
            }
            else
            {
                vehicleAudio.ResumeAllSounds();
            }
        }
    }
    
    // 입력 이벤트 핸들러들
    private void OnThrottleInputChanged(float throttleInput)
    {
        // 가속 입력에 따른 추가 로직
        if (throttleInput > 0.1f && !isEngineRunning)
        {
            StartEngine();
        }
    }
    
    private void OnBrakeInputChanged(float brakeInput)
    {
        // 브레이크 입력에 따른 추가 로직
    }
    
    private void OnSteeringInputChanged(float steeringInput)
    {
        // 조향 입력에 따른 추가 로직
    }
    
    // 차량 제어 메서드들
    public void SetPlayerControl(bool enabled)
    {
        isPlayerControlling = enabled;
        
        if (vehicleInput != null)
        {
            vehicleInput.SetInputEnabled(enabled);
        }
        
        if (!enabled)
        {
            // AI 제어로 전환 시 모든 입력을 0으로
            if (vehiclePhysics != null)
            {
                vehiclePhysics.SetMotorTorque(0f);
                vehiclePhysics.SetSteerAngle(0f);
                vehiclePhysics.SetBrakeForce(0f);
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0f)
        {
            DestroyVehicle();
        }
    }
    
    public void Repair(float repairAmount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + repairAmount);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    private void DestroyVehicle()
    {
        // 차량 파괴 로직
        Debug.Log("차량이 파괴되었습니다!");
        
        // 모든 사운드 정지
        if (vehicleAudio != null)
        {
            vehicleAudio.StopAllSounds();
        }
        
        // 시각적 효과
        if (engineSmoke != null)
        {
            engineSmoke.Stop();
        }
        
        // 차량 비활성화
        SetPlayerControl(false);
        enabled = false;
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentSpeed() => vehiclePhysics != null ? vehiclePhysics.GetCurrentSpeed() : 0f;
    public bool IsEngineRunning() => isEngineRunning;
    public bool IsPlayerControlling() => isPlayerControlling;
    
    // 디버그 정보
    private void OnGUI()
    {
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Mobile Fortress Status");
            GUILayout.Label($"Health: {currentHealth:F1}/{maxHealth:F1}");
            GUILayout.Label($"Speed: {GetCurrentSpeed():F1} m/s");
            GUILayout.Label($"Engine: {(isEngineRunning ? "Running" : "Stopped")}");
            GUILayout.Label($"Player Control: {(isPlayerControlling ? "Yes" : "No")}");
            GUILayout.EndArea();
        }
    }
}
