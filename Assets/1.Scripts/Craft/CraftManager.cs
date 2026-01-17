using System.Collections.Generic;
using UnityEngine;

public class CraftManager : MonoBehaviour
{
    public static CraftManager instance;

    [SerializeField] private CraftRecipeContainer[] recipeContainer;

    [SerializeField] private GameObject craftCardParent;
    [SerializeField] private CraftCard cradtCardPrefab;

    private Dictionary<string, CraftRecipe> recipeDic = new Dictionary<string, CraftRecipe>();
    private Dictionary<string, CraftCard> cardDic = new Dictionary<string, CraftCard>();

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        foreach(CraftRecipeContainer container in recipeContainer)
        {
            foreach(CraftRecipe recipe in container.craftRecipeContainer)
            {
                recipeDic.Add(recipe.ID, recipe);
            }
        }
        
        InitCraftCard();

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void InitCraftCard()
    {
        if(craftCardParent != null)
        {
            if(cradtCardPrefab != null)
            {
                foreach (KeyValuePair<string, CraftRecipe> pair in recipeDic)
                {
                    CraftRecipe recipe = pair.Value;

                    CraftCard card = Instantiate(cradtCardPrefab, craftCardParent.transform);
                    card.SetCraftRecipe(recipe);

                    cardDic.Add(recipe.ID, card);
                }

                cradtCardPrefab.gameObject.SetActive(false);
            }
        }

        
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public CraftRecipe GetCraftRecipeByID(string ID)
    {
        if(recipeDic.TryGetValue(ID, out CraftRecipe outValue))
        {
            return outValue;
        }
        else
        {
            return null;
        }        
    }

    public Dictionary<string, CraftCard> GetCraftCards()
    {
        return cardDic;
    }
}
