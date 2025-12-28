using UnityEngine;

[System.Serializable]
public class Room
{
    public RectInt rect;
    public RoomType type;

    public Vector2Int Center =>
        new Vector2Int(
            rect.x + rect.width / 2,
            rect.y + rect.height / 2
        );

    public Room(RectInt rect)
    {
        this.rect = rect;
        type = RoomType.Combat;//일단 기본은 전투방
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
