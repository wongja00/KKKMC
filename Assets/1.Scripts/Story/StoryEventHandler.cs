using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

//캐릭터 전용 혹은 돌발 이벤트(상호작용 제외) 전용 스크립트 이벤트 관리 클래스
public class StoryEventHandler : MonoBehaviour
{
    [SerializeField] private List<string> storyEventIDs = new List<string>();

    Dictionary<string, List<string>> storyScripts = new Dictionary<string, List<string>>();

    public event Action<string> OnScriptEvent;

    private void Start()
    {
        //캐릭터 던전 입장시 스크립트 이벤트

    }

    void OnCharacterDungeonStart(string characterID)
    {
        //현재 씬 이름
        if(SceneManager.GetActiveScene().name.Contains("Dungeon"))
        {
            //딕셔너리에서 캐릭터 ID가 포함된 이벤트 ID 검색
            var scripts = StoryManager.Instance.GetAllScript();

        }
    }


}
