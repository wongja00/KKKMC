using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine.Animations.Rigging;

public class HandHeld : NetworkBehaviour
{
    [SerializeField] Transform handHeldPoint;
    [SerializeField] InventorySystem inventorySystem;
    [SerializeField] HotBarSlotsController hotBarSlotsController;
    [SerializeField] TextMeshProUGUI magText;
    [SerializeField] Aiming aiming;
    [SerializeField] Rig AimingRig;
    
    [SerializeField] private Animator animator;

    [SerializeField] private KeyCode itemUseKey = KeyCode.Mouse0;

    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private Transform rightHandIKPoint;
    [SerializeField] private Transform leftHandIKPoint;
    private Dictionary<int, GameObject> HotBarPrefabs;

    private GameObject curObjectItem;

    public event Action OnUseItem;

    private GunBase curGun;
    private Quaternion gunOriginRot;

    private float ikWeightVelocity;
    private Quaternion targetGunRotation;
    private bool isTransitionAim = false;
    private float aimTransitionSpeed = 5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;

        magText = InteractUIManager.Instance.GetGunMagText();
        
        InventorySystem.OnInventoryChanged += UpdateHandPrefabs;
        HotBarSlotsController.OnChangeSlot += ShowCurItem;

        HotBarPrefabs = new Dictionary<int, GameObject>();


    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;

        UseItem();

        SetIKPos();

        //총 보간
        //GunRoatateLinear();
    }

    void LateUpdate()
{
    if(!isLocalPlayer) return;
    
    // Animator가 업데이트된 후에 IK 적용
    if(curGun != null)
    {
        //leftHandIKPoint.position = curGun.leftHandIKPoint.position;
        //leftHandIKPoint.rotation = curGun.leftHandIKPoint.rotation;
        
        //rightHandIKPoint.position = curGun.rightHandIKPoint.position;
        //rightHandIKPoint.rotation = curGun.rightHandIKPoint.rotation;

        //위치와 회전을 보간하여 부드럽게 전화
        leftHandIKPoint.position = Vector3.Lerp(
            leftHandIKPoint.position,
        curGun.leftHandIKPoint.position,
        Time.deltaTime * 15f);

        leftHandIKPoint.rotation = Quaternion.Slerp(
            leftHandIKPoint.rotation,
        curGun.leftHandIKPoint.rotation,
        Time.deltaTime * 15f);

       //        rightHandIKPoint.position = Vector3.Lerp(
       //    rightHandIKPoint.position,
       //curGun.rightHandIKPoint.position,
       //Time.deltaTime * 15f);

       //rightHandIKPoint.rotation = Quaternion.Slerp(
       //    rightHandIKPoint.rotation,
       //curGun.rightHandIKPoint.rotation,
       //Time.deltaTime * 15f);
    }
}

    void UpdateHandPrefabs(Item item, int amount)
    {
        SetHandItems();
    }

    GameObject GetCurItem(int cu)
    {
        if(HotBarPrefabs.TryGetValue(cu, out GameObject item))
        {
            return item;
        }
        else
        {
            return null;
        }

    }

    void ShowCurItem()
    {
        if(!isLocalPlayer) return;

        if(curObjectItem != null)
        {
            curObjectItem.SetActive(false);
        }

        GameObject curObj = GetCurItem(hotBarSlotsController.curSelSlot);
        
        OnUseItem = null;

       // AnimLayer enum의 값(0: Base 제외)만큼 animator의 레이어 웨이트를 0으로 초기화
       for (int i = 1; i < Enum.GetValues(typeof(AnimLayer)).Length; i++)
       {
           animator.SetLayerWeight(i, 0f);
            leftHandIK.weight = 0;
            rightHandIK.weight = 0;
            AimingRig.weight = 0f;
       }

        if(curObj == null)
        {
            magText.gameObject.SetActive(false);
            return;
        } 
            
        curObjectItem = curObj;
        curObjectItem.SetActive(true);

        var toolItem = curObjectItem.GetComponent<Equable>();
        
        if (toolItem != null)
        {

            toolItem.SetIsEquipped(true);
            OnUseItem+=toolItem.UseItem;

            if (toolItem is GunBase gunBase)
            {
                // GunBase인 경우 탄창 정보를 magText에 표시
                if (magText != null)
                {

                    magText.text = gunBase.CurMag();
                    magText.gameObject.SetActive(true);
                    magText.text = gunBase.CurMag();
                    OnUseItem += () => {magText.text = gunBase.CurMag();};

                    aiming.curGun = gunBase;
                    gunBase.SetAimingCompo(aiming);

                    if(gunBase.gunData.type == GunType.AssultRifle)
                    {
                        animator.SetLayerWeight((int)AnimLayer.Rifle, 1f);

                        //leftHandIK.data.target = gunBase.leftHandIKPoint;
                        //rightHandIK.data.target = gunBase.rightHandIKPoint;

                        leftHandIK.weight = 1f;
                        AimingRig.weight = 0.9f;
                        //rightHandIK.weight = 0.9f;

                        curGun = gunBase;
                        gunOriginRot = gunBase.transform.localRotation;
                    }
                    else
                    {
                        curGun = null;
                    }
                }
            }
            else
            {
                magText.gameObject.SetActive(false);
                curGun = null;
                
            }

            inventorySystem.SetCurItme(curObjectItem);
        }
        else
        {
            magText.gameObject.SetActive(false);
        }
    }

    private void SetIKPos()
    {
        if(curGun != null)
        {

            //Debug.Log("총");
        }
    }

    void SetHandItems()
    {
        if(!isLocalPlayer) return;

        HotBarPrefabs.Clear();

        for(int i = 0; i < inventorySystem.hotBarSlots.Count; i ++)
        {
            if(inventorySystem.hotBarSlots[i].GetSlotType() != SlotType.NullSlot)
            {
                GameObject slotItemPrefab = inventorySystem.hotBarSlots[i].itemData.itemPrefab;
                GameObject slotItem = Instantiate(slotItemPrefab, handHeldPoint);
                
                slotItem.transform.localPosition = Vector3.zero;
                slotItem.transform.localScale = Vector3.one;
                slotItem.transform.localRotation = Quaternion.identity;

                //slotItem.transform.localPosition = Vector3.zero;
                //slotItem.transform.localScale = Vector3.one;
                //slotItem.transform.localRotation = Quaternion.identity;

                HotBarPrefabs.Add(i, slotItem);

                if (curObjectItem != null)
                {
                    var toolItem = curObjectItem.GetComponent<Equable>();
                    if (toolItem != null)
                    {
                        toolItem.SetIsEquipped(false);
                    }
                }

                slotItem.SetActive(false);
            }
        }

        ShowCurItem();
    }

    void UseItem()
    {
        if(!isLocalPlayer || curObjectItem == null) return;

        // 총(Equable && GunBase)이고 자동사격(isAutomatic)이 true면 꾹 누를 때 사용, 아니면 한 번만 사용
        var gunBase = curObjectItem?.GetComponent<GunBase>();
        bool isAutomaticGun = false;
        if (gunBase != null && gunBase.gunData != null)
        {
            isAutomaticGun = gunBase.gunData.isAutomatic;
        }

        if (isAutomaticGun)
        {
            if (Input.GetKey(itemUseKey))
            {
                Debug.Log("연발");
                OnUseItem?.Invoke();
                aiming.isADS = true;
            }
            else
            {
                aiming.isADS = false;
            }
        }
        else
        {
            if (Input.GetKeyDown(itemUseKey))
            {
                OnUseItem?.Invoke();
                aiming.isADS = true;
            }
            else if(Input.GetKeyDown(itemUseKey))
            {
                aiming.isADS = false;
            }
        }



    }

    void SetGunMagagine()
    {

    }

    public void GunAiming(Transform target)
    {
        if(curGun == null) return;

        Vector3 direction = target.position - curGun.firePoint.transform.position;

        if(direction.magnitude > 0.01f)
        {
            curGun.transform.rotation = Quaternion.LookRotation(direction);
        }

        //targetGunRotation = Quaternion.LookRotation(target.position - curGun.transform.position);
        
        isTransitionAim = false;
        //gunOriginRot = curGun.transform.rotation;
        //curGun.transform.LookAt(target);

    }
    public void SetOriginAim()
    {
        if(curGun == null) return;

        //curGun.transform.localRotation = gunOriginRot;
        targetGunRotation = gunOriginRot;
        isTransitionAim = true;
    }

    //총 회전 보간 함수
    private void GunRoatateLinear()
    {
        if(curGun != null && isTransitionAim == true)
        {
            curGun.transform.localRotation = Quaternion.Slerp(curGun.transform.localRotation,
            targetGunRotation,
            Time.deltaTime * aimTransitionSpeed);

            
            //회전이 거의 일치하면 전환 완료
            if(Quaternion.Angle(curGun.transform.localRotation, targetGunRotation) < 0.1f)
            {
                curGun.transform.localRotation = targetGunRotation;
                isTransitionAim = false;
            }
        }

        //IK Weight 보간(조준 상태에 따라)
        float targetIKWeight = aiming.isAiming ? 0.9f : 0.9f; //필요시 조정
        float currentLeftWeight = Mathf.SmoothDamp(
            leftHandIK.weight,
            targetIKWeight,
            ref ikWeightVelocity,
            0.2f
        );

        leftHandIK.weight = currentLeftWeight;
        //rightHandIK.weight = currentLeftWeight;


    } 
}
