using UnityEngine;

public class VehicleSetupHelper : MonoBehaviour
{
    [Header("자동 설정")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createWheelColliders = true;
    [SerializeField] private bool createWheelMeshes = true;
    
    [Header("휠 설정")]
    [SerializeField] private float wheelRadius = 0.5f;
    [SerializeField] private float wheelWidth = 0.3f;
    [SerializeField] private float wheelMass = 20f;
    [SerializeField] private float wheelSuspensionDistance = 0.3f;
    [SerializeField] private float wheelSuspensionSpring = 35000f;
    [SerializeField] private float wheelSuspensionDamper = 4500f;
    
    [Header("차량 설정")]
    [SerializeField] private float vehicleMass = 1500f;
    [SerializeField] private float drag = 0.1f;
    [SerializeField] private float angularDrag = 0.05f;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupVehicle();
        }
    }
    
    [ContextMenu("차량 자동 설정")]
    public void SetupVehicle()
    {
        SetupRigidbody();
        SetupWheelColliders();
        SetupWheelMeshes();
        SetupComponents();
        
        Debug.Log("차량 설정이 완료되었습니다!");
    }
    
    private void SetupRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = vehicleMass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
    }
    
    private void SetupWheelColliders()
    {
        if (!createWheelColliders) return;
        
        // 기존 휠 콜라이더 찾기
        WheelCollider[] existingWheelColliders = GetComponentsInChildren<WheelCollider>();
        
        if (existingWheelColliders.Length >= 4)
        {
            Debug.Log("휠 콜라이더가 이미 설정되어 있습니다.");
            return;
        }
        
        // 휠 콜라이더 생성
        CreateWheelCollider("FrontLeftWheel", new Vector3(-1.5f, 0, 2f));
        CreateWheelCollider("FrontRightWheel", new Vector3(1.5f, 0, 2f));
        CreateWheelCollider("RearLeftWheel", new Vector3(-1.5f, 0, -2f));
        CreateWheelCollider("RearRightWheel", new Vector3(1.5f, 0, -2f));
    }
    
    private void CreateWheelCollider(string wheelName, Vector3 localPosition)
    {
        GameObject wheelObject = new GameObject(wheelName);
        wheelObject.transform.SetParent(transform);
        wheelObject.transform.localPosition = localPosition;
        
        WheelCollider wheelCollider = wheelObject.AddComponent<WheelCollider>();
        
        // 휠 콜라이더 설정
        wheelCollider.radius = wheelRadius;
        wheelCollider.mass = wheelMass;
        wheelCollider.suspensionDistance = wheelSuspensionDistance;
        
        // 서스펜션 스프링 설정
        JointSpring suspensionSpring = wheelCollider.suspensionSpring;
        suspensionSpring.spring = wheelSuspensionSpring;
        suspensionSpring.damper = wheelSuspensionDamper;
        suspensionSpring.targetPosition = 0.5f;
        wheelCollider.suspensionSpring = suspensionSpring;
        
        // 휠 콜라이더 피팅 설정
        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        forwardFriction.extremumSlip = 2f;
        forwardFriction.extremumValue = 1f;
        forwardFriction.asymptoteSlip = 4f;
        forwardFriction.asymptoteValue = 0.5f;
        forwardFriction.stiffness = 1f;
        wheelCollider.forwardFriction = forwardFriction;
        
        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
        sidewaysFriction.extremumSlip = 2f;
        sidewaysFriction.extremumValue = 1f;
        sidewaysFriction.asymptoteSlip = 4f;
        sidewaysFriction.asymptoteValue = 0.5f;
        sidewaysFriction.stiffness = 1f;
        wheelCollider.sidewaysFriction = sidewaysFriction;
        
        Debug.Log($"{wheelName} 휠 콜라이더가 생성되었습니다.");
    }
    
    private void SetupWheelMeshes()
    {
        if (!createWheelMeshes) return;
        
        // 간단한 휠 메시 생성 (원통형)
        CreateWheelMesh("FrontLeftWheelMesh", new Vector3(-1.5f, 0, 2f));
        CreateWheelMesh("FrontRightWheelMesh", new Vector3(1.5f, 0, 2f));
        CreateWheelMesh("RearLeftWheelMesh", new Vector3(-1.5f, 0, -2f));
        CreateWheelMesh("RearRightWheelMesh", new Vector3(1.5f, 0, -2f));
    }
    
    private void CreateWheelMesh(string meshName, Vector3 localPosition)
    {
        GameObject wheelMeshObject = new GameObject(meshName);
        wheelMeshObject.transform.SetParent(transform);
        wheelMeshObject.transform.localPosition = localPosition;
        
        // 간단한 원통형 메시 생성
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(wheelMeshObject.transform);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localRotation = Quaternion.Euler(0, 0, 90); // Z축을 중심으로 회전
        cylinder.transform.localScale = new Vector3(wheelWidth, wheelRadius, wheelRadius);
        
        // 콜라이더 제거 (휠 콜라이더가 있으므로)
        DestroyImmediate(cylinder.GetComponent<Collider>());
        
        Debug.Log($"{meshName} 휠 메시가 생성되었습니다.");
    }
    
    private void SetupComponents()
    {
        // 필요한 컴포넌트들이 없으면 자동으로 추가
        if (GetComponent<VehiclePhysics>() == null)
        {
            gameObject.AddComponent<VehiclePhysics>();
            Debug.Log("VehiclePhysics 컴포넌트가 추가되었습니다.");
        }
        
        if (GetComponent<VehicleInput>() == null)
        {
            gameObject.AddComponent<VehicleInput>();
            Debug.Log("VehicleInput 컴포넌트가 추가되었습니다.");
        }
        
        if (GetComponent<VehicleAudio>() == null)
        {
            gameObject.AddComponent<VehicleAudio>();
            Debug.Log("VehicleAudio 컴포넌트가 추가되었습니다.");
        }
        
        if (GetComponent<MobileFortress>() == null)
        {
            gameObject.AddComponent<MobileFortress>();
            Debug.Log("MobileFortress 컴포넌트가 추가되었습니다.");
        }
    }
    
    [ContextMenu("휠 콜라이더 참조 설정")]
    public void SetupWheelReferences()
    {
        VehiclePhysics vehiclePhysics = GetComponent<VehiclePhysics>();
        if (vehiclePhysics == null)
        {
            Debug.LogError("VehiclePhysics 컴포넌트가 없습니다!");
            return;
        }
        
        // 휠 콜라이더들 찾기
        WheelCollider[] wheelColliders = GetComponentsInChildren<WheelCollider>();
        if (wheelColliders.Length < 4)
        {
            Debug.LogError("휠 콜라이더가 4개 미만입니다!");
            return;
        }
        
        // 휠 메시들 찾기
        Transform[] wheelMeshes = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            string wheelName = wheelColliders[i].name;
            string meshName = wheelName.Replace("Wheel", "WheelMesh");
            Transform meshTransform = transform.Find(meshName);
            wheelMeshes[i] = meshTransform;
        }
        
        // VehiclePhysics에 휠 참조 자동 설정
        SetupVehiclePhysicsReferences(vehiclePhysics, wheelColliders, wheelMeshes);
        
        Debug.Log("휠 참조가 자동으로 설정되었습니다!");
    }
    
    private void SetupVehiclePhysicsReferences(VehiclePhysics vehiclePhysics, WheelCollider[] wheelColliders, Transform[] wheelMeshes)
    {
        // 리플렉션을 사용하여 private 필드에 접근
        var physicsType = typeof(VehiclePhysics);
        
        // 휠 콜라이더 참조 설정
        var frontLeftField = physicsType.GetField("frontLeftWheelCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var frontRightField = physicsType.GetField("frontRightWheelCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rearLeftField = physicsType.GetField("rearLeftWheelCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rearRightField = physicsType.GetField("rearRightWheelCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // 휠 메시 참조 설정
        var frontLeftMeshField = physicsType.GetField("frontLeftWheelTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var frontRightMeshField = physicsType.GetField("frontRightWheelTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rearLeftMeshField = physicsType.GetField("rearLeftWheelTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rearRightMeshField = physicsType.GetField("rearRightWheelTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // 순서대로 설정 (FrontLeft, FrontRight, RearLeft, RearRight)
        if (frontLeftField != null) frontLeftField.SetValue(vehiclePhysics, wheelColliders[0]);
        if (frontRightField != null) frontRightField.SetValue(vehiclePhysics, wheelColliders[1]);
        if (rearLeftField != null) rearLeftField.SetValue(vehiclePhysics, wheelColliders[2]);
        if (rearRightField != null) rearRightField.SetValue(vehiclePhysics, wheelColliders[3]);
        
        if (frontLeftMeshField != null) frontLeftMeshField.SetValue(vehiclePhysics, wheelMeshes[0]);
        if (frontRightMeshField != null) frontRightMeshField.SetValue(vehiclePhysics, wheelMeshes[1]);
        if (rearLeftMeshField != null) rearLeftMeshField.SetValue(vehiclePhysics, wheelMeshes[2]);
        if (rearRightMeshField != null) rearRightMeshField.SetValue(vehiclePhysics, wheelMeshes[3]);
        
        Debug.Log("VehiclePhysics 휠 참조가 자동으로 설정되었습니다!");
    }
}
