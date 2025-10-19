using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class AttachedModule : MonoBehaviour, ModuleSlot
{

    [SerializeField] private ModuleType moduleType;

    [SerializeField] private SphereCollider coll;

    private WheelType wheelType;

    private bool isAttached;

    public event Action<WheelCollider, WheelType> OnAttach;

    //붙여진 모듈
    private GameObject childModule;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        childModule = null;

        coll = GetComponent<SphereCollider>();

        isAttached = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetModule(GameObject module)
    {
        if (module != null)
        {
            // 기존 모듈이 있다면 리턴
            if (transform.childCount > 0)
            {
                return;
            }
            
            // 새 모듈 인스턴스 생성
            GameObject newModule = Instantiate(module, transform.position, transform.rotation);
            newModule.SetActive(true);
            newModule.hideFlags = HideFlags.None;
            newModule.transform.SetParent(this.transform);
            newModule.transform.localPosition = Vector3.zero;
            newModule.transform.localRotation = Quaternion.identity;

            childModule = newModule;

            if(newModule.TryGetComponent<VehicleWheel>(out VehicleWheel coll))
            {
                OnAttach?.Invoke(coll.GetWheelCollider(), wheelType);
                coll.SetIsAttached(true);
                coll.OnDetach += DetachModule;
            }

            this.coll.enabled = false;
            isAttached = true;
        }
    }

    public void DetachModule()
    {
        if(childModule == null)
        {
            return;
        }

        // 기존 모듈이 없다면 리턴
        if (transform.childCount <= 0)
        {
            return;
        }

        if(childModule.TryGetComponent<VehicleWheel>(out VehicleWheel coll))
        {
            coll.OnDetach -= DetachModule;
        }
        
        childModule = null;
        this.coll.enabled = true;
        isAttached = false;
    }

    public ModuleType GetModuleType()
    {
        return moduleType;
    }

    public WheelType GetWheelType()
    {
        return wheelType;
    }

    public void SetWheelType(WheelType type)
    {
        wheelType = type;
    }

    public bool GetIsAttached()
    {
        return isAttached;
    } 

    public void SetIsAttached(bool isSet)
    {
        isAttached = isSet;
    }
}
