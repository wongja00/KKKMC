using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class BackgroundBlur : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Camera mainCamera;
    
    [Header("블러 설정")]
    [SerializeField] private Material blurMaterial;
    [SerializeField] private int blurIterations = 2;
    [SerializeField] private float blurSize = 1.0f;
    [SerializeField] private float blurStrength = 1.0f;
    
    [Header("UI 설정")]
    [SerializeField] private Image blurBackgroundImage;
    
    private Camera blurCamera;
    private RenderTexture sourceTexture;
    private RenderTexture blurTexture;
    private RenderTexture tempTexture;
    
    private int screenWidth;
    private int screenHeight;
    
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (blurMaterial == null)
        {
            blurMaterial = new Material(Shader.Find("Custom/Blur"));
        }
        
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        
        CreateBlurCamera();
        CreateRenderTextures();
        
        if (blurBackgroundImage != null)
        {
            blurBackgroundImage.material = blurMaterial;
        }
    }
    
    void CreateBlurCamera()
    {
        // 별도의 블러용 카메라 생성
        GameObject blurCameraObj = new GameObject("BlurCamera");
        blurCameraObj.transform.SetParent(transform);
        blurCamera = blurCameraObj.AddComponent<Camera>();
        
        // 메인 카메라와 동일한 설정 복사
        blurCamera.CopyFrom(mainCamera);
        
        // UI 레이어는 제외 (UI는 블러되지 않도록)
        blurCamera.cullingMask = mainCamera.cullingMask & ~(1 << LayerMask.NameToLayer("UI"));
        
        // 카메라를 비활성화 (필요할 때만 활성화)
        blurCamera.enabled = false;
        blurCamera.targetTexture = sourceTexture;
    }
    
    void CreateRenderTextures()
    {
        // 원본 텍스처 생성
        sourceTexture = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.ARGB32);
        sourceTexture.name = "BlurSource";
        
        // 블러 텍스처 생성 (성능 최적화를 위해 해상도 절반)
        blurTexture = new RenderTexture(screenWidth / 2, screenHeight / 2, 0, RenderTextureFormat.ARGB32);
        blurTexture.name = "BlurResult";
        
        // 임시 텍스처 생성
        tempTexture = new RenderTexture(screenWidth / 2, screenHeight / 2, 0, RenderTextureFormat.ARGB32);
        tempTexture.name = "BlurTemp";
        
        if (blurCamera != null)
        {
            blurCamera.targetTexture = sourceTexture;
        }
    }
    
    void OnEnable()
    {
        if (blurCamera != null)
        {
            blurCamera.enabled = true;
        }
    }
    
    void OnDisable()
    {
        if (blurCamera != null)
        {
            blurCamera.enabled = false;
        }
    }
    
    void LateUpdate()
    {
        if (blurCamera == null || blurMaterial == null) return;
        
        // 화면 크기가 변경되었는지 확인
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            
            // 기존 텍스처 해제
            if (sourceTexture != null) sourceTexture.Release();
            if (blurTexture != null) blurTexture.Release();
            if (tempTexture != null) tempTexture.Release();
            
            CreateRenderTextures();
        }
        
        // 블러 카메라가 메인 카메라와 동기화
        if (mainCamera != null && blurCamera != null)
        {
            blurCamera.transform.position = mainCamera.transform.position;
            blurCamera.transform.rotation = mainCamera.transform.rotation;
            blurCamera.fieldOfView = mainCamera.fieldOfView;
        }
        
        // 블러 적용
        ApplyBlur();
    }
    
    void ApplyBlur()
    {
        if (sourceTexture == null || blurMaterial == null) return;
        
        // Material에 파라미터 설정
        blurMaterial.SetFloat("_BlurSize", blurSize);
        blurMaterial.SetFloat("_BlurStrength", blurStrength);
        
        // 원본 텍스처를 블러 텍스처 크기로 다운스케일
        Graphics.Blit(sourceTexture, blurTexture);
        
        // 블러 반복 적용
        for (int i = 0; i < blurIterations; i++)
        {
            Graphics.Blit(blurTexture, tempTexture, blurMaterial);
            Graphics.Blit(tempTexture, blurTexture, blurMaterial);
        }
        
        // 블러된 텍스처를 UI Image에 할당
        if (blurBackgroundImage != null)
        {
            // RenderTexture을 Texture2D로 복사 후 Sprite로 생성
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = blurTexture;

            Texture2D blurTex2D = new Texture2D(blurTexture.width, blurTexture.height, TextureFormat.RGBA32, false);
            blurTex2D.ReadPixels(new Rect(0, 0, blurTexture.width, blurTexture.height), 0, 0);
            blurTex2D.Apply();

            RenderTexture.active = activeRT;

            blurBackgroundImage.sprite = Sprite.Create(
                blurTex2D, 
                new Rect(0, 0, blurTex2D.width, blurTex2D.height), 
                new Vector2(0.5f, 0.5f)
            );
        }
    }
    
    void OnDestroy()
    {
        // 메모리 해제
        if (sourceTexture != null) sourceTexture.Release();
        if (blurTexture != null) blurTexture.Release();
        if (tempTexture != null) tempTexture.Release();
        
        // 블러 카메라 제거
        if (blurCamera != null)
        {
            Destroy(blurCamera.gameObject);
        }
    }
    
    // 외부에서 블러 강도 조절
    public void SetBlurSize(float size)
    {
        blurSize = size;
    }
    
    public void SetBlurStrength(float strength)
    {
        blurStrength = strength;
    }
}

