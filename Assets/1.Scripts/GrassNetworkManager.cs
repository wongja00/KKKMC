using UnityEngine;
using System.Collections.Generic;

public class GrassNetworkManager : MonoBehaviour
{
    [Header("Network Settings")]
    public bool isServer = true;
    public float syncInterval = 0.1f; // 10 FPS sync
    
    [Header("Grass Interaction")]
    public float cutRadius = 2f;
    public float plantRadius = 1f;
    public int plantCount = 5;
    
    private GrassComputeShader grassSystem;
    private Dictionary<Vector3, float> grassHealthMap = new Dictionary<Vector3, float>();
    private float lastSyncTime = 0f;
    
    // Network events
    public System.Action<Vector3, float> OnGrassCut;
    public System.Action<Vector3, int> OnGrassPlanted;
    
    void Start()
    {
        grassSystem = FindObjectOfType<GrassComputeShader>();
        if (grassSystem == null)
        {
            Debug.LogError("GrassNetworkManager: No GrassComputeShader found!");
        }
    }
    
    void Update()
    {
        // Sync grass state periodically
        if (Time.time - lastSyncTime > syncInterval)
        {
            SyncGrassState();
            lastSyncTime = Time.time;
        }
    }
    
    // Called when player cuts grass
    public void CutGrassAtPosition(Vector3 position)
    {
        if (grassSystem == null) return;
        
        // Local cut
        grassSystem.CutGrass(position, cutRadius);
        
        // Network event
        if (OnGrassCut != null)
            OnGrassCut(position, cutRadius);
        
        // Store in health map for network sync
        StoreGrassHealthChange(position, cutRadius, 0f);
        
        Debug.Log($"Cut grass at {position}");
    }
    
    // Called when player plants grass
    public void PlantGrassAtPosition(Vector3 position)
    {
        if (grassSystem == null) return;
        
        // Local plant
        grassSystem.PlantGrass(position, plantCount);
        
        // Network event
        if (OnGrassPlanted != null)
            OnGrassPlanted(position, plantCount);
        
        // Store in health map for network sync
        StoreGrassHealthChange(position, plantRadius, 1f);
        
        Debug.Log($"Planted grass at {position}");
    }
    
    // Network receive: grass cut from other players
    public void ReceiveGrassCut(Vector3 position, float radius)
    {
        if (grassSystem == null) return;
        
        grassSystem.CutGrass(position, radius);
        StoreGrassHealthChange(position, radius, 0f);
    }
    
    // Network receive: grass planted from other players
    public void ReceiveGrassPlanted(Vector3 position, int count)
    {
        if (grassSystem == null) return;
        
        grassSystem.PlantGrass(position, count);
        StoreGrassHealthChange(position, plantRadius, 1f);
    }
    
    // Store grass health changes for network synchronization
    private void StoreGrassHealthChange(Vector3 position, float radius, float health)
    {
        // This would be used to sync grass state across network
        // In a real implementation, you'd send this data to other clients
        Vector3 gridPos = new Vector3(
            Mathf.Round(position.x / 2f) * 2f,
            Mathf.Round(position.y / 2f) * 2f,
            Mathf.Round(position.z / 2f) * 2f
        );
        
        grassHealthMap[gridPos] = health;
    }
    
    // Sync grass state across network
    private void SyncGrassState()
    {
        if (!isServer) return;
        
        // In a real implementation, you'd send grassHealthMap to all clients
        // This is a simplified version
        foreach (var kvp in grassHealthMap)
        {
            Vector3 pos = kvp.Key;
            float health = kvp.Value;
            
            // Send to all clients
            // NetworkManager.Instance.SendGrassUpdate(pos, health);
        }
    }
    
    // Get grass health at position (for UI, etc.)
    public float GetGrassHealth(Vector3 position)
    {
        Vector3 gridPos = new Vector3(
            Mathf.Round(position.x / 2f) * 2f,
            Mathf.Round(position.y / 2f) * 2f,
            Mathf.Round(position.z / 2f) * 2f
        );
        
        if (grassHealthMap.ContainsKey(gridPos))
            return grassHealthMap[gridPos];
        
        return 1f; // Default to healthy grass
    }
    
    // Reset all grass (for testing)
    public void ResetAllGrass()
    {
        if (grassSystem == null) return;
        
        // This would require a reset method in GrassComputeShader
        // grassSystem.ResetGrass();
        grassHealthMap.Clear();
        
        Debug.Log("Reset all grass");
    }
    
    // Get grass statistics
    public GrassStats GetGrassStats()
    {
        int totalGrass = grassHealthMap.Count;
        int healthyGrass = 0;
        int cutGrass = 0;
        
        foreach (var health in grassHealthMap.Values)
        {
            if (health > 0.5f)
                healthyGrass++;
            else
                cutGrass++;
        }
        
        return new GrassStats
        {
            totalGrass = totalGrass,
            healthyGrass = healthyGrass,
            cutGrass = cutGrass
        };
    }
}

[System.Serializable]
public struct GrassStats
{
    public int totalGrass;
    public int healthyGrass;
    public int cutGrass;
    
    public float healthPercentage
    {
        get { return totalGrass > 0 ? (float)healthyGrass / totalGrass : 0f; }
    }
}
