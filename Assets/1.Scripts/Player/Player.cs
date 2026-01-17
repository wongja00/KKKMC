using Mirror;
using UnityEngine;

public class Player : CharacterBase
{
    [SerializeField] SkinnedMeshRenderer skinRederer;

    [SerializeField] KeyCode skillKey1 = KeyCode.E;
    readonly int rimEnable = Shader.PropertyToID("_Enable");
    readonly int rimColor = Shader.PropertyToID("_RimColor");
    readonly int rimIntensity = Shader.PropertyToID("_RimIntensity");
    readonly int rimPower = Shader.PropertyToID("_RimPower");

    private MaterialPropertyBlock mpb;

    private bool isBuff = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;
        
        mpb = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;


        if(Input.GetKeyDown(skillKey1))
        {
            isBuff = !isBuff;

            SetRim(isBuff, Color.blue, 3, 5);
        }
    }

    void OnEnable()
    {
        PlayerRegistry.Register(transform);
    }

    void OnDisable()
    {
        PlayerRegistry.Unregister(transform);
    }

    void SetRim(bool on, Color color, float intensity = 2f, float power = 3f)
    {
        if(skinRederer == null) return;

        skinRederer.GetPropertyBlock(mpb);

        mpb.SetFloat(rimEnable, on ? 1f : 0f);
        mpb.SetColor(rimColor, color);
        mpb.SetFloat(rimIntensity, intensity);
        mpb.SetFloat(rimPower, power);
        
        skinRederer.SetPropertyBlock(mpb);
    }


}
