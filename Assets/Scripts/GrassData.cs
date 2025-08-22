using UnityEngine;

[System.Serializable]
public struct GrassData
{
    public Vector3 position;
    public Vector3 normal;
    public float height;
    public float width;
    public float rotation;
    public float windOffset;
    public float health; // 0-1, 베기/심기용
}
