using UnityEngine;

public class VehicleAudio : MonoBehaviour
{
    [Header("엔진 사운드")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioClip engineIdleClip;
    [SerializeField] private AudioClip engineRunningClip;
    [SerializeField] private float enginePitchRange = 0.5f;
    [SerializeField] private float engineVolumeRange = 0.3f;
    
    [Header("휠 사운드")]
    [SerializeField] private AudioSource wheelAudioSource;
    [SerializeField] private AudioClip wheelRollingClip;
    [SerializeField] private AudioClip wheelSkidClip;
    [SerializeField] private float wheelVolume = 0.5f;
    
    [Header("브레이크 사운드")]
    [SerializeField] private AudioSource brakeAudioSource;
    [SerializeField] private AudioClip brakeClip;
    [SerializeField] private float brakeVolume = 0.6f;
    
    [Header("경적 사운드")]
    [SerializeField] private AudioSource hornAudioSource;
    [SerializeField] private AudioClip hornClip;
    [SerializeField] private float hornVolume = 0.8f;
    
    [Header("사운드 설정")]
    [SerializeField] private float minEnginePitch = 0.8f;
    [SerializeField] private float maxEnginePitch = 1.5f;
    [SerializeField] private float enginePitchSmoothTime = 0.1f;
    [SerializeField] private float engineVolumeSmoothTime = 0.2f;
    
    private VehiclePhysics vehiclePhysics;
    private VehicleInput vehicleInput;
    
    // 현재 사운드 상태
    private float currentEnginePitch;
    private float currentEngineVolume;
    private float targetEnginePitch;
    private float targetEngineVolume;
    
    // 스무딩을 위한 변수들
    private float pitchVelocity;
    private float volumeVelocity;
    
    // 사운드 재생 상태
    private bool isEngineRunning = false;
    private bool isWheelRolling = false;
    private bool isBraking = false;
    
    private void Start()
    {
        vehiclePhysics = GetComponent<VehiclePhysics>();
        vehicleInput = GetComponent<VehicleInput>();
        
        // AudioSource 컴포넌트들이 없으면 자동으로 생성
        SetupAudioSources();
        
        // 기본 엔진 사운드 시작
        StartEngineSound();
        
        // 입력 이벤트 구독
        if (vehicleInput != null)
        {
            vehicleInput.OnThrottleChanged += OnThrottleChanged;
            vehicleInput.OnBrakeChanged += OnBrakeChanged;
            vehicleInput.OnHornPressed += OnHornPressed;
        }
    }
    
    private void Update()
    {
        UpdateEngineSound();
        UpdateWheelSound();
    }
    
    private void SetupAudioSources()
    {
        // 엔진 AudioSource
        if (engineAudioSource == null)
        {
            engineAudioSource = gameObject.AddComponent<AudioSource>();
            engineAudioSource.loop = true;
            engineAudioSource.playOnAwake = false;
            engineAudioSource.spatialBlend = 1f; // 3D 사운드
            engineAudioSource.rolloffMode = AudioRolloffMode.Linear;
            engineAudioSource.minDistance = 5f;
            engineAudioSource.maxDistance = 50f;
        }
        
        // 휠 AudioSource
        if (wheelAudioSource == null)
        {
            wheelAudioSource = gameObject.AddComponent<AudioSource>();
            wheelAudioSource.loop = true;
            wheelAudioSource.playOnAwake = false;
            wheelAudioSource.spatialBlend = 1f;
            wheelAudioSource.rolloffMode = AudioRolloffMode.Linear;
            wheelAudioSource.minDistance = 3f;
            wheelAudioSource.maxDistance = 20f;
        }
        
        // 브레이크 AudioSource
        if (brakeAudioSource == null)
        {
            brakeAudioSource = gameObject.AddComponent<AudioSource>();
            brakeAudioSource.loop = false;
            brakeAudioSource.playOnAwake = false;
            brakeAudioSource.spatialBlend = 1f;
            brakeAudioSource.rolloffMode = AudioRolloffMode.Linear;
            brakeAudioSource.minDistance = 5f;
            brakeAudioSource.maxDistance = 30f;
        }
        
        // 경적 AudioSource
        if (hornAudioSource == null)
        {
            hornAudioSource = gameObject.AddComponent<AudioSource>();
            hornAudioSource.loop = false;
            hornAudioSource.playOnAwake = false;
            hornAudioSource.spatialBlend = 1f;
            hornAudioSource.rolloffMode = AudioRolloffMode.Linear;
            hornAudioSource.minDistance = 10f;
            hornAudioSource.maxDistance = 100f;
        }
    }
    
    private void StartEngineSound()
    {
        if (engineAudioSource != null && engineIdleClip != null)
        {
            engineAudioSource.clip = engineIdleClip;
            engineAudioSource.Play();
            isEngineRunning = true;
            
            // 초기값 설정
            currentEnginePitch = minEnginePitch;
            currentEngineVolume = 0.5f;
            engineAudioSource.pitch = currentEnginePitch;
            engineAudioSource.volume = currentEngineVolume;
        }
    }
    
    private void UpdateEngineSound()
    {
        if (vehiclePhysics == null || !isEngineRunning) return;
        
        // 속도에 따른 엔진 피치 계산
        float currentSpeed = vehiclePhysics.GetCurrentSpeed();
        float maxSpeed = vehiclePhysics.GetMaxSpeed();
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        
        // 엔진 피치 계산 (속도에 따라 증가)
        targetEnginePitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedRatio);
        
        // 엔진 볼륨 계산 (가속 시 증가)
        float throttleInput = vehicleInput != null ? vehicleInput.GetThrottleInput() : 0f;
        targetEngineVolume = 0.5f + (throttleInput * engineVolumeRange);
        
        // 스무딩 적용
        currentEnginePitch = Mathf.SmoothDamp(currentEnginePitch, targetEnginePitch, ref pitchVelocity, enginePitchSmoothTime);
        currentEngineVolume = Mathf.SmoothDamp(currentEngineVolume, targetEngineVolume, ref volumeVelocity, engineVolumeSmoothTime);
        
        // AudioSource에 적용
        engineAudioSource.pitch = currentEnginePitch;
        engineAudioSource.volume = currentEngineVolume;
    }
    
    private void UpdateWheelSound()
    {
        if (vehiclePhysics == null || wheelAudioSource == null) return;
        
        float currentSpeed = vehiclePhysics.GetCurrentSpeed();
        bool shouldPlayWheelSound = currentSpeed > 1f; // 1m/s 이상일 때만 재생
        
        if (shouldPlayWheelSound && !isWheelRolling)
        {
            // 휠 굴림 사운드 시작
            if (wheelRollingClip != null)
            {
                wheelAudioSource.clip = wheelRollingClip;
                wheelAudioSource.volume = wheelVolume;
                wheelAudioSource.Play();
                isWheelRolling = true;
            }
        }
        else if (!shouldPlayWheelSound && isWheelRolling)
        {
            // 휠 굴림 사운드 정지
            wheelAudioSource.Stop();
            isWheelRolling = false;
        }
        
        // 속도에 따른 피치 조정
        if (isWheelRolling)
        {
            float speedRatio = Mathf.Clamp01(currentSpeed / vehiclePhysics.GetMaxSpeed());
            wheelAudioSource.pitch = 0.8f + (speedRatio * 0.4f);
        }
    }
    
    private void OnThrottleChanged(float throttleInput)
    {
        // 가속 입력에 따른 사운드 변화는 UpdateEngineSound에서 처리
    }
    
    private void OnBrakeChanged(float brakeInput)
    {
        if (brakeAudioSource == null || brakeClip == null) return;
        
        if (brakeInput > 0.1f && !isBraking)
        {
            // 브레이크 사운드 시작
            brakeAudioSource.clip = brakeClip;
            brakeAudioSource.volume = brakeVolume;
            brakeAudioSource.Play();
            isBraking = true;
        }
        else if (brakeInput <= 0.1f && isBraking)
        {
            isBraking = false;
        }
    }
    
    private void OnHornPressed()
    {
        if (hornAudioSource != null && hornClip != null)
        {
            hornAudioSource.clip = hornClip;
            hornAudioSource.volume = hornVolume;
            hornAudioSource.Play();
        }
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void SetEngineVolume(float volume)
    {
        if (engineAudioSource != null)
        {
            engineAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    public void SetWheelVolume(float volume)
    {
        wheelVolume = Mathf.Clamp01(volume);
        if (wheelAudioSource != null)
        {
            wheelAudioSource.volume = wheelVolume;
        }
    }
    
    public void StopAllSounds()
    {
        if (engineAudioSource != null) engineAudioSource.Stop();
        if (wheelAudioSource != null) wheelAudioSource.Stop();
        if (brakeAudioSource != null) brakeAudioSource.Stop();
        if (hornAudioSource != null) hornAudioSource.Stop();
        
        isEngineRunning = false;
        isWheelRolling = false;
        isBraking = false;
    }
    
    public void ResumeAllSounds()
    {
        if (isEngineRunning && engineAudioSource != null)
        {
            engineAudioSource.Play();
        }
        if (isWheelRolling && wheelAudioSource != null)
        {
            wheelAudioSource.Play();
        }
    }
}
