using UnityEngine;
using System.Collections.Generic;

public class GrassComputeShader : MonoBehaviour
{
    [Header("Grass Settings")]
    public ComputeShader grassComputeShader;
    public Mesh grassBladeMesh;
    public Material grassMaterial;
    public int maxGrassCount = 100000;
    public float grassDensity = 10f;
    public float grassHeight = 5f;     // 1f → 5f로 증가
    public float grassWidth = 0.5f;    // 0.1f → 0.5f로 증가
    
    [Header("Wind Settings")]
    public float windStrength = 0.3f;  // 1f → 0.3f로 감소
    public float windSpeed = 1f;
    public Vector2 windDirection = Vector2.right;
    
    [Header("Terrain Settings")]
    public Terrain targetTerrain;
    public float minHeight = 0.0f;    // 0.05f → 0.0f로 변경 (모든 높이 허용)
    public float maxHeight = 1.0f;    // 0.95f → 1.0f로 변경 (모든 높이 허용)
    public float maxSlope = 89f;      // 75f → 89f로 변경 (거의 모든 경사 허용)
    [Range(0f, 30f)]
    public float maxGrassTilt = 15f;  // 잔디 최대 기울기 각도 (0 = 완전 수직, 30 = 최대 기울기)
    
    [Header("Performance")]
    public int batchSize = 1000;
    public float cullDistance = 500f;    // 100f → 500f로 증가
    public bool useFrustumCulling = false; // true → false로 변경 (Frustum 컬링 비활성화)
    public bool fallbackToCPU = true; // CPU 폴백 옵션 추가
    
    // Compute buffers
    private ComputeBuffer grassDataBuffer;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer culledGrassBuffer;
    
    // GPU Instancing
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private MaterialPropertyBlock propertyBlock;
    
    // Grass data
    private GrassData[] grassDataArray;
    private int currentGrassCount;
    private bool isInitialized = false;
    private bool useComputeShader = true; // 컴퓨트 셰이더 사용 여부
    
    // Wind time
    private float windTime = 0f;
    
    // GrassData 구조체 정의
    [System.Serializable]
    public struct GrassData
    {
        public Vector3 position;      // 잔디 위치
        public Vector3 normal;        // 지형 법선
        public float height;          // 잔디 높이
        public float width;           // 잔디 너비
        public Quaternion rotation;   // 잔디 회전 (Vector3 대신 Quaternion 사용)
        public float windOffset;      // 바람 오프셋
        public float health;          // 잔디 상태
    }
    
    void Start()
    {
        // Validate all required components
        if (grassComputeShader == null)
        {
            Debug.LogError("GrassComputeShader: grassComputeShader is not assigned!");
            return;
        }
        
        if (grassBladeMesh == null)
        {
            Debug.LogError("GrassComputeShader: grassBladeMesh is not assigned!");
            return;
        }
        
        if (grassMaterial == null)
        {
            Debug.LogError("GrassComputeShader: grassMaterial is not assigned!");
            return;
        }
        
        if (targetTerrain == null)
        {
            Debug.LogError("GrassComputeShader: targetTerrain is not assigned!");
            return;
        }
        
        Debug.Log($"GrassComputeShader: Initialized with {grassBladeMesh.name}, {grassMaterial.name}");
        
        // Check if kernel exists
        int kernel = grassComputeShader.FindKernel("UpdateGrass");
        if (kernel < 0)
        {
            Debug.LogError($"GrassComputeShader: Kernel 'UpdateGrass' not found!");
            return;
        }
        
        // Check if we already have grass data from editor
        if (currentGrassCount > 0 && grassDataArray != null && grassDataArray.Length > 0)
        {
            Debug.Log($"GrassComputeShader: Using existing grass data from editor ({currentGrassCount} blades)");
        }
        else
        {
            Debug.Log("GrassComputeShader: No existing grass data, generating new grass...");
        }
        
        InitializeGrassSystem();
    }
    
    void InitializeGrassSystem()
    {
        if (grassComputeShader == null || grassBladeMesh == null || grassMaterial == null)
        {
            Debug.LogError("GrassComputeShader: Missing required components!");
            return;
        }
        
        // Initialize grass data only if we don't have any
        if (currentGrassCount <= 0 || grassDataArray == null || grassDataArray.Length == 0)
        {
            GenerateGrassData();
        }
        
        // 버퍼 크기 검증
        int expectedStride = 56; // Vector3(12) + Vector3(12) + float(4) + float(4) + Quaternion(16) + float(4) + float(4)
        Debug.Log($"GrassComputeShader: Creating buffers with stride {expectedStride} bytes for {currentGrassCount} grass blades");
        
        // Create compute buffers
        // GrassData struct: Vector3(12) + Vector3(12) + float(4) + float(4) + Quaternion(16) + float(4) + float(4) = 56 bytes
        grassDataBuffer = new ComputeBuffer(maxGrassCount, expectedStride);
        grassDataBuffer.SetData(grassDataArray);
        
        culledGrassBuffer = new ComputeBuffer(maxGrassCount, expectedStride);
        
        // Setup GPU instancing
        args[0] = grassBladeMesh.GetIndexCount(0);
        args[1] = (uint)currentGrassCount;
        args[2] = grassBladeMesh.GetIndexStart(0);
        args[3] = grassBladeMesh.GetBaseVertex(0);
        args[4] = 0;
        
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        
        // Setup material property block
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetBuffer("_GrassData", grassDataBuffer);
        propertyBlock.SetFloat("_WindStrength", windStrength);
        propertyBlock.SetFloat("_WindSpeed", windSpeed);
        propertyBlock.SetVector("_WindDirection", windDirection);
        
        isInitialized = true;
        Debug.Log($"GrassComputeShader: System ready with {currentGrassCount} grass blades");
    }
    
    void GenerateGrassData()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("GrassComputeShader: No target terrain assigned!");
            return;
        }
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPosition = targetTerrain.transform.position;
        
        List<GrassData> grassList = new List<GrassData>();
        
        // Calculate grass positions based on terrain
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(maxGrassCount / grassDensity));
        float cellSize = Mathf.Min(terrainSize.x, terrainSize.z) / gridSize;
        
        // 각 셀에서 여러 개의 잔디 생성 시도
        int grassPerCell = Mathf.Max(1, Mathf.RoundToInt(grassDensity));
        
        Debug.Log($"GrassComputeShader: Grid size: {gridSize}x{gridSize}, Cell size: {cellSize:F2}");
        Debug.Log($"GrassComputeShader: Grass per cell: {grassPerCell}, Max attempts: {gridSize * gridSize * grassPerCell}");
        Debug.Log($"GrassComputeShader: Terrain size: {terrainSize}, Position: {terrainPosition}");
        Debug.Log($"GrassComputeShader: Height range: {minHeight} - {maxHeight}, Max slope: {maxSlope}");
        
        int attempts = 0;
        int heightRejected = 0;
        int slopeRejected = 0;
        int boundsRejected = 0;
        int successful = 0;
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (grassList.Count >= maxGrassCount) break;
                
                // 각 셀에서 여러 개의 잔디 생성
                for (int g = 0; g < grassPerCell; g++)
                {
                    if (grassList.Count >= maxGrassCount) break;
                    
                    attempts++;
                    
                    // Random position within cell
                    float worldX = terrainPosition.x + x * cellSize + Random.Range(0f, cellSize);
                    float worldZ = terrainPosition.z + z * cellSize + Random.Range(0f, cellSize);
                    
                    // Get terrain height and normal
                    float u = (worldX - terrainPosition.x) / terrainSize.x;
                    float v = (worldZ - terrainPosition.z) / terrainSize.z;
                    
                    if (u < 0f || u > 1f || v < 0f || v > 1f) 
                    {
                        boundsRejected++;
                        continue;
                    }
                    
                    float height = terrainData.GetInterpolatedHeight(u, v);
                    Vector3 normal = terrainData.GetInterpolatedNormal(u, v);
                    float normalizedHeight = height / terrainSize.y;
                    
                    // Check height and slope constraints
                    if (normalizedHeight < minHeight || normalizedHeight > maxHeight) 
                    {
                        heightRejected++;
                        continue;
                    }
                    
                    float slope = Vector3.Angle(normal, Vector3.up);
                    if (slope > maxSlope) 
                    {
                        slopeRejected++;
                        continue;
                    }
                    
                    // Random grass properties
                    float randomHeight = grassHeight * Random.Range(0.8f, 1.2f);
                    float randomWidth = grassWidth * Random.Range(0.8f, 1.2f);
                    float randomRotation = Random.Range(0f, 360f);
                    float windOffset = Random.Range(0f, 2f * Mathf.PI);
                    
                    // 잔디가 지형 경사에 관계없이 수직으로 서도록 회전 계산
                    Quaternion grassRotation = Quaternion.identity;
                    
                    // 지형의 경사가 너무 가파르면 수직으로 서도록 조정
                    if (slope > 45f)
                    {
                        // 가파른 경사에서는 수직으로 서도록
                        grassRotation = Quaternion.Euler(0, randomRotation, 0);
                    }
                    else
                    {
                        // 완만한 경사에서는 지형에 맞춰 약간 기울어지도록
                        Vector3 upDirection = Vector3.up;
                        Vector3 slopeDirection = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
                        
                        // 경사에 따른 회전 (maxGrassTilt로 제한)
                        float maxTilt = Mathf.Clamp(slope * 0.3f, 0f, maxGrassTilt);
                        Quaternion tiltRotation = Quaternion.AngleAxis(maxTilt, Vector3.Cross(upDirection, slopeDirection));
                        Quaternion yawRotation = Quaternion.AngleAxis(randomRotation, Vector3.up);
                        
                        grassRotation = tiltRotation * yawRotation;
                    }
                    
                    GrassData grass = new GrassData
                    {
                        position = new Vector3(worldX, height, worldZ),
                        normal = normal,
                        height = randomHeight,
                        width = randomWidth,
                        rotation = grassRotation, // Quaternion 사용
                        windOffset = windOffset,
                        health = 1f
                    };
                    
                    grassList.Add(grass);
                    successful++;
                    
                    // 첫 번째 잔디 위치 로그
                    if (grassList.Count == 1)
                    {
                        Debug.Log($"GrassComputeShader: First grass at position {grass.position}, height: {normalizedHeight:F3}, slope: {slope:F1}");
                    }
                    
                    // 100개마다 진행상황 로그
                    if (grassList.Count % 100 == 0)
                    {
                        Debug.Log($"GrassComputeShader: Generated {grassList.Count} grass blades so far...");
                    }
                }
            }
        }
        
        currentGrassCount = grassList.Count;
        grassDataArray = grassList.ToArray();
        
        Debug.Log($"GrassComputeShader: Generated {currentGrassCount} grass blades from {attempts} attempts");
        Debug.Log($"GrassComputeShader: Successful: {successful}, Rejected - Bounds: {boundsRejected}, Height: {heightRejected}, Slope: {slopeRejected}");
        
        // If no grass was generated, try with relaxed conditions
        if (currentGrassCount == 0)
        {
            Debug.LogWarning("GrassComputeShader: No grass generated! Trying with relaxed conditions...");
            GenerateGrassDataRelaxed();
        }
    }
    
    void GenerateGrassDataRelaxed()
    {
        Debug.Log("GrassComputeShader: Using relaxed generation conditions");
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPosition = targetTerrain.transform.position;
        
        List<GrassData> grassList = new List<GrassData>();
        
        // Use much smaller grid for testing
        int testCount = Mathf.Min(100, maxGrassCount);
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(testCount));
        float cellSize = Mathf.Min(terrainSize.x, terrainSize.z) / gridSize;
        
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (grassList.Count >= testCount) break;
                
                // Center of each cell
                float worldX = terrainPosition.x + x * cellSize + cellSize * 0.5f;
                float worldZ = terrainPosition.z + z * cellSize + cellSize * 0.5f;
                
                // Get terrain height and normal
                float u = (worldX - terrainPosition.x) / terrainSize.x;
                float v = (worldZ - terrainPosition.z) / terrainSize.z;
                
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                
                float height = terrainData.GetInterpolatedHeight(u, v);
                Vector3 normal = terrainData.GetInterpolatedNormal(u, v);
                float normalizedHeight = height / terrainSize.y;
                
                // Very relaxed conditions
                if (normalizedHeight < 0.1f || normalizedHeight > 0.9f) continue;
                
                float slope = Vector3.Angle(normal, Vector3.up);
                if (slope > 60f) continue; // Much more permissive slope
                
                GrassData grass = new GrassData
                {
                    position = new Vector3(worldX, height, worldZ),
                    normal = normal,
                    height = grassHeight,
                    width = grassWidth,
                    rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0), // Quaternion 사용
                    windOffset = Random.Range(0f, 2f * Mathf.PI),
                    health = 1f
                };
                
                grassList.Add(grass);
            }
        }
        
        currentGrassCount = grassList.Count;
        grassDataArray = grassList.ToArray();
        
        Debug.Log($"GrassComputeShader: Relaxed generation complete - {currentGrassCount} grass blades");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Update wind time
        windTime += Time.deltaTime * windSpeed;
        
        // Update grass based on available method
        if (useComputeShader)
        {
            UpdateGrassCompute();
        }
        else
        {
            UpdateGrassCPU();
        }
        
        // Render grass
        RenderGrass();
    }
    
    void UpdateGrassCompute()
    {
        if (grassComputeShader == null) 
        {
            Debug.LogError("GrassComputeShader: grassComputeShader is null!");
            return;
        }
        
        if (currentGrassCount <= 0)
        {
            return;
        }
        
        try
        {
            int kernel = grassComputeShader.FindKernel("UpdateGrass");
            
            if (kernel < 0)
            {
                Debug.LogError($"GrassComputeShader: Kernel 'UpdateGrass' not found!");
                return;
            }
            
            // Set buffers
            grassComputeShader.SetBuffer(kernel, "GrassData", grassDataBuffer);
            grassComputeShader.SetBuffer(kernel, "CulledGrass", culledGrassBuffer);
            
            // Set parameters
            grassComputeShader.SetFloat("WindTime", windTime);
            grassComputeShader.SetFloat("WindStrength", windStrength);
            grassComputeShader.SetVector("WindDirection", windDirection);
            grassComputeShader.SetFloat("CullDistance", cullDistance);
            
            if (Camera.main != null)
            {
                grassComputeShader.SetVector("CameraPosition", Camera.main.transform.position);
            }
            else
            {
                grassComputeShader.SetVector("CameraPosition", Vector3.zero);
            }
            
            grassComputeShader.SetInt("GrassCount", currentGrassCount);
            
            // Calculate thread groups
            int threadGroups = Mathf.CeilToInt(currentGrassCount / 64f);
            
            if (threadGroups > 0)
            {
                // Dispatch compute shader
                grassComputeShader.Dispatch(kernel, threadGroups, 1, 1);
                
                // Check if dispatch was successful by trying to access the buffer
                try
                {
                    // This will trigger an error if the dispatch failed
                    float[] testData = new float[1];
                    grassDataBuffer.GetData(testData);
                }
                catch (System.Exception bufferError)
                {
                    if (fallbackToCPU)
                    {
                        Debug.Log("GrassComputeShader: Falling back to CPU-based grass update");
                        useComputeShader = false;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GrassComputeShader: Error in UpdateGrassCompute: {e.Message}");
            if (fallbackToCPU)
            {
                Debug.Log("GrassComputeShader: Falling back to CPU-based grass update due to error");
                useComputeShader = false;
            }
        }
    }
    
    void UpdateGrassCPU()
    {
        if (currentGrassCount <= 0) return;
        
        Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        
        for (int i = 0; i < currentGrassCount; i++)
        {
            if (grassDataArray[i].health <= 0f) continue;
            
            // Distance culling (disabled for better visibility)
            // float distanceToCamera = Vector3.Distance(grassDataArray[i].position, cameraPos);
            // if (distanceToCamera > cullDistance) continue;
            
            // Simple wind effect (reduced intensity)
            float windEffect = Mathf.Sin(windTime + grassDataArray[i].windOffset) * windStrength * 0.1f;
            
            // Update rotation based on wind (reduced rotation)
            grassDataArray[i].rotation = Quaternion.Euler(0, grassDataArray[i].rotation.eulerAngles.y + windEffect, 0); // Quaternion 사용
        }
        
        // 버퍼 크기 확인 후 데이터 업데이트
        if (grassDataBuffer != null && grassDataBuffer.count > 0)
        {
            // Update the buffer with CPU-processed data
            grassDataBuffer.SetData(grassDataArray);
            
            // Copy to culled buffer for rendering
            culledGrassBuffer.SetData(grassDataArray);
        }
    }
    
    void RenderGrass()
    {
        if (grassMaterial == null)
        {
            Debug.LogError("GrassComputeShader: grassMaterial is null!");
            return;
        }
        
        if (grassBladeMesh == null)
        {
            Debug.LogError("GrassComputeShader: grassBladeMesh is null!");
            return;
        }
        
        if (currentGrassCount <= 0)
        {
            return;
        }
        
        // 렌더링 디버깅 정보 (매 프레임마다 출력하지 않도록)
        if (Time.frameCount % 300 == 0) // 5초마다 한 번씩
        {
            Debug.Log($"GrassComputeShader: Rendering {currentGrassCount} grass blades");
            Debug.Log($"GrassComputeShader: Mesh - Vertices: {grassBladeMesh.vertexCount}, Triangles: {grassBladeMesh.triangles.Length / 3}");
            Debug.Log($"GrassComputeShader: Material: {grassMaterial.name}, Shader: {grassMaterial.shader.name}");
        }
        
        // GPU Instancing 대신 일반 렌더링 사용
        for (int i = 0; i < Mathf.Min(currentGrassCount, 1000); i++) // 성능을 위해 1000개로 제한
        {
            if (grassDataArray[i].health <= 0f) continue;
            
            // 각 잔디를 개별적으로 렌더링
            Matrix4x4 matrix = Matrix4x4.TRS(
                grassDataArray[i].position,
                grassDataArray[i].rotation,
                new Vector3(grassDataArray[i].width, grassDataArray[i].height, grassDataArray[i].width)
            );
            
            Graphics.DrawMesh(grassBladeMesh, matrix, grassMaterial, 0);
        }
        
        // 렌더링 완료 확인
        if (Time.frameCount % 300 == 0)
        {
            Debug.Log("GrassComputeShader: Render call completed");
        }
    }
    
    // Public methods for multiplayer interaction
    public void CutGrass(Vector3 position, float radius)
    {
        if (!isInitialized) return;
        
        for (int i = 0; i < currentGrassCount; i++)
        {
            float distance = Vector3.Distance(grassDataArray[i].position, position);
            if (distance <= radius)
            {
                grassDataArray[i].health = 0f;
            }
        }
        
        grassDataBuffer.SetData(grassDataArray);
    }
    
    public void PlantGrass(Vector3 position, int count = 1)
    {
        if (!isInitialized || currentGrassCount >= maxGrassCount) return;
        
        for (int i = 0; i < count; i++)
        {
            if (currentGrassCount >= maxGrassCount) break;
            
            Vector3 randomOffset = Random.insideUnitSphere * 2f;
            Vector3 plantPosition = position + randomOffset;
            
            // Get terrain height at position
            if (targetTerrain != null)
            {
                TerrainData terrainData = targetTerrain.terrainData;
                Vector3 terrainPosition = targetTerrain.transform.position;
                Vector3 terrainSize = terrainData.size;
                
                float u = (plantPosition.x - terrainPosition.x) / terrainSize.x;
                float v = (plantPosition.z - terrainPosition.z) / terrainSize.z;
                
                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                {
                    float height = terrainData.GetInterpolatedHeight(u, v);
                    Vector3 normal = terrainData.GetInterpolatedNormal(u, v);
                    
                    GrassData newGrass = new GrassData
                    {
                        position = new Vector3(plantPosition.x, height, plantPosition.z),
                        normal = normal,
                        height = grassHeight * Random.Range(0.8f, 1.2f),
                        width = grassWidth * Random.Range(0.8f, 1.2f),
                        rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0), // Quaternion 사용
                        windOffset = Random.Range(0f, 2f * Mathf.PI),
                        health = 1f
                    };
                    
                    grassDataArray[currentGrassCount] = newGrass;
                    currentGrassCount++;
                }
            }
        }
        
        grassDataBuffer.SetData(grassDataArray);
        args[1] = (uint)currentGrassCount;
        argsBuffer.SetData(args);
    }
    
    // Editor methods for generating grass without play mode
    [ContextMenu("Generate Grass in Editor")]
    public void GenerateGrassInEditor()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("GrassComputeShader: No target terrain assigned!");
            return;
        }
        
        if (grassBladeMesh == null)
        {
            Debug.LogError("GrassComputeShader: No grass blade mesh assigned!");
            return;
        }
        
        if (grassMaterial == null)
        {
            Debug.LogError("GrassComputeShader: No grass material assigned!");
            return;
        }
        
        Debug.Log("GrassComputeShader: Generating grass in editor...");
        
        // Generate grass data
        GenerateGrassData();
        
        // Create visual representation in editor
        CreateEditorGrassObjects();
        
        Debug.Log($"GrassComputeShader: Generated {currentGrassCount} grass blades in editor");
        
        // Mark scene as dirty to save changes
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        #endif
    }
    
    [ContextMenu("Clear All Grass")]
    public void ClearAllGrass()
    {
        // Clear grass data
        currentGrassCount = 0;
        grassDataArray = new GrassData[0];
        
        // Remove editor grass objects
        RemoveEditorGrassObjects();
        
        Debug.Log("GrassComputeShader: Cleared all grass");
        
        // Mark scene as dirty
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        #endif
    }
    
    public int GetCurrentGrassCount()
    {
        return currentGrassCount;
    }
    
    private void CreateEditorGrassObjects()
    {
        // Remove existing editor grass objects
        RemoveEditorGrassObjects();
        
        if (currentGrassCount <= 0) return;
        
        // Create parent object for all grass
        GameObject grassParent = new GameObject("Editor Grass");
        grassParent.transform.SetParent(transform);
        
        // Create individual grass objects
        for (int i = 0; i < Mathf.Min(currentGrassCount, 1000); i++) // Limit to 1000 for performance
        {
            if (grassDataArray[i].health <= 0f) continue;
            
            GameObject grassObject = new GameObject($"Grass_{i}");
            grassObject.transform.SetParent(grassParent.transform);
            grassObject.transform.position = grassDataArray[i].position;
            
            // Quaternion 회전을 그대로 적용
            grassObject.transform.rotation = grassDataArray[i].rotation;
            
            // Add mesh filter and renderer
            MeshFilter meshFilter = grassObject.AddComponent<MeshFilter>();
            meshFilter.mesh = grassBladeMesh;
            
            MeshRenderer meshRenderer = grassObject.AddComponent<MeshRenderer>();
            meshRenderer.material = grassMaterial;
            
            // Scale based on grass properties
            float scale = grassDataArray[i].height / 5f; // Normalize to reasonable size
            grassObject.transform.localScale = new Vector3(grassDataArray[i].width, scale, grassDataArray[i].width);
        }
        
        Debug.Log($"GrassComputeShader: Created {grassParent.transform.childCount} editor grass objects");
    }
    
    private void RemoveEditorGrassObjects()
    {
        // Find and remove existing editor grass parent
        Transform existingParent = transform.Find("Editor Grass");
        if (existingParent != null)
        {
            DestroyImmediate(existingParent.gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (grassDataBuffer != null && grassDataBuffer.IsValid())
        {
            grassDataBuffer.Release();
            grassDataBuffer = null;
        }
        if (argsBuffer != null && argsBuffer.IsValid())
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
        if (culledGrassBuffer != null && culledGrassBuffer.IsValid())
        {
            culledGrassBuffer.Release();
            culledGrassBuffer = null;
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw terrain bounds
        if (targetTerrain != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(targetTerrain.transform.position + targetTerrain.terrainData.size * 0.5f, targetTerrain.terrainData.size);
            
            // 지형 중심점 표시
            Gizmos.color = Color.magenta;
            Vector3 terrainCenter = targetTerrain.transform.position + targetTerrain.terrainData.size * 0.5f;
            Gizmos.DrawWireSphere(terrainCenter, 2f);
        }
        
        // Draw grass positions
        if (grassDataArray != null && grassDataArray.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < Mathf.Min(grassDataArray.Length, 100); i++) // 최대 100개만 표시
            {
                if (grassDataArray[i].health > 0f)
                {
                    // 잔디 위치에 작은 구체 그리기
                    Gizmos.DrawWireSphere(grassDataArray[i].position, 0.5f);
                    
                    // 잔디 방향 표시
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(grassDataArray[i].position, grassDataArray[i].normal * 2f);
                    Gizmos.color = Color.yellow;
                }
            }
        }
        
        // Draw camera position and cull distance
        if (Camera.main != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(Camera.main.transform.position, 1f);
            
            // 컬링 거리 표시 (더 큰 구체)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Camera.main.transform.position, cullDistance);
            
            // 카메라 방향 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 10f);
        }
    }

    // Editor methods for generating grass without play mode
    [ContextMenu("Analyze Terrain")]
    public void AnalyzeTerrain()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("GrassComputeShader: No target terrain assigned!");
            return;
        }
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPosition = targetTerrain.transform.position;
        
        Debug.Log("=== TERRAIN ANALYSIS ===");
        Debug.Log($"Terrain Size: {terrainSize}");
        Debug.Log($"Terrain Position: {terrainPosition}");
        
        // Analyze height distribution
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        float totalHeight = 0f;
        int sampleCount = 0;
        
        // Sample terrain heights
        int sampleGrid = 50; // 50x50 grid for analysis
        for (int x = 0; x < sampleGrid; x++)
        {
            for (int z = 0; z < sampleGrid; z++)
            {
                float u = (float)x / (sampleGrid - 1);
                float v = (float)z / (sampleGrid - 1);
                
                float height = terrainData.GetInterpolatedHeight(u, v);
                float normalizedHeight = height / terrainSize.y;
                
                minHeight = Mathf.Min(minHeight, normalizedHeight);
                maxHeight = Mathf.Max(maxHeight, normalizedHeight);
                totalHeight += normalizedHeight;
                sampleCount++;
            }
        }
        
        float avgHeight = totalHeight / sampleCount;
        
        Debug.Log($"Height Analysis (from {sampleCount} samples):");
        Debug.Log($"  Min Height: {minHeight:F3} ({minHeight * 100:F1}%)");
        Debug.Log($"  Max Height: {maxHeight:F3} ({maxHeight * 100:F1}%)");
        Debug.Log($"  Avg Height: {avgHeight:F3} ({avgHeight * 100:F1}%)");
        
        // Analyze slope distribution
        int flatCount = 0;      // 0-15 degrees
        int gentleCount = 0;    // 15-30 degrees
        int moderateCount = 0;  // 30-45 degrees
        int steepCount = 0;     // 45-60 degrees
        int verySteepCount = 0; // 60+ degrees
        
        for (int x = 0; x < sampleGrid; x++)
        {
            for (int z = 0; z < sampleGrid; z++)
            {
                float u = (float)x / (sampleGrid - 1);
                float v = (float)z / (sampleGrid - 1);
                
                Vector3 normal = terrainData.GetInterpolatedNormal(u, v);
                float slope = Vector3.Angle(normal, Vector3.up);
                
                if (slope <= 15f) flatCount++;
                else if (slope <= 30f) gentleCount++;
                else if (slope <= 45f) moderateCount++;
                else if (slope <= 60f) steepCount++;
                else verySteepCount++;
            }
        }
        
        Debug.Log($"Slope Analysis (from {sampleCount} samples):");
        Debug.Log($"  Flat (0-15°): {flatCount} ({flatCount * 100f / sampleCount:F1}%)");
        Debug.Log($"  Gentle (15-30°): {gentleCount} ({gentleCount * 100f / sampleCount:F1}%)");
        Debug.Log($"  Moderate (30-45°): {moderateCount} ({moderateCount * 100f / sampleCount:F1}%)");
        Debug.Log($"  Steep (45-60°): {steepCount} ({steepCount * 100f / sampleCount:F1}%)");
        Debug.Log($"  Very Steep (60°+): {verySteepCount} ({verySteepCount * 100f / sampleCount:F1}%)");
        
        // Recommend settings
        Debug.Log("=== RECOMMENDED SETTINGS ===");
        Debug.Log($"  minHeight: {Mathf.Max(0f, minHeight - 0.05f):F3}");
        Debug.Log($"  maxHeight: {Mathf.Min(1f, maxHeight + 0.05f):F3}");
        Debug.Log($"  maxSlope: 89f (to cover all slopes)");
        Debug.Log("=== END ANALYSIS ===");
    }

    // Editor methods for generating grass without play mode
    [ContextMenu("Check Material Settings")]
    public void CheckMaterialSettings()
    {
        if (grassMaterial == null)
        {
            Debug.LogError("GrassComputeShader: No grass material assigned!");
            return;
        }
        
        Debug.Log("=== MATERIAL SETTINGS CHECK ===");
        Debug.Log($"Material: {grassMaterial.name}");
        Debug.Log($"Shader: {grassMaterial.shader.name}");
        
        // Material 속성 확인
        if (grassMaterial.HasProperty("_Color"))
        {
            Color color = grassMaterial.GetColor("_Color");
            Debug.Log($"Base Color: {color} (Alpha: {color.a:F3})");
            
            // 알파값이 너무 낮으면 경고
            if (color.a < 0.9f)
            {
                Debug.LogWarning($"Material alpha is too low ({color.a:F3}). Setting to 1.0");
                color.a = 1.0f;
                grassMaterial.SetColor("_Color", color);
            }
        }
        
        if (grassMaterial.HasProperty("_MainTex"))
        {
            Texture mainTex = grassMaterial.GetTexture("_MainTex");
            if (mainTex != null)
            {
                Debug.Log($"Main Texture: {mainTex.name} ({mainTex.width}x{mainTex.height})");
            }
            else
            {
                Debug.LogWarning("No main texture assigned!");
            }
        }
        
        // Shader 키워드 확인
        if (grassMaterial.IsKeywordEnabled("_ALPHATEST_ON"))
        {
            Debug.Log("Alpha Test: ON");
        }
        if (grassMaterial.IsKeywordEnabled("_ALPHABLEND_ON"))
        {
            Debug.Log("Alpha Blend: ON");
        }
        if (grassMaterial.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
        {
            Debug.Log("Alpha Premultiply: ON");
        }
        
        Debug.Log("=== END MATERIAL CHECK ===");
    }
    
    [ContextMenu("Create Default Grass Material")]
    public void CreateDefaultGrassMaterial()
    {
        #if UNITY_EDITOR
        // 기본 잔디 Material 생성 (Standard 쉐이더 사용)
        Material newMaterial = new Material(Shader.Find("Standard"));
        if (newMaterial != null)
        {
            // 기본 설정
            newMaterial.name = "DefaultGrassMaterial";
            newMaterial.SetColor("_Color", new Color(0.2f, 0.8f, 0.2f, 1.0f)); // 녹색, 알파 1.0
            newMaterial.SetFloat("_Metallic", 0.0f);
            newMaterial.SetFloat("_Smoothness", 0.1f);
            
            // 알파 블렌딩 비활성화
            newMaterial.SetFloat("_Mode", 0); // Opaque mode
            newMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            newMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            newMaterial.SetInt("_ZWrite", 1);
            newMaterial.DisableKeyword("_ALPHATEST_ON");
            newMaterial.DisableKeyword("_ALPHABLEND_ON");
            newMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            newMaterial.renderQueue = -1;
            
            // Material을 프로젝트에 저장
            string path = "Assets/Materials/";
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            
            UnityEditor.AssetDatabase.CreateAsset(newMaterial, path + "DefaultGrassMaterial.mat");
            UnityEditor.AssetDatabase.SaveAssets();
            
            // 현재 Material로 설정
            grassMaterial = newMaterial;
            
            Debug.Log("Default grass material created and assigned!");
        }
        else
        {
            Debug.LogError("Failed to create default material!");
        }
        #else
        Debug.LogWarning("CreateDefaultGrassMaterial is only available in editor mode");
        #endif
    }
    
    [ContextMenu("Switch to Standard Shader")]
    public void SwitchToStandardShader()
    {
        #if UNITY_EDITOR
        if (grassMaterial == null)
        {
            Debug.LogError("No grass material assigned!");
            return;
        }
        
        // Standard 쉐이더로 변경
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            grassMaterial.shader = standardShader;
            
            // 기본 설정 적용
            grassMaterial.SetColor("_Color", new Color(0.2f, 0.8f, 0.2f, 1.0f));
            grassMaterial.SetFloat("_Metallic", 0.0f);
            grassMaterial.SetFloat("_Smoothness", 0.1f);
            
            // 알파 블렌딩 비활성화
            grassMaterial.SetFloat("_Mode", 0);
            grassMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            grassMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            grassMaterial.SetInt("_ZWrite", 1);
            grassMaterial.DisableKeyword("_ALPHATEST_ON");
            grassMaterial.DisableKeyword("_ALPHABLEND_ON");
            grassMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            grassMaterial.renderQueue = -1;
            
            Debug.Log($"Switched to Standard shader: {grassMaterial.name}");
        }
        else
        {
            Debug.LogError("Standard shader not found!");
        }
        #else
        Debug.LogWarning("SwitchToStandardShader is only available in editor mode");
        #endif
    }
    
    [ContextMenu("Check Rendering Status")]
    public void CheckRenderingStatus()
    {
        Debug.Log("=== RENDERING STATUS CHECK ===");
        
        // 기본 컴포넌트 확인
        Debug.Log($"Grass Compute Shader: {(grassComputeShader != null ? "✓" : "✗")}");
        Debug.Log($"Grass Blade Mesh: {(grassBladeMesh != null ? "✓" : "✗")}");
        Debug.Log($"Grass Material: {(grassMaterial != null ? "✓" : "✗")}");
        Debug.Log($"Target Terrain: {(targetTerrain != null ? "✓" : "✗")}");
        
        // Mesh 정보 확인
        if (grassBladeMesh != null)
        {
            Debug.Log($"Mesh Info - Vertices: {grassBladeMesh.vertexCount}, Triangles: {grassBladeMesh.triangles.Length / 3}");
            Debug.Log($"Mesh Bounds: {grassBladeMesh.bounds}");
        }
        
        // Material 정보 확인
        if (grassMaterial != null)
        {
            Debug.Log($"Material: {grassMaterial.name}");
            Debug.Log($"Shader: {grassMaterial.shader.name}");
            Debug.Log($"Render Queue: {grassMaterial.renderQueue}");
            
            // Material이 렌더링 가능한지 확인
            if (grassMaterial.HasProperty("_Color"))
            {
                Color color = grassMaterial.GetColor("_Color");
                Debug.Log($"Base Color: {color} (Alpha: {color.a:F3})");
            }
        }
        
        // 시스템 상태 확인
        Debug.Log($"System Initialized: {(isInitialized ? "✓" : "✗")}");
        Debug.Log($"Current Grass Count: {currentGrassCount}");
        Debug.Log($"Use Compute Shader: {useComputeShader}");
        
        // 버퍼 상태 확인
        if (grassDataBuffer != null)
        {
            Debug.Log($"Grass Data Buffer: ✓ (Count: {grassDataBuffer.count}, Stride: {grassDataBuffer.stride})");
        }
        else
        {
            Debug.Log("Grass Data Buffer: ✗");
        }
        
        if (argsBuffer != null)
        {
            Debug.Log($"Args Buffer: ✓");
        }
        else
        {
            Debug.Log("Args Buffer: ✗");
        }
        
        Debug.Log("=== END RENDERING STATUS ===");
    }
}
