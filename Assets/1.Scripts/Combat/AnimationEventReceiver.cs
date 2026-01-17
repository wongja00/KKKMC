using UnityEngine;

//애니메이션 아벤트를 받는 리시버
public class AnimationEventReceiver : MonoBehaviour
{
    private CombatSystem combatSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        combatSystem = GetComponent<CombatSystem>();
    }

    //애ㅐ니메이션 이벤트에서 호출되는 함수들
    public void OnDamageStart()
    {
        //데미지 판정 시작

    }

    public void OnDamageEnd()
    {
        //데미지 판정 종료
    }

    public void OnSpawnEffect(string effectName)
    {
        //이펙트 생성
    }

    public void OnPlaySound(string soundName)
    {
        //사운드 재생
    }

    public void OnCustomEvent(string eventName)
    {
        //커스텀 이벤트
        if(combatSystem != null)
        {
            //
        }
    }
}
