using UnityEngine;

public class VehicleInput : MonoBehaviour
{
    [Header("입력 설정")]
    [SerializeField] private bool useInputSystem = false;
    [SerializeField] private float inputSensitivity = 1f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("입력 매핑")]
    [SerializeField] private KeyCode accelerateKey = KeyCode.W;
    [SerializeField] private KeyCode backKey = KeyCode.S;
    [SerializeField] private KeyCode turnLeftKey = KeyCode.A;
    [SerializeField] private KeyCode turnRightKey = KeyCode.D;
    [SerializeField] private KeyCode handbrakeKey = KeyCode.Space;
    [SerializeField] private KeyCode hornKey = KeyCode.H;
    
    private VehiclePhysics vehiclePhysics;
    private VehicleAudio vehicleAudio;
    
    // 입력 값들
    private float throttleInput;
    private float brakeInput;
    private float steeringInput;
    private bool handbrakeInput;
    private bool hornInput;

    public float throttleValue = 1f;
    
    // 입력 이벤트
    public System.Action<float> OnThrottleChanged;
    public System.Action<float> OnBrakeChanged;
    public System.Action<float> OnSteeringChanged;
    public System.Action<bool> OnHandbrakeChanged;
    public System.Action OnHornPressed;
    
    private void Start()
    {
        vehiclePhysics = GetComponent<VehiclePhysics>();
        vehicleAudio = GetComponent<VehicleAudio>();
        
        if (vehiclePhysics == null)
        {
            Debug.LogWarning("VehiclePhysics 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    private void Update()
    {
        HandleInput();
        ApplyInput();
    }
    
    private void HandleInput()
    {
        // 키보드 입력 처리
        HandleKeyboardInput();
        
        // 입력 정규화 및 데드존 적용
        NormalizeInputs();
    }
    
    private void HandleKeyboardInput()
    {
        // 가속/감속
        if (Input.GetKey(accelerateKey))
        {
            throttleInput = throttleValue;
        }
        else if (Input.GetKey(backKey))
        {
            throttleInput = -throttleValue;
        }
        else
        {
            throttleInput = 0f;
            brakeInput = 0f;
        }
        
        // 조향
        if (Input.GetKey(turnLeftKey))
        {
            steeringInput = -1f;
        }
        else if (Input.GetKey(turnRightKey))
        {
            steeringInput = 1f;
        }
        else
        {
            steeringInput = 0f;
        }
        
        // 핸드브레이크 이건 눌렀는지 안눌렀지만 체크!
        handbrakeInput = Input.GetKey(handbrakeKey);
        
        // 경적
        if (Input.GetKeyDown(hornKey))
        {
            hornInput = true;
        }
        else
        {
            hornInput = false;
        }
    }
    
    private void NormalizeInputs()
    {
        // 데드존 적용 즉, 최소치 이하는 0으로 처리
        if (Mathf.Abs(throttleInput) < deadZone) throttleInput = 0f;
        if (Mathf.Abs(brakeInput) < deadZone) brakeInput = 0f;
        if (Mathf.Abs(steeringInput) < deadZone) steeringInput = 0f;
        
        // 감도 적용
        throttleInput *= inputSensitivity;
        brakeInput *= inputSensitivity;
        steeringInput *= inputSensitivity;
        
        // 값 제한
        throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
        brakeInput = Mathf.Clamp01(brakeInput);
        steeringInput = Mathf.Clamp(steeringInput, -1f, 1f);
    }
    
    private void ApplyInput()
    {
        if (vehiclePhysics != null)
        {
            // 엔진 토크 설정
            vehiclePhysics.SetMotorTorque(throttleInput * vehiclePhysics.GetMaxSpeed());
            
            // 브레이크 설정
            vehiclePhysics.SetBrakeForce(brakeInput * vehiclePhysics.GetMaxSpeed() * 1000f);
            
            // 조향 설정
            vehiclePhysics.SetSteerAngle(steeringInput * 30f);
        }
        
        // 입력 이벤트 발생
        OnThrottleChanged?.Invoke(throttleInput);
        OnBrakeChanged?.Invoke(brakeInput);
        OnSteeringChanged?.Invoke(steeringInput);
        OnHandbrakeChanged?.Invoke(handbrakeInput);
        
        if (hornInput)
        {
            OnHornPressed?.Invoke();
            if (vehicleAudio != null)
            {
                // 경적 소리 재생 (VehicleAudio에서 구현 예정)
            }
        }
    }
    
    // 외부에서 입력 값을 가져올 수 있는 메서드들
    public float GetThrottleInput() => throttleInput;
    public float GetBrakeInput() => brakeInput;
    public float GetSteeringInput() => steeringInput;
    public bool GetHandbrakeInput() => handbrakeInput;
    
    // 입력 활성화/비활성화
    public void SetInputEnabled(bool enabled)
    {
        enabled = enabled;
        if (!enabled)
        {
            // 입력을 0으로 리셋
            throttleInput = 0f;
            brakeInput = 0f;
            steeringInput = 0f;
            handbrakeInput = false;
        }
    }
    
    // 입력 감도 조정
    public void SetInputSensitivity(float sensitivity)
    {
        inputSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3f);
    }
}
