using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "CraftRecipeContainer", menuName = "Scriptable Objects/CraftRecipeContainer")]
public class CraftRecipeContainer : ScriptableObject
{
    public CraftRecipe[] craftRecipeContainer;
}
