using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using Unity.VisualScripting;

public class CraftCard : MonoBehaviour
{
    //제작템 이름
    [SerializeField] private TextMeshProUGUI nameText;
    
    //템설명
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image thumnail;

    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private IngreDientText ingreSample;

    private Dictionary<string, IngreDientText> ingredientsData =  new  Dictionary<string, IngreDientText>();
     
    
    [SerializeField] private Transform ingredientsParent;
    [SerializeField] private Image craftingLoading;

    public CraftRecipe craftRecipe;

    private bool canCraft = false;

    public event Action<CraftCard, CraftRecipe> OnCraftRequested;
    public event Action<Image> OnLoading;

    
    void Awake()
    {

        IngredientsDataInit();
        InitNameDesc();

        craftButton.onClick.AddListener(CraftStuff);

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void IngredientsDataInit()
    {
        foreach(RecipeIngredient ingredient in craftRecipe.ingredients)
        {
            if(ingredientsParent == null || ingreSample == null) return;

            IngreDientText ingre = Instantiate(ingreSample, ingredientsParent);

            ingre.SetNameText(ingredient.item.itemName);
            ingre.SetAmountText(ingredient.amount);
            ingredientsData.Add(ingredient.item.itemId, ingre);

        }

        ingreSample.gameObject.SetActive(false);
    }

    private void InitNameDesc()
    {
        nameText.text = craftRecipe.resultItemData.itemName;
        descriptionText.text = craftRecipe.resultItemData.description;

        thumnail.sprite = craftRecipe.resultItemData.itemIcon;
    }

    public void SetCraftRecipe(CraftRecipe recipe)
    {
        this.craftRecipe = recipe;
    }
    
    public void CraftStuff()
    {
        OnCraftRequested?.Invoke(this, craftRecipe);
    }

    public void UpdateIngredientAmounts(string ID, int amount)
    {
        if(ingredientsData.TryGetValue(ID, out IngreDientText text))
        {
            text.SetCurAmountText(amount);
        }
    } 
    
    public void CraftingItem()
    {
        craftButton.interactable = false;

        buttonText.text = "제작중";

        OnLoading?.Invoke(craftingLoading);
    }

    public void CraftingEnd()
    {
        craftButton.interactable = true;

        buttonText.text = "제작";
    }

    public void OnEnable()
    {
    }

}


