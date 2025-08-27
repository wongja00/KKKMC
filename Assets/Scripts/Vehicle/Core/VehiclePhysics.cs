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
        
        // 휠 콜라이더 상태 확인
        CheckWheelColliderSetup();
    }
    
    private void CheckWheelColliderSetup()
    {
        Debug.Log("=== 휠 콜라이더 설정 상태 ===");
        Debug.Log($"FrontLeft: {(frontLeftWheelCollider != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"FrontRight: {(frontRightWheelCollider != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"RearLeft: {(rearLeftWheelCollider != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"RearRight: {(rearRightWheelCollider != null ? "설정됨" : "설정되지 않음")}");
        
        Debug.Log("=== 휠 메시 설정 상태 ===");
        Debug.Log($"FrontLeftMesh: {(frontLeftWheelTransform != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"FrontRightMesh: {(frontRightWheelTransform != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"RearLeftMesh: {(rearLeftWheelTransform != null ? "설정됨" : "설정되지 않음")}");
        Debug.Log($"RearRightMesh: {(rearRightWheelTransform != null ? "설정됨" : "설정되지 않음")}");
        
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null || 
            rearLeftWheelCollider == null || rearRightWheelCollider == null)
        {
            Debug.LogError("휠 콜라이더가 완전히 설정되지 않았습니다! 차량이 움직이지 않을 수 있습니다.");
        }
    }
    
    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }
    
    private void HandleMotor()
    {
        // 휠 콜라이더 null 체크
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null || 
            rearLeftWheelCollider == null || rearRightWheelCollider == null)
        {
            Debug.LogWarning("휠 콜라이더가 설정되지 않았습니다!");
            return;
        }
        
        // 속도에 따른 토크 조정
        float currentSpeed = vehicleRigidbody.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float torqueMultiplier = torqueCurve.Evaluate(speedRatio);
        
        if(currentSpeed < 1.5f)
        {
            torqueMultiplier = 3f;
        }

        float adjustedMotorTorque = currentMotorTorque * torqueMultiplier;
        
        // 전륜 구동이니까 앞바퀴만 - 그냥 전륜으로 가자 시벌거
        frontLeftWheelCollider.motorTorque = adjustedMotorTorque;
        frontRightWheelCollider.motorTorque = adjustedMotorTorque;
        rearLeftWheelCollider.motorTorque = adjustedMotorTorque;
        rearRightWheelCollider.motorTorque = adjustedMotorTorque;
        
        // 브레이크 적용 4바퀴 모두 적용
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }
    
    private void HandleSteering()
    {
        // 휠 콜라이더 null 체크
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null)
        {
            Debug.LogWarning("전륜 콜라이더가 설정되지 않았습니다!");
            return;
        }
        
        FlipCar();

        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }
    
    private void UpdateWheels()
    {
        // 휠 콜라이더와 메시 null 체크
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null || 
            rearLeftWheelCollider == null || rearRightWheelCollider == null)
        {
            return;
        }
        
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

    //차 뒤집혔을때 좌우 조작으로 z축 회전 방향
    private void FlipCar()
    {
        if(!frontLeftWheelCollider.isGrounded && !frontRightWheelCollider.isGrounded && Vector3.Dot(transform.up, Vector3.up) < 0.3f)
        {
            //좌우 조작으로 z축 회전 방향
            if(Input.GetKey(KeyCode.A))
            {
                vehicleRigidbody.AddTorque(Vector3.forward * 50000f);
            }
            else if(Input.GetKey(KeyCode.D))
            {
                vehicleRigidbody.AddTorque(Vector3.forward * -50000f);
            }
        }
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
        return vehicleRigidbody != null ? vehicleRigidbody.linearVelocity.magnitude : 0f;
    }
    
    public float GetMaxSpeed()
    {
        return maxSpeed;
    }
}
