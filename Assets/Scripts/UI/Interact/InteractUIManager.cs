using UnityEngine;
using System;
using Unity.Cinemachine;
using UnityEngine.UI;
using TMPro;


public class InteractUIManager : MonoBehaviour
{
    public static InteractUIManager Instance;

    [SerializeField] TextMeshProUGUI InteractUIText;
    [SerializeField] TextMeshProUGUI GunUIText;
    [SerializeField] Image aimCrosshair;
    [SerializeField] RectTransform aimCrosshairRect;
    [SerializeField] Camera cam;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(aimCrosshair !=null)
        {
            aimCrosshair.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetInteractUIText(string text)
    {
        if(InteractUIText == null)
            return;

        InteractUIText.text = text;
    }

    public void ActiveTextUI(bool isActive)
    {
        InteractUIText.gameObject.SetActive(isActive);
    }

    public TextMeshProUGUI GetGunMagText()
    {
        return GunUIText;
    }

    public void SetAimCrosshair(bool isActive)
    {
        if(aimCrosshair == null) return;

        aimCrosshair.gameObject.SetActive(isActive);
    }

    public void SetAimCrosshairPosition(Vector2 screenPosition)
    {
        if(aimCrosshair == null) return;

        //화면 좌표를 RectTransform의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(aimCrosshairRect.parent as RectTransform,
        screenPosition,
        null,
        out Vector2 localPoint);

        aimCrosshairRect.localPosition = localPoint;
    }

    public void SetAimCrosshairWorldPosition(Vector3 worldPosition)
    {
        if(aimCrosshairRect == null || cam == null) return;

        //월드 좌표를 화면 좌표로 변환
        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);

        if(screenPos.z < 0)
        {
            SetAimCrosshair(false);
            return;
        }

        SetAimCrosshair(true);
        SetAimCrosshairPosition(screenPos);

    }
}
