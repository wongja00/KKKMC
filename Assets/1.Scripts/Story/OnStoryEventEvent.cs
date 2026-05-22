using System;
using System.Collections.Generic;
using UnityEngine;

//들어오면 스토리 이벤트 발생시키는 오브젝트
public class OnStoryEventEvent : MonoBehaviour
{
    [SerializeField] private BoxCollider storyCollider;
    
    [SerializeField] string eventID;//스크립트 아이디

    [SerializeField] StoryEventCondition eventAction;


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("충돌 감지: " + other.gameObject.name);

        if (eventAction.defCondition == StoryEventAction.EnterCollider)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if(eventAction.extraConditions.Count > 0)
                {
                    Debug.Log("이벤트 감지: " + other.gameObject.name);
                    //추가 조건 체크
                    foreach (var condition in eventAction.extraConditions)
                    {
                        if(condition == StoryEventAction.CharacterID)
                        {
                            //캐릭터 ID 조건 체크
                            if (NetworkManager.instance.GetCurCharacterName() == eventAction.conditionValue)
                            {
                                Debug.Log("스크립트 실행");
                                StoryManager.Instance.OnScriptEvent(eventID);
                                Destroy(gameObject); //이벤트 발생 후 오브젝트 제거
                            }
                        }
                    }
                }
                else
                {
                    //추가 조건이 없으면 바로 이벤트 발생
                    StoryManager.Instance.OnScriptEvent(eventID);
                    Destroy(gameObject); //이벤트 발생 후 오브젝트 제거
                }
            }
        }

        //Destroy(gameObject); //이벤트 발생 후 오브젝트 제거
    }

    private void OnTriggerExit(Collider other)
    {
        if (eventAction.defCondition == StoryEventAction.ExitCollider)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                //스토리 이벤트 발생시키는 함수 호출
                StoryManager.Instance.OnScriptEvent(eventID);
            }
        }

        //Destroy(gameObject); //이벤트 발생 후 오브젝트 제거
    }
}

[Serializable]
enum StoryEventAction
{
    EnterCollider,//들어가면 무조건
    ExitCollider,//나가면 무조건
    Interact,//상호작용하면 무조건
    CharacterID//특정 캐릭터가 있을 때
}

[Serializable]
class StoryEventCondition
{
    public StoryEventAction defCondition;
    public List<StoryEventAction> extraConditions = new List<StoryEventAction>();

    //조건에 필요한 값들(예: 캐릭터 ID, 아이템 ID 등)
    public int conditionValue;
}
