using UnityEngine;
using UnityEngine.VFX;

public class RoomDustEffect : MonoBehaviour
{
    [SerializeField] private VisualEffect roomDustEffect;

    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        roomDustEffect = GetComponent<VisualEffect>();

        roomDustEffect.SetVector3("RoomSize", boxCollider.size);
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
