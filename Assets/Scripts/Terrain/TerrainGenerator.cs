using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

//터레인 생성 코드
public class TerrainGenerator : MonoBehaviour
{
	//터레인 크기
	public int width = 256;
	//터레인 높이
	public int height = 100;  // 50 → 100으로 증가
	//터레인 깊이
	public int depth = 256;
	//터레인 노이즈 스케일
	public float scale = 15f;  // 20 → 15로 감소 (더 거친 지형)
	
	//멀티 옥타브 설정
	[Header("Multi-Octave Settings")]
	public int octaves = 4;           // 3 → 4로 증가 (더 복잡한 지형)
	public float persistence = 0.5f;  // 0.3 → 0.5로 증가 (더 다양한 변화)
	public float lacunarity = 2f;     // 1.8 → 2로 증가 (더 세밀한 변화)
	
	//추가 설정
	[Header("Settings")]
	public float islandRadius = 0.9f;     // 0.8 → 0.9로 증가 (섬 크기 확대)
	public float coastSmoothness = 0.15f; // 0.1 → 0.15로 증가 (해안선 부드러움)
	public float mountainScale = 1.2f;    // 0.6 → 1.2로 증가 (산 강도 대폭 증가)
	public float baseHeightMultiplier = 1.5f; // 기본 높이 배수 추가
	
	// 평지 확장 파라미터
	[Header("Plains Settings")]
	public float plainsThreshold = 0.45f;       // 이 값 이하의 영역을 평지화 대상으로
	public float plainsTargetHeight = 0.28f;    // 평지의 목표 높이(정규화)
	public float plainsFlattenStrength = 0.65f; // 평지로 당기는 강도(0~1)
	public float heightBiasPower = 1.25f;       // 전체 고도 곡선(>1이면 저지대 확대)
	
	// 텍스처 레이어(인스펙터에서 Sand/Grass/Rock 순으로 지정 권장)
	[Header("Textures")]
	public TerrainLayer[] terrainLayers;
	
	// 오브젝트 배치(나무/바위)
	[Header("Object Placement")]
	public GameObject treePrefab;
	public GameObject rockPrefab;
	public GameObject[] treePrefabs;
	public GameObject[] rockPrefabs;
	public int treeCount = 300;
	public int rockCount = 150;
	public float minTreeHeight = 0.25f;
	public float maxTreeHeight = 0.7f;
	public float maxTreeSlopeDeg = 25f;
	public float minRockHeight = 0.35f;
	public float maxRockSlopeDeg = 35f;
	public Vector2 treeScaleRange = new Vector2(0.85f, 1.2f);
	public Vector2 rockScaleRange = new Vector2(0.6f, 1.1f);
	public float minTreeSpacing = 3f; // 월드 단위 간격
	public float minRockSpacing = 7f; // 월드 단위 간격
    public float rockScaleMultiplier = 0.25f; // 바위 전체 스케일 배수
	public bool alignToNormal = true;
	public bool useDeterministicSeed = false;
	public int randomSeed = 12345;

	// 잔디 디테일
	[Header("Grass Details")]
	public Texture2D grassTexture;
	public int detailResolution = 512;
	public int grassDensity = 16; // 0~16 권장
	public int grassLayerIndex = 0;
	public bool forcePlaceIfZero = true; // 0개일 때 소량 강제 배치로 원인 분리
	public bool autoSaveAfterEditPlacement = true; // 에디트 모드 배치 후 자동 저장
	public bool skipSpawnIfExists = true; // 이미 배치되어 있으면 플레이 시 재배치 생략
	
	// 마지막으로 생성한 높이맵 보관(텍스처 페인팅에 사용)
	float[,] lastHeights;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		//터레인 생성
		Terrain terrain = GetComponent<Terrain>();
		terrain.terrainData = GenerateTerrain(terrain.terrainData);
	}

	// 터레인 생성 함수
	TerrainData GenerateTerrain(TerrainData terrainData)
	{
		terrainData.heightmapResolution = width + 1;
		terrainData.size = new Vector3(width, height, depth);
		terrainData.SetHeights(0, 0, GenerateHeights());
		
		// 텍스처 적용
		ApplyTextures(terrainData);
		
		// 잔디/오브젝트 배치
		AddGrassDetails(terrainData);
		if (!skipSpawnIfExists || !HasExistingProps())
		{
			ScatterTreesAndRocks(GetComponent<Terrain>());
		}
		else
		{
			Debug.Log("[TerrainGenerator] 기존 Props_Trees_Rocks 가 존재하여 배치를 생략합니다.");
		}
		return terrainData;
	}

	// 터레인 높이 생성 함수
	float[,] GenerateHeights()
	{
		float[,] heights = new float[width, depth];
		
		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				float xCoord = (float)x / width * scale;
				float zCoord = (float)z / depth * scale;
				
				//노이즈 계산
				heights[x, z] = GenerateRustStyleNoise(xCoord, zCoord, x, z);
			}
		}
		// 텍스처 페인팅을 위해 저장
		lastHeights = heights;
		return heights;
	}
	
	//노이즈 생성 함수
	float GenerateRustStyleNoise(float x, float z, int pixelX, int pixelZ)
	{
		// 1. 기본 멀티 옥타브 노이즈 생성
		float baseNoise = GenerateMultiOctaveNoise(x, z);
		
		// 1-1. 전체 고도 곡선(저지대 비중 확대)
		baseNoise = Mathf.Pow(Mathf.Clamp01(baseNoise), heightBiasPower);
		
		// 2. 섬 모양 마스크 생성 (중앙이 높고 가장자리가 낮음)
		float islandMask = GenerateIslandMask(pixelX, pixelZ);
		
		// 3. 산과 평원 분리
		float mountainNoise = GenerateMountainNoise(x * 2f, z * 2f);
		
		// 4. 최종 조합
		float finalHeight = CombineRustTerrain(baseNoise, islandMask, mountainNoise);
		
		return finalHeight;
	}
	
	// 멀티 옥타브 Perlin Noise 생성 함수
	float GenerateMultiOctaveNoise(float x, float z)
	{
		float noise = 0f;
		float amplitude = 1f;
		float frequency = 1f;
		float maxValue = 0f;
		
		for (int i = 0; i < octaves; i++)
		{
			noise += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
			maxValue += amplitude;
			amplitude *= persistence;
			frequency *= lacunarity;
		}
		
		return noise / maxValue;
	}
	
	// 섬 모양 마스크 생성
	float GenerateIslandMask(int x, int z)
	{
		// 중앙에서의 거리 계산 (0~1)
		float centerX = width * 0.5f;
		float centerZ = depth * 0.5f;
		float distanceFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(centerX, centerZ));
		float maxDistance = Mathf.Min(width, depth) * 0.5f * islandRadius;
		
		// 거리에 따른 높이 계산 (중앙이 높고 가장자리가 낮음)
		float normalizedDistance = distanceFromCenter / maxDistance;
		float islandMask = Mathf.Clamp01(1f - normalizedDistance);
		
		// 부드러운 해안선
		islandMask = Mathf.SmoothStep(0f, coastSmoothness, islandMask);
		
		return islandMask;
	}
	
	// 산 노이즈 생성 (더 거친 패턴)
	float GenerateMountainNoise(float x, float z)
	{
		float mountainNoise = 0f;
		float amplitude = 1f;
		float frequency = 1f;
		float maxValue = 0f;
		
		// 산은 더 적은 옥타브로 거친 패턴 생성
		for (int i = 0; i < 3; i++)  // 2 → 3으로 증가 (더 복잡한 산)
		{
			mountainNoise += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
			maxValue += amplitude;
			amplitude *= 0.5f;  // 0.4 → 0.5로 증가
			frequency *= 2.2f;  // 2.5 → 2.2로 감소 (부드러운 산)
		}
		
		return (mountainNoise / maxValue) * mountainScale;
	}
	
	//지형 조합
	float CombineRustTerrain(float baseNoise, float islandMask, float mountainNoise)
	{
		// 기본 지형 (더 높은 언덕)
		float terrain = baseNoise * baseHeightMultiplier; // 0.7f → baseHeightMultiplier로 변경
		
		// 평지 확장: 낮은 고도 구간을 목표 평지 높이로 당김
		float plainsMask = Mathf.Clamp01((plainsThreshold - baseNoise) / Mathf.Max(0.0001f, plainsThreshold));
		plainsMask = plainsMask * plainsMask; // 가장 낮은 구간에서 더 강하게
		terrain = Mathf.Lerp(terrain, plainsTargetHeight, plainsMask * plainsFlattenStrength);
		
		// 산 추가 (더 낮은 임계값으로 더 많은 지역에 산 생성)
		float mountainThreshold = 0.4f;  // 0.6 → 0.4로 감소
		if (baseNoise > mountainThreshold)
		{
			float mountainBlend = (baseNoise - mountainThreshold) / (1f - mountainThreshold);
			terrain += mountainNoise * mountainBlend * 1.5f; // 산 강도 1.5배 증가
		}
		
		// 섬 모양 적용
		terrain *= islandMask;
		
		// 최종 정규화
		return Mathf.Clamp01(terrain);
	}

	bool HasExistingProps()
	{
		Transform parent = transform.Find("Props_Trees_Rocks");
		return parent != null && parent.childCount > 0;
	}

	// 에디트 모드에서 한 번에 생성/배치/저장
	[ContextMenu("Generate & Place (Edit Mode)")]
	public void GenerateAndPlaceInEditMode()
	{
		Terrain terrain = GetComponent<Terrain>();
		if (terrain == null)
		{
			Debug.LogError("[TerrainGenerator] Terrain 컴포넌트를 찾을 수 없습니다.");
			return;
		}
		terrain.terrainData = GenerateTerrain(terrain.terrainData);
		#if UNITY_EDITOR
		if (autoSaveAfterEditPlacement)
		{
			EditorSceneManager.MarkSceneDirty(gameObject.scene);
			EditorSceneManager.SaveOpenScenes();
			Debug.Log("[TerrainGenerator] Edit Mode 배치 후 씬 저장 완료");
		}
		#endif
	}

	// 에디트 모드에서 배치된 프롭 정리
	[ContextMenu("Clear Props (Edit Mode)")]
	public void ClearPlacedProps()
	{
		Transform oldParent = transform.Find("Props_Trees_Rocks");
		if (oldParent != null)
		{
			#if UNITY_EDITOR
			DestroyImmediate(oldParent.gameObject);
			EditorSceneManager.MarkSceneDirty(gameObject.scene);
			#else
			Destroy(oldParent.gameObject);
			#endif
			Debug.Log("[TerrainGenerator] Props_Trees_Rocks 정리 완료");
		}
	}
	
	// 잔디 디테일 추가
	void AddGrassDetails(TerrainData td)
	{
		if (grassTexture == null)
		{
			return;
		}

		var proto = new DetailPrototype
		{
			prototype = null,
			prototypeTexture = grassTexture,
			renderMode = DetailRenderMode.GrassBillboard,
			healthyColor = Color.white,
			dryColor = new Color(0.8f, 0.8f, 0.7f),
			minWidth = 0.6f,
			maxWidth = 1.1f,
			minHeight = 0.6f,
			maxHeight = 1.1f,
			noiseSpread = 0.3f,
			usePrototypeMesh = false
		};

		td.detailPrototypes = new DetailPrototype[] { proto };
		td.SetDetailResolution(detailResolution, 16);

		int[,] map = new int[detailResolution, detailResolution];
		for (int y = 0; y < detailResolution; y++)
		{
			for (int x = 0; x < detailResolution; x++)
			{
				float u = (float)x / (detailResolution - 1);
				float v = (float)y / (detailResolution - 1);
				int ix = Mathf.Clamp(Mathf.RoundToInt(u * (width - 1)), 0, width - 1);
				int iz = Mathf.Clamp(Mathf.RoundToInt(v * (depth - 1)), 0, depth - 1);

				float hNorm = lastHeights != null ? lastHeights[ix, iz] : td.GetInterpolatedHeight(u, v) / Mathf.Max(1f, height);
				float slopeDeg = Vector3.Angle(td.GetInterpolatedNormal(u, v), Vector3.up);

				int density = 0;
				if (hNorm >= 0.2f && hNorm <= 0.6f && slopeDeg <= 18f)
				{
					float noise = Mathf.PerlinNoise(u * 6f, v * 6f);
					density = Mathf.RoundToInt(grassDensity * Mathf.Lerp(0.4f, 1f, noise));
				}
				map[y, x] = Mathf.Clamp(density, 0, 16);
			}
		}

		td.SetDetailLayer(0, 0, grassLayerIndex, map);
	}

	// 나무/바위 배치
	void ScatterTreesAndRocks(Terrain terrain)
	{
		if (terrain == null)
		{
			return;
		}

		// 이전 배치 정리
		Transform oldParent = transform.Find("Props_Trees_Rocks");
		if (oldParent != null)
		{
			#if UNITY_EDITOR
			UnityEngine.Object.DestroyImmediate(oldParent.gameObject);
			#else
			UnityEngine.Object.Destroy(oldParent.gameObject);
			#endif
		}

		Transform parent = new GameObject("Props_Trees_Rocks").transform;
		parent.SetParent(transform, false);

		TerrainData td = terrain.terrainData;

		// 로컬 함수: 위치 샘플링 (Terrain 실제 높이에 정확히 맞춤)
		Vector3 SampleWorld(int x, int z, out float hNorm, out float slopeDeg)
		{
			float u = Mathf.Clamp01((float)x / Mathf.Max(1, width - 1));
			float v = Mathf.Clamp01((float)z / Mathf.Max(1, depth - 1));
			hNorm = lastHeights != null ? lastHeights[x, z] : td.GetInterpolatedHeight(u, v) / Mathf.Max(1f, height);
			Vector3 normal = td.GetInterpolatedNormal(u, v);
			slopeDeg = Vector3.Angle(normal, Vector3.up);
			float worldY = td.GetInterpolatedHeight(u, v) + terrain.transform.position.y;
			return new Vector3(
				terrain.transform.position.x + u * width,
				worldY,
				terrain.transform.position.z + v * depth
			);
		}

		int placedTrees = 0;
		int placedRocks = 0;

		List<Vector3> treePositions = new List<Vector3>();
		List<Vector3> rockPositions = new List<Vector3>();

		bool IsFarEnough(List<Vector3> list, Vector3 candidate, float minDist)
		{
			float minSqr = minDist * minDist;
			for (int i = 0; i < list.Count; i++)
			{
				Vector3 a = list[i];
				Vector2 da = new Vector2(a.x, a.z);
				Vector2 db = new Vector2(candidate.x, candidate.z);
				if ((da - db).sqrMagnitude < minSqr) return false;
			}
			return true;
		}

		if (treePrefab == null && (treePrefabs == null || treePrefabs.Length == 0))
		{
			Debug.LogWarning("[TerrainGenerator] treePrefab이 비어 있습니다. 나무가 배치되지 않습니다.");
		}
		if (rockPrefab == null && (rockPrefabs == null || rockPrefabs.Length == 0))
		{
			Debug.LogWarning("[TerrainGenerator] rockPrefab이 비어 있습니다. 바위가 배치되지 않습니다.");
		}

		if (useDeterministicSeed)
		{
			Random.InitState(randomSeed);
		}

		// Trees
		if ((treePrefab != null || (treePrefabs != null && treePrefabs.Length > 0)) && treeCount > 0)
		{
			int attempts = 0;
			int maxAttempts = Mathf.Max(treeCount * 20, 200);
			while (placedTrees < treeCount && attempts < maxAttempts)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out float h, out float slope);
				if (h >= minTreeHeight && h <= maxTreeHeight && slope <= maxTreeSlopeDeg && IsFarEnough(treePositions, pos, minTreeSpacing))
				{
					GameObject prefab = treePrefab;
					if (treePrefabs != null && treePrefabs.Length > 0)
					{
						prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
					}
					if (prefab != null)
					{
						float yaw = Random.Range(0f, 360f);
						Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
						if (alignToNormal)
						{
							float u = Mathf.Clamp01((float)x / Mathf.Max(1, width - 1));
							float v = Mathf.Clamp01((float)z / Mathf.Max(1, depth - 1));
							Vector3 n = td.GetInterpolatedNormal(u, v);
							rot = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, yaw, 0f);
						}
						float s = Random.Range(Mathf.Min(treeScaleRange.x, treeScaleRange.y), Mathf.Max(treeScaleRange.x, treeScaleRange.y));
						var go = Instantiate(prefab, pos, rot, parent);
						go.transform.localScale = Vector3.one * s;
						treePositions.Add(pos);
						placedTrees++;
					}
				}
				attempts++;
			}
		}

		// Rocks
		if ((rockPrefab != null || (rockPrefabs != null && rockPrefabs.Length > 0)) && rockCount > 0)
		{
			int attempts = 0;
			int maxAttempts = Mathf.Max(rockCount * 20, 200);
			while (placedRocks < rockCount && attempts < maxAttempts)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out float h, out float slope);
				if (h >= minRockHeight && slope <= maxRockSlopeDeg && IsFarEnough(rockPositions, pos, minRockSpacing))
				{
					GameObject prefab = rockPrefab;
					if (rockPrefabs != null && rockPrefabs.Length > 0)
					{
						prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
					}
					if (prefab != null)
					{
						float yaw = Random.Range(0f, 360f);
						Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
						if (alignToNormal)
						{
							float u = Mathf.Clamp01((float)x / Mathf.Max(1, width - 1));
							float v = Mathf.Clamp01((float)z / Mathf.Max(1, depth - 1));
							Vector3 n = td.GetInterpolatedNormal(u, v);
							rot = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, yaw, 0f);
						}
						float s = Random.Range(Mathf.Min(rockScaleRange.x, rockScaleRange.y), Mathf.Max(rockScaleRange.x, rockScaleRange.y)) * rockScaleMultiplier;
						var go = Instantiate(prefab, pos, rot, parent);
						go.transform.localScale = Vector3.one * s;
						rockPositions.Add(pos);
						placedRocks++;
					}
				}
				attempts++;
			}
		}

		// 2차 시도: 아무것도 배치되지 않았을 때 임계치 자동 완화
		if (placedTrees == 0 && (treePrefab != null || (treePrefabs != null && treePrefabs.Length > 0)) && treeCount > 0)
		{
			Debug.LogWarning("[TerrainGenerator] Trees 1차 배치 실패 → 조건 완화 후 재시도합니다.");
			for (int i = 0; i < treeCount; i++)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out float h, out float slope);
				if (h >= 0.02f && h <= 0.98f && slope <= 60f)
				{
					GameObject prefab = treePrefab;
					if (treePrefabs != null && treePrefabs.Length > 0)
					{
						prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
					}
					float yaw = Random.Range(0f, 360f);
					Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
					float s = Random.Range(Mathf.Min(treeScaleRange.x, treeScaleRange.y), Mathf.Max(treeScaleRange.x, treeScaleRange.y));
					var go = Instantiate(prefab, pos, rot, parent);
					go.transform.localScale = Vector3.one * s;
					placedTrees++;
				}
			}
		}

		if (placedRocks == 0 && (rockPrefab != null || (rockPrefabs != null && rockPrefabs.Length > 0)) && rockCount > 0)
		{
			Debug.LogWarning("[TerrainGenerator] Rocks 1차 배치 실패 → 조건 완화 후 재시도합니다.");
			for (int i = 0; i < rockCount; i++)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out float h, out float slope);
				if (h >= 0.02f && slope <= 70f)
				{
					GameObject prefab = rockPrefab;
					if (rockPrefabs != null && rockPrefabs.Length > 0)
					{
						prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
					}
					float yaw = Random.Range(0f, 360f);
					Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
					float s = Random.Range(Mathf.Min(rockScaleRange.x, rockScaleRange.y), Mathf.Max(rockScaleRange.x, rockScaleRange.y));
					var go = Instantiate(prefab, pos, rot, parent);
					go.transform.localScale = Vector3.one * s;
					placedRocks++;
				}
			}
		}

		// 0개면 소량 강제 배치(원인 분리용)
		if (forcePlaceIfZero && placedTrees == 0 && (treePrefab != null || (treePrefabs != null && treePrefabs.Length > 0)))
		{
			for (int i = 0; i < Mathf.Min(10, treeCount); i++)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out _, out _);
				GameObject prefab = treePrefab;
				if (treePrefabs != null && treePrefabs.Length > 0)
				{
					prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
				}
				var go = Instantiate(prefab, pos, Quaternion.identity, parent);
				go.transform.localScale = Vector3.one * Random.Range(0.9f, 1.2f);
				placedTrees++;
			}
			Debug.LogWarning("[TerrainGenerator] Trees 강제 배치 수행(원인 분리용). 조건/프리팹/렌더링 설정 확인 필요.");
		}
		if (forcePlaceIfZero && placedRocks == 0 && (rockPrefab != null || (rockPrefabs != null && rockPrefabs.Length > 0)))
		{
			for (int i = 0; i < Mathf.Min(10, rockCount); i++)
			{
				int x = Random.Range(0, Mathf.Max(1, width));
				int z = Random.Range(0, Mathf.Max(1, depth));
				Vector3 pos = SampleWorld(x, z, out _, out _);
				GameObject prefab = rockPrefab;
				if (rockPrefabs != null && rockPrefabs.Length > 0)
				{
					prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
				}
				var go = Instantiate(prefab, pos, Quaternion.identity, parent);
				go.transform.localScale = Vector3.one * Random.Range(0.9f, 1.2f);
				placedRocks++;
			}
			Debug.LogWarning("[TerrainGenerator] Rocks 강제 배치 수행(원인 분리용). 조건/프리팹/렌더링 설정 확인 필요.");
		}

		Debug.Log($"Placed Trees: {placedTrees}/{treeCount}, Rocks: {placedRocks}/{rockCount}");
	}


// 텍스처 자동 페인팅 (수정된 버전)
void ApplyTextures(TerrainData terrainData)
{
    if (terrainLayers == null || terrainLayers.Length == 0)
    {
        Debug.LogError("No terrain layers assigned");
        return; // 인스펙터에서 레이어를 지정해야 함
    }
    
    Debug.Log($"Applying {terrainLayers.Length} terrain layers");
    terrainData.terrainLayers = terrainLayers;
    
    //텍스처 혼합을 위한 해상도
    int alphaRes = Mathf.Clamp(width, 16, 1024);
    //알파맵 해상도 터레인에 적용
    terrainData.alphamapResolution = alphaRes;
    //텍스처 레이어 개수
    int layersCount = terrainLayers.Length;
    //텍스처 혼합 맵 생성 3차원 배열인 이유는 텍스처 혼합 맵을 생성하기 위해서 
    //첫번째 차원은 y축 좌표, 두번째 차원은 x축 좌표, 세번째 차원은 텍스처 레이어 인덱스
    float[,,] splatmaps = new float[alphaRes, alphaRes, layersCount];
    

    //디버깅 로그
    Debug.Log($"Alpha resolution: {alphaRes}, Layers count: {layersCount}");
    
    // 디버깅용 통계
    float totalSand = 0f, totalGrass = 0f, totalRock = 0f;
    int pixelCount = 0;
    float minHeight = 1f, maxHeight = 0f, avgHeight = 0f;
    
    // 먼저 높이 통계 수집
    for (int y = 0; y < alphaRes; y++)
    {
        for (int x = 0; x < alphaRes; x++)
        {
            //해상도 좌표를 높이맵으로 변환
            //heightx, heightz
            float hx = (float)x / (alphaRes - 1) * (width - 1);
            float hz = (float)y / (alphaRes - 1) * (depth - 1);

            //높이맵 인덱스 계산함으로써 인덱스 배열범위 안정성 보장
            //indexx, indexz는 0~width-1, 0~depth-1 사이의 값
            int ix = Mathf.Clamp(Mathf.RoundToInt(hx), 0, width - 1);
            int iz = Mathf.Clamp(Mathf.RoundToInt(hz), 0, depth - 1);
            
            //높이값 추출 및 통계 수집
            //위에서 얻은 인덱스를 통해 높이값 h 추출 없으면 0f
            float h = lastHeights != null ? lastHeights[ix, iz] : 0f;
            //최소높이 최대높이 평균높이 계산
            minHeight = Mathf.Min(minHeight, h);
            maxHeight = Mathf.Max(maxHeight, h);
            avgHeight += h;
        }
    }
    
    if (alphaRes * alphaRes > 0)
    {
        avgHeight /= (alphaRes * alphaRes);
        Debug.Log($"Height Range: {minHeight:F3} - {maxHeight:F3}, Average: {avgHeight:F3}");
    }
    
    // 높이 분포에 기반한 백분위수 계산
    List<float> allHeights = new List<float>();
    for (int y = 0; y < alphaRes; y++)
    {
        for (int x = 0; x < alphaRes; x++)
        {
            float hx = (float)x / (alphaRes - 1) * (width - 1);
            float hz = (float)y / (alphaRes - 1) * (depth - 1);

            int ix = Mathf.Clamp(Mathf.RoundToInt(hx), 0, width - 1);
            int iz = Mathf.Clamp(Mathf.RoundToInt(hz), 0, depth - 1);
            allHeights.Add(lastHeights[ix, iz]);
        }
    }
    //하, 중, 상을 나누기 위해 정렬
    allHeights.Sort();
    
    // 백분위수 기반 임계값 (실제 높이 분포 고려)
    int totalPixels = allHeights.Count;
    float sandThreshold = allHeights[Mathf.RoundToInt(totalPixels * 0.4f)]; // 하위 40%
    float rockThreshold = allHeights[Mathf.RoundToInt(totalPixels * 0.75f)]; // 상위 25%
    
    Debug.Log($"Percentile Thresholds - Sand: {sandThreshold:F3} (40th percentile), Rock: {rockThreshold:F3} (75th percentile)");
    
    // 텍스처 적용
    for (int y = 0; y < alphaRes; y++)
    {
        for (int x = 0; x < alphaRes; x++)
        {
            float hx = (float)x / (alphaRes - 1) * (width - 1);
            float hz = (float)y / (alphaRes - 1) * (depth - 1);
            int ix = Mathf.Clamp(Mathf.RoundToInt(hx), 0, width - 1);
            int iz = Mathf.Clamp(Mathf.RoundToInt(hz), 0, depth - 1);
            
            float h = lastHeights != null ? lastHeights[ix, iz] : 0f;
            
            // 섬 밖은 전부 모래 처리
            float centerXw = (width - 1) * 0.5f;
            float centerZw = (depth - 1) * 0.5f;
            float distanceFromCenterW = Vector2.Distance(new Vector2(hx, hz), new Vector2(centerXw, centerZw));
            float maxDistanceW = Mathf.Min(width - 1, depth - 1) * 0.5f * islandRadius;
            if (distanceFromCenterW > maxDistanceW)
            {
                // Sand 전부, 나머지 0
                for (int l = 0; l < layersCount; l++)
                {
                    float w = 0f;
                    if (l == 0 && layersCount >= 1) w = 1f; // 0번 레이어를 Sand로 가정
                    splatmaps[y, x, l] = w;
                }
                totalSand += 1f;
                pixelCount++;
                continue;
            }
            
            // 경사 계산 개선
            int ix0 = Mathf.Max(ix - 1, 0);
            int ix1 = Mathf.Min(ix + 1, width - 1);
            int iz0 = Mathf.Max(iz - 1, 0);
            int iz1 = Mathf.Min(iz + 1, depth - 1);

            float dx = (lastHeights[ix1, iz] - lastHeights[ix0, iz]) * width;
            float dz = (lastHeights[ix, iz1] - lastHeights[ix, iz0]) * depth;
            float slope = Mathf.Sqrt(dx * dx + dz * dz);
            
            // 기본 규칙: 0:Sand, 1:Grass, 2:Rock 가정
            float wSand = 0f, wGrass = 0f, wRock = 0f;
            
            // 새로운 적응적 분류 시스템
            // 1. 높이 백분위수 기반 기본 분류
            if (h < sandThreshold) // 하위 40%
            {
                // 낮은 지역 - 모래와 잔디 혼합
                wSand = 0.6f;
                wGrass = 0.4f;
                wRock = 0f;
            }
            else if (h > rockThreshold) // 상위 25%
            {
                // 높은 지역 - 바위와 잔디 혼합
                wRock = 0.7f;
                wGrass = 0.3f;
                wSand = 0f;
            }
            else // 중간 35% (40% ~ 75%)
            {
                // 중간 지역 - 잔디 위주
                wGrass = 0.7f;
                wSand = 0.2f;
                wRock = 0.1f;
            }
            
            // 2. 경사도에 따른 보정 (더 강하게)
            if (slope > 0.2f) // 가파른 경사
            {
                // 바위 대폭 증가
                float slopeBonus = Mathf.Clamp01((slope - 0.2f) * 5f);
                //바위는 증가 나머지 감소
                wRock += slopeBonus * 0.6f;
                wGrass *= (1f - slopeBonus * 0.4f);
                wSand *= (1f - slopeBonus * 0.2f);
            }
            else if (slope < 0.05f) // 매우 평탄한 지역
            {
                // 평탄도에 따라 모래 또는 잔디 증가
                if (h < sandThreshold * 1.2f) // 낮고 평탄한 곳
                {
                    //모래 증가 나머지 감소
                    wSand += 0.3f;
                    wGrass *= 0.7f;
                    wRock *= 0.5f;
                }
                else // 높고 평탄한 곳
                {
                    //잔디 증가 나머지 감소
                    wGrass += 0.3f;
                    wSand *= 0.7f;
                    wRock *= 0.5f;
                }
            }
            
            // 3. 섬 가장자리 보정 (해안선) - 더 강하게
            float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(alphaRes * 0.5f, alphaRes * 0.5f));
            float maxDistance = alphaRes * 0.5f;
            float normalizedDistance = distanceFromCenter / maxDistance;
            
            if (normalizedDistance > 0.6f) // 가장자리 지역(중심에서 먼곳)
            {
                if (h < rockThreshold) // 가장자리 + 낮은~중간 지역
                {
                    // 해안가는 모래 대폭 증가
                    float coastBonus = (normalizedDistance - 0.6f) * 2.5f; // 0 ~ 1
                    wSand += coastBonus * 0.5f;
                    wGrass *= (1f - coastBonus * 0.3f);
                    wRock *= (1f - coastBonus * 0.2f);
                }
            }
            
            // 4. 노이즈로 자연스러운 변화 추가 (더 강하게)
            float noiseX = (float)x / alphaRes * 8f;
            float noiseY = (float)y / alphaRes * 8f;
            float textureNoise = Mathf.PerlinNoise(noiseX, noiseY);
            
            // 노이즈에 따라 텍스처 변경
            if (textureNoise > 0.65f)
            {
                wRock += 0.15f;
                wGrass *= 0.9f;
            }
            else if (textureNoise < 0.35f)
            {
                wSand += 0.15f;
                wGrass *= 0.9f;
            }
            else
            {
                wGrass += 0.1f; // 중간 노이즈는 잔디 증가
            }
            
            // 정규화
            float sum = wSand + wGrass + wRock;
            if (sum < 0.0001f) { wGrass = 1f; sum = 1f; }
            wSand /= sum; 
            wGrass /= sum; 
            wRock /= sum;
            
            // 디버깅 통계 누적
            totalSand += wSand;
            totalGrass += wGrass;
            totalRock += wRock;
            pixelCount++;
            
            // 레이어 할당
            for (int l = 0; l < layersCount; l++)
            {
                float w = 0f;
                if (l == 0 && layersCount >= 1) w = wSand;
                else if (l == 1 && layersCount >= 2) w = wGrass;
                else if (l == 2 && layersCount >= 3) w = wRock;
                // 레이어가 더 많다면 나머지는 0으로
                splatmaps[y, x, l] = w;
            }
        }
    }
    
    // 디버깅 통계 출력
    if (pixelCount > 0)
    {
        Debug.Log($"Texture Distribution - Sand: {totalSand/pixelCount*100:F1}%, Grass: {totalGrass/pixelCount*100:F1}%, Rock: {totalRock/pixelCount*100:F1}%");
        Debug.Log($"Target: Sand: ~30-35%, Grass: ~40-50%, Rock: ~20-25%");
    }
    
    terrainData.SetAlphamaps(0, 0, splatmaps);
    Debug.Log("Textures applied successfully");
}
}
