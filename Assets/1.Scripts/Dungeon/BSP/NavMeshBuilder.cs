using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBuilder : MonoBehaviour
{
    NavMeshSurface surface;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        surface = GetComponent<NavMeshSurface>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NavMeshbuilding()
    {
        surface.BuildNavMesh();
    }
}
