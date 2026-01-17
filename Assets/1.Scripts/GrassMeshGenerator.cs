using UnityEngine;

public class GrassMeshGenerator : MonoBehaviour
{
    [Header("Grass Blade Settings")]
    public float bladeHeight = 1f;
    public float bladeWidth = 0.1f;
    public int segments = 3;
    public bool createMeshAsset = true;
    
    [Header("Mesh Settings")]
    public string meshName = "GrassBlade";
    
    void Start()
    {
        if (createMeshAsset)
        {
            CreateGrassBladeMesh();
        }
    }
    
    [ContextMenu("Create Grass Blade Mesh")]
    public void CreateGrassBladeMesh()
    {
        Mesh grassMesh = GenerateGrassBladeMesh();
        
        if (createMeshAsset)
        {
            #if UNITY_EDITOR
            string path = "Assets/Meshes/" + meshName + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(grassMesh, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Grass blade mesh saved to: {path}");
            #endif
        }
        
        // Assign to mesh filter if available
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = grassMesh;
        }
    }
    
    public Mesh GenerateGrassBladeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        
        // Calculate vertices
        int vertexCount = (segments + 1) * 2; // Two vertices per segment
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        
        // Calculate triangles
        int triangleCount = segments * 2; // Two triangles per segment
        int[] triangles = new int[triangleCount * 3];
        
        // Generate vertices
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float height = t * bladeHeight;
            float width = bladeWidth * (1f - t * 0.5f); // Taper towards top
            
            // Left vertex
            vertices[i * 2] = new Vector3(-width * 0.5f, height, 0f);
            uvs[i * 2] = new Vector2(0f, t);
            normals[i * 2] = new Vector3(-1f, 0f, 0f);
            
            // Right vertex
            vertices[i * 2 + 1] = new Vector3(width * 0.5f, height, 0f);
            uvs[i * 2 + 1] = new Vector2(1f, t);
            normals[i * 2 + 1] = new Vector3(1f, 0f, 0f);
        }
        
        // Generate triangles
        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * 2;
            
            // First triangle
            triangles[triangleIndex++] = baseIndex;
            triangles[triangleIndex++] = baseIndex + 1;
            triangles[triangleIndex++] = baseIndex + 2;
            
            // Second triangle
            triangles[triangleIndex++] = baseIndex + 1;
            triangles[triangleIndex++] = baseIndex + 3;
            triangles[triangleIndex++] = baseIndex + 2;
        }
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        
        // Recalculate bounds and normals
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        
        return mesh;
    }
    
    // Create a simple quad grass blade (alternative)
    public Mesh CreateSimpleGrassBlade()
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName + "_Simple";
        
        // Simple quad vertices
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-bladeWidth * 0.5f, 0f, 0f),
            new Vector3(bladeWidth * 0.5f, 0f, 0f),
            new Vector3(-bladeWidth * 0.25f, bladeHeight, 0f),
            new Vector3(bladeWidth * 0.25f, bladeHeight, 0f)
        };
        
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        
        int[] triangles = new int[]
        {
            0, 1, 2,
            1, 3, 2
        };
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    // Create cross-shaped grass blade (better for wind effects)
    public Mesh CreateCrossGrassBlade()
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName + "_Cross";
        
        // Cross shape vertices (two quads perpendicular to each other)
        Vector3[] vertices = new Vector3[]
        {
            // First quad (X direction)
            new Vector3(-bladeWidth * 0.5f, 0f, 0f),
            new Vector3(bladeWidth * 0.5f, 0f, 0f),
            new Vector3(-bladeWidth * 0.25f, bladeHeight, 0f),
            new Vector3(bladeWidth * 0.25f, bladeHeight, 0f),
            
            // Second quad (Z direction)
            new Vector3(0f, 0f, -bladeWidth * 0.5f),
            new Vector3(0f, 0f, bladeWidth * 0.5f),
            new Vector3(0f, bladeHeight, -bladeWidth * 0.25f),
            new Vector3(0f, bladeHeight, bladeWidth * 0.25f)
        };
        
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        
        int[] triangles = new int[]
        {
            // First quad
            0, 1, 2, 1, 3, 2,
            // Second quad
            4, 5, 6, 5, 7, 6
        };
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw grass blade preview
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float height = t * bladeHeight;
            float width = bladeWidth * (1f - t * 0.5f);
            
            Vector3 left = new Vector3(-width * 0.5f, height, 0f);
            Vector3 right = new Vector3(width * 0.5f, height, 0f);
            
            if (i > 0)
            {
                Gizmos.DrawLine(left, right);
            }
            
            if (i < segments)
            {
                float nextT = (float)(i + 1) / segments;
                float nextHeight = nextT * bladeHeight;
                float nextWidth = bladeWidth * (1f - nextT * 0.5f);
                
                Vector3 nextLeft = new Vector3(-nextWidth * 0.5f, nextHeight, 0f);
                Vector3 nextRight = new Vector3(nextWidth * 0.5f, nextHeight, 0f);
                
                Gizmos.DrawLine(left, nextLeft);
                Gizmos.DrawLine(right, nextRight);
            }
        }
    }
}
