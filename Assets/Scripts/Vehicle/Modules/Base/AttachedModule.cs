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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

            if(newModule.TryGetComponent<VehicleWheel>(out VehicleWheel coll))
            {
                Debug.Log("바퀴 장착");
                OnAttach?.Invoke(coll.GetWheelCollider(), wheelType);
            }

            this.coll.enabled = false;
            isAttached = true;
        }
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
}
