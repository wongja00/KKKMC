using UnityEngine;
using TMPro;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CraftRecipe", menuName = "Scriptable Objects/CraftRecipe")]
public class CraftRecipe : ScriptableObject
{
    [Header("레시피 아이디")]
    public string ID;

    [Header("제작 결과물")]
    public ScriptableItemData resultItemData;
    public int resultAmount = 1;

    [Header("필요 재료")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    [Header("제작 조건")]
    public int requiredLevel = 1;
    public bool isUnlocked = true;

    [Header("제작 시간")]
    public float craftTime = 1f;
    
}

[System.Serializable]
public class RecipeIngredient
{
    public ScriptableItemData item;
    public int amount;
}
