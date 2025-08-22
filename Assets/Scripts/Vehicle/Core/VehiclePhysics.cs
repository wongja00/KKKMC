using UnityEngine;

public class VehiclePhysics : MonoBehaviour
{
    [Header("차량 설정")]
    [SerializeField] private float motorForce = 1500f;
    [SerializeField] private float brakeForce = 3000f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float centerOfMassHeight = -0.5f;
    
    [Header("휠 콜라이더")]
    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;
    
    [Header("휠 메시")]
    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;
    
    [Header("엔진 설정")]
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private AnimationCurve torqueCurve;
    [SerializeField] private float gearRatio = 3.5f;
    
    private Rigidbody vehicleRigidbody;
    private float currentSteerAngle;
    private float currentBrakeForce;
    private float currentMotorTorque;
    
    private void Start()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
        if (vehicleRigidbody != null)
        {
            // 무게중심을 낮춰서 차량이 뒤집히지 않도록 함
            vehicleRigidbody.centerOfMass = new Vector3(0, centerOfMassHeight, 0);
        }
        
        // 토크 커브 초기화 (기본값)
        if (torqueCurve.length == 0)
        {
            torqueCurve = new AnimationCurve();
            torqueCurve.AddKey(0, 1f);
            torqueCurve.AddKey(0.3f, 1f);
            torqueCurve.AddKey(0.7f, 0.8f);
            torqueCurve.AddKey(1f, 0.5f);
        }
    }
    
    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }
    
    private void GetInput()
    {
        // 입력은 VehicleInput에서 받아올 예정
        // 현재는 테스트용으로 직접 처리
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isBraking = Input.GetKey(KeyCode.Space);
        
        currentSteerAngle = maxSteerAngle * horizontalInput;
        currentMotorTorque = motorForce * verticalInput;
        currentBrakeForce = isBraking ? brakeForce : 0f;
    }
    
    private void HandleMotor()
    {
        // 속도에 따른 토크 조정
        float currentSpeed = vehicleRigidbody.velocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float torqueMultiplier = torqueCurve.Evaluate(speedRatio);
        
        float adjustedMotorTorque = currentMotorTorque * torqueMultiplier;
        
        // 전륜 구동
        frontLeftWheelCollider.motorTorque = adjustedMotorTorque;
        frontRightWheelCollider.motorTorque = adjustedMotorTorque;
        
        // 브레이크 적용
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }
    
    private void HandleSteering()
    {
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }
    
    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }
    
    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        if (wheelTransform == null) return;
        
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);
        
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void SetMotorTorque(float torque)
    {
        currentMotorTorque = Mathf.Clamp(torque, -motorForce, motorForce);
    }
    
    public void SetSteerAngle(float angle)
    {
        currentSteerAngle = Mathf.Clamp(angle, -maxSteerAngle, maxSteerAngle);
    }
    
    public void SetBrakeForce(float force)
    {
        currentBrakeForce = Mathf.Clamp(force, 0, brakeForce);
    }
    
    public float GetCurrentSpeed()
    {
        return vehicleRigidbody != null ? vehicleRigidbody.velocity.magnitude : 0f;
    }
    
    public float GetMaxSpeed()
    {
        return maxSpeed;
    }
}
