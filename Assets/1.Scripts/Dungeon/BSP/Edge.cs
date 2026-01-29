using UnityEngine;

public class Edge : MonoBehaviour
{
    public Room a;
    public Room b;
    public float cost;

    public Edge(Room a, Room b)
    {
        this.a = a;
        this.b = b;
        cost = Vector2Int.Distance(a.Center, b.Center);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
