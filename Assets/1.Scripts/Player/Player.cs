using Mirror;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class Player : CharacterBase
{
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] CombatSystem combat;
    [SerializeField] PlayerBuffSystem buffSystem;
    [SerializeField] Animator animator;
    [SerializeField] SkinnedMeshRenderer skinRederer;
    
    //[Header("UI")]
    private PlayerHP playerHP;
    private BuffUI buffUI;
    private StatusUI statusUI;

    [SerializeField] KeyCode skillKey1 = KeyCode.Alpha1;
    readonly int rimEnable = Shader.PropertyToID("_Enable");
    readonly int rimColor = Shader.PropertyToID("_RimColor");
    readonly int rimIntensity = Shader.PropertyToID("_RimIntensity");
    readonly int rimPower = Shader.PropertyToID("_RimPower");

    private MaterialPropertyBlock mpb;

    private bool isBuff = false;

    public event Action OnInteract;

    public KeyCode InteractkeyCode = KeyCode.E;

    public LayerMask layer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {

    }
    void Start()
    {        
        CharacterStartInit();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        buffSystem.OnApplyBuff += (BuffType type, float value)=>{if(buffSystem.GetselectableBuffCount() > 0)ApplyBuff(type, value);};
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        //NetworkManager.instance.OnClientSceneChangedEvent += CmdReplaceCharacter;
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

        if(Input.GetKeyDown(InteractkeyCode))
        {
            Interactcmd();
        }
    }

    public void CharacterStartInit()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<PlayerModel>().animator;
        }

        if (skinRederer == null)
        {
            skinRederer = GetComponentInChildren<PlayerModel>().meshRenderer;
        }

        if (!isLocalPlayer) return;

        playerHP = InteractUIManager.Instance.GetHpUI();
        buffUI = InteractUIManager.Instance.buffUI;
        statusUI = InteractUIManager.Instance.statusUI;

        OnHpChanged += Hpchange;
        OnGetBuff += buffUI.AddBuffCount;
        OnStatChanged += OnStatChangeUI;

        buffSystem.OnSelectBuff += buffUI.SetBuffSelectCount;
        OnGetBuffServer += buffSystem.DecreaseSelectBuffCount;

        mpb = new MaterialPropertyBlock();

        OnApplyBuff();
        SetStatUI();
        OnStatChangeUI();
    }

    public void SetStatUI()
    {
        if(statusUI == null) return;

        // Status 구조체의 각 멤버를 StatusUI에 카드로 추가
        statusUI.statDic.Clear();

        statusUI.AddCards("레벨", Level);
        statusUI.AddCards("현재 경험치", curExp);
        statusUI.AddCards("최대 경험치", maxExp);
        statusUI.AddCards("현재 체력", (int)CurHP);
        statusUI.AddCards("최대 체력", (int)CurHP);
        statusUI.AddCards("속도", (int)speed);

        statusUI.AddCards("힘", stat.strength);
        statusUI.AddCards("민첩", stat.agility);
        statusUI.AddCards("지능", stat.intelligence);
        statusUI.AddCards("방어력", stat.defense);
        statusUI.AddCards("치명타 확률(%)", (int)(stat.critChance*100));
        statusUI.AddCards("치명타 데미지(%)", (int)(stat.critDamage*100));
        statusUI.AddCards("공격 속도", (int)stat.attackSpeed);
        
        statusUI.DisableOriginCard();
    }

    public void OnStatChangeUI()
    {
        if(statusUI == null) return;

        statusUI.statDic["힘"].SetValue(stat.strength);
        statusUI.statDic["민첩"].SetValue(stat.agility);
        statusUI.statDic["지능"].SetValue(stat.intelligence);
        statusUI.statDic["방어력"].SetValue(stat.defense);
        statusUI.statDic["치명타 확률(%)"].SetValue( (int)(stat.critChance*100));
        statusUI.statDic["치명타 데미지(%)"].SetValue((int)(stat.critDamage*100));
        statusUI.statDic["공격 속도"].SetValue((int)stat.attackSpeed);

        Debug.Log("UI 업데이트");
    }

    void UpdateStatusUI(String statName, int Value)
    {
        statusUI.statDic[statName].SetValue(Value);
    }

    [Command]
    void OnApplyBuff()
    {
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

    public void Interactcmd()
    {
        OnInteract?.Invoke();

        Collider[] hits = Physics.OverlapSphere(transform.position, 3f, layer);

        if(hits.Length > 0)
        {
            foreach(Collider hit in hits)
            {
                NetworkIdentity id = hit.GetComponent<NetworkIdentity>();

                if(id == null) continue;

                Interact(id);
            }
        }
    }

    
    [Command]
    public void Interact(NetworkIdentity id)
    {
        if(id == null) return;
        
        if(id.GetComponent<Interactable>() != null)
        {
        }

        id.GetComponent<Interactable>()?.Interact();
    }

    [Server]
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        Debug.Log($"데미지: {damage}");

        if(CurHP <= 0)
        {
            Die();
        }

    }

    [Server]
    void Die()
    {
        if(isDead) return;

        isDead = true;

        playerMovement.enabled = false;

        combat.EndAttack();

        combat.enabled = false;

        DieAnimation();
    }

    [ClientRpc]
    void DieAnimation()
    {
        animator.SetTrigger("isDead");
    }

    void Hpchange()
    {
        playerHP.SetHPImage(CurHP, MaxHP);

        statusUI.statDic["현재 체력"].SetValue((int)CurHP);
        statusUI.statDic["최대 체력"].SetValue((int)MaxHP);
    }


    [Command]
    public void CmdReplaceCharacter(int ID)
    {
        
        NetworkManager.instance.CharacterReplace(ID, connectionToClient);
        TargetCloseChoiceUI(connectionToClient);
        TargetSetCurCharacterID(connectionToClient, ID);
    } 
    [TargetRpc]
    void TargetCloseChoiceUI(NetworkConnection network)
    {
        NetworkManager.instance.CloseChoiceUI();
    }
    
    [TargetRpc]
    void TargetSetCurCharacterID(NetworkConnectionToClient conn, int ID)
    {
        NetworkManager.instance.SetCurCharacterID(ID);
    }
}
