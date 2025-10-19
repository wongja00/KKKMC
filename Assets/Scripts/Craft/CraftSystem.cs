using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class CraftSystem : MonoBehaviour
{
    [Header("제작키")]
    [SerializeField] private KeyCode craftKey = KeyCode.G;

    [Header("UI")]
    [SerializeField] private GameObject craftUI;

    [Header("제작 슬롯")]
    [SerializeField] private List<CraftCard> craftCardList = new List<CraftCard>();

    [Header("시스템 참조")]
    [SerializeField] private InventorySystem inventorySystem;

    private Dictionary<string, CraftCard> cachedCraftCards = null;

    private CraftRecipe selectRecipe;
    private bool isCrafting = false;

    public event Action OnCraftItem;
    public event Action EndCraftItem;

    public static event Action<string, int> OnInventoryChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InventorySystem.OnInventoryChanged += OnChangeItem;
        AddToEventCard();
    }

    // Update is called once per frame
    void Update()
    {
        ToggleCraftUI();
    }

    private void ToggleCraftUI()
    {
        if(Input.GetKeyDown(craftKey))
        {
            if(craftUI.activeSelf)
            {
                CloseCraftUI();
            }
            else
            {
                OpenCraftUI();
            }
        }
    } 

    public void OpenCraftUI()
    {
        craftUI.SetActive(true);
        RefreshIngredientsAmounts();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    } 

    public void CloseCraftUI()
    {
        craftUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SelectRecipe(CraftRecipe recipe)
    {
        selectRecipe = recipe;
    }


    public void StartCrafting()
    {
        if(CanCraft(selectRecipe))
        {
            StartCoroutine(CraftItemCoroutine(selectRecipe));
        }
    }

    private bool CanCraft(CraftRecipe recipe)
    {
        foreach(var ingredient in recipe.ingredients)
        {
            if(!inventorySystem.HasItem(ingredient.item, ingredient.amount))
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator CraftItemCoroutine(CraftRecipe recipe)
    {
        isCrafting = true;
        OnCraftItem?.Invoke();
        //재료 소모
        foreach(var ingredient in recipe.ingredients)
        {
            inventorySystem.RemoveItem(ingredient.item, ingredient.amount);
        }

        //제작 대기 시간 
        yield return new WaitForSeconds(recipe.craftTime);

        //걀과물 추가
        inventorySystem.AddItem(recipe.resultItemData, recipe.resultAmount);
        EndCraftItem?.Invoke();
    }

    private void OnChangeItem(Item item, int amount)
    {
        OnInventoryChanged?.Invoke(item.GetItemId(), amount);
    }

    private void AddToEventCard()
    {
        Dictionary<string, CraftCard> dic = new Dictionary<string, CraftCard>();
        dic = CraftManager.instance.GetCraftCards();

        foreach(KeyValuePair<string, CraftCard> keyvalue in dic)
        {
            CraftCard card = keyvalue.Value;

            card.OnCraftRequested += OnCraftItemReceive;
            card.OnLoading += LoadingCraftTime;

            OnCraftItem += card.CraftingItem;
            EndCraftItem += card.CraftingEnd;
            OnInventoryChanged += card.UpdateIngredientAmounts;
        }
    }

    private void OnCraftItemReceive(CraftCard card, CraftRecipe recipe)
    {
        SelectRecipe(recipe);

        StartCrafting();
    }

    private void LoadingCraftTime(Image image)
    {
        StartCoroutine(CraftingLoadingCoroutine(image));
    }

    private IEnumerator CraftingLoadingCoroutine(Image image)
    {
        if(selectRecipe == null) yield break;

        float duration = selectRecipe.craftTime;
        float elapsed = 0f;
        image.fillAmount = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            image.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        image.fillAmount = 0f;
    }

    public void RefreshIngredientsAmounts()
    {
        if(cachedCraftCards == null)
        {
            cachedCraftCards = CraftManager.instance.GetCraftCards();
        }

        foreach(var card in cachedCraftCards.Values)
        {
            if(card != null && card.craftRecipe != null)
            {
                foreach(var ingredient in card.craftRecipe.ingredients)
                {
                    int currentAmount = inventorySystem.GetItemCount(ingredient.item.itemId);
                    card.UpdateIngredientAmounts(ingredient.item.itemId, currentAmount);
                }
            }
        }
    }
}
