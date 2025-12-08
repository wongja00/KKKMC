using UnityEngine;
using Unity.Cinemachine;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.VFX;
using Mirror;

public class GunBase : NetworkBehaviour, Item, Equable
{
    public GunDataScriptableObject gunData;

    [SerializeField] private KeyCode gunFireKey = KeyCode.Mouse0;

    [SerializeField] private VisualEffect muzzleFalsh;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireClip;

    [SerializeField] private int currentAmmo = 0;

    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] public Transform leftHandIKPoint;
    [SerializeField] public Transform rightHandIKPoint;
    [SerializeField] private Transform aimTarget;

    private bool isReloading = false;
    private float lastFireTime = 0f;
    private Aiming aiming;

    //총알 프리팹
    public GameObject bulletPrefab;
    public BulletDecal bulletDecalPrefab;
    //총알이 발사될 위치(총구)
    public Transform firePoint;

    [SerializeField] ScriptableItemData scriptableItemData;

    private int curCount = 0;
    private bool isAttached = false;

    private string curMagText;

    [SerializeField] private bool isEquipped = false;

    [SerializeField] private LayerMask shootLayerMask;
    public event Action OnUseItem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        currentAmmo = gunData.maxAmmo;
    }
    void Start()
    {
        currentAmmo = gunData.maxAmmo;
        lastFireTime = 0;
        curMagText = $"{currentAmmo} / {gunData.maxAmmo}";
    }

    // Update is called once per frame
    void Update()
    {
        if(isReloading || !isEquipped) return;
        
        if(currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }

        if(Input.GetKeyDown(gunFireKey))
        {
            OnUseItem?.Invoke();
        }
    }

    public void SetAimingCompo(Aiming aim)
    {
        this.aiming = aim;
        aimTarget = aim.aimTarget;
    }

    public void Shoot()
    {
        if(Time.time - lastFireTime < gunData.fireRate || currentAmmo <= 0) return;

        lastFireTime = Time.time;

        currentAmmo--;
        curMagText = $"{currentAmmo} / {gunData.maxAmmo}";

        ShotBullet();

    }

    private void ShotBullet()
    {
        //aimTarget이 설정되어 있고 조준중이면 aimTarget 방향 사용
        // 그렇지 않으면 카메라 방향 사용
        Vector3 shootDirection;

        if(aimTarget != null && aiming != null)
        {
            //firePoint 에서 aimTarget으로의 방향 계산
            shootDirection = (aimTarget.position - firePoint.transform.position).normalized;
        }
        else
        {
            //기본갑: firePoint의 forward
            shootDirection = firePoint.transform.forward;
        }


        Ray ray = new Ray(firePoint.transform.position, shootDirection);

        muzzleFalsh.Play();
        CmdShoot();
        PlayShootSoundLocal();

        if(Physics.Raycast(ray, out RaycastHit hit, 100f, shootLayerMask))
        {
            if (hit.collider.transform.root == this.transform.root) 
                return;

            // 맞은 collider의 EnemyBase를 찾아서, 맞았다면 데미지를 준다.
            CharacterBase enemy = hit.collider.GetComponentInParent<CharacterBase>();
            if(enemy != null)
            {
                enemy.TakeDamage((int)gunData.damage);
            }


            //데칼
            Vector3 decalForward = Vector3.forward; // 또는 -Vector3.forward, Vector3.up 등

            Quaternion rot = Quaternion.FromToRotation(decalForward, hit.normal);

            BulletDecal decal = Instantiate(bulletDecalPrefab, hit.point + hit.normal * 0.001f,rot);

            decal.transform.SetParent(hit.collider.transform);

            Vector3 euler = decal.transform.eulerAngles;

            // Y축이 270도 (또는 -90도)인지 확인
            if (Mathf.Abs(euler.y - 270f) < 5f || Mathf.Abs(euler.y - 90f) < 5f)
            {
                decal.transform.rotation = Quaternion.Euler(euler.x, euler.y + 180f, euler.z);
            }
            // Y축이 180도 (또는 -180도)인지 확인
            if (Mathf.Abs(euler.y - 180f) < 5f)
            {
                decal.transform.rotation = Quaternion.Euler(euler.x, 0f, euler.z);
            }

            decal.SetDecalImage();
        }

    }


    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(gunData.reloadTime);
        currentAmmo = gunData.maxAmmo;
        isReloading = false;
    }

    public string GetName()
    {
        return scriptableItemData.itemName;
    }

    public string GetDescription()
    {
        return scriptableItemData.description;
    }

    public int GetCount()
    {
        return curCount;
    }

    public void SetCount(int count)
    {
        curCount = count;
    }

    public ItemType GetItemType()
    {
        return scriptableItemData.itemType;
    }

    public bool IsStackable()
    {
        return scriptableItemData.isStackable;
    }

    public bool IsAttached()
    {
        return isAttached;
    }

    public void SetIsAttached(bool set)
    {
        isAttached = set;
    }

    public int GetMaxStackSize()
    {
        return scriptableItemData.maxStackSize;
    }

    public Sprite GetIcon()
    {
        return scriptableItemData.itemIcon;
    }

    public string GetItemId()
    {
        return scriptableItemData.itemId;
    }

    public void SetItemData(ScriptableItemData itemData)
    {
        scriptableItemData = itemData;
    }

    public GameObject GetObject()
    {
        return this.gameObject;
    }

    public ScriptableItemData GetItemData()
    {
        return scriptableItemData;
    }

    public bool GetIsEquipped()
    {
        return isEquipped;
    }

    public void SetIsEquipped(bool isEquipped)
    {
        this.isEquipped = isEquipped; 
    }

    public KeyCode GetKeyCode()
    {
        return gunFireKey;
    }

    public void UseItem()
    {
        Shoot();
    }

    public string CurMag()
    {
        curMagText = $"{currentAmmo} / {gunData.maxAmmo}";

        return curMagText;
    }

    [Command]
    private void CmdShoot()
    {
        RpcPlayFireSound();
    }

    [ClientRpc(includeOwner = false)]
    void RpcPlayFireSound()
    {
        PlayShootSoundLocal();
    }

    private void PlayShootSoundLocal()
    {
        audioSource.PlayOneShot(fireClip);
    }
}
