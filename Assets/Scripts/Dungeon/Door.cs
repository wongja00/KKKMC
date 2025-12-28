using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField]
    private Collider col;

    public void Open()
    {
        col.enabled = false;
    }

    public void Close()
    {
        col.enabled = true;
    }
}
