using UnityEngine;

//캐릭터 기본
[CreateAssetMenu(fileName = "CharacterInfo", menuName = "Scriptable Objects/CharacterInfo")] 
public class CharacterInfo : ScriptableObject
{
    public int ID;
    public Status status;
    public Sprite thumnail;
    public string characterName;
    [TextArea(3, 5)]
    public string desc;
    
    public GameObject prefab;
}
