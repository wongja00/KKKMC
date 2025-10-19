using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;


public class InteractUIManager : MonoBehaviour
{
    public static InteractUIManager Instance;

    [SerializeField] TextMeshProUGUI InteractUIText;

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
}
