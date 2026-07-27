using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "BleedEffect", menuName = "StatusEffects/BleedEffect")]
public class BleedEffect : StatusEffectBase
{
    public float Damage = 5f;

    public float moveDistanceTheadhold = 1f;

    Vector3 prePos;


    //디폴트   생성자
    public BleedEffect()
    {
        effectID = 1;

    }

    //매개변수 있는 생성자
    public BleedEffect(float damage, float tick, int stack)
    {
        Damage = damage;
        this.TickInterval = tick;
        this.Duration = 300;
        stackType = StackType.Stack;
        this.StackStride = stack;

        effectID = 1;
    }

    public override void OnApply(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"출혈시작 {target.name}");
        prePos = target.transform.position;

        StatusEffectHandler effHandle = StatusEffectPoolManager.Instance.GetStatusEffectHandler(effectID);
        effHandle.gameObject.transform.SetParent(target.transform);
        effHandle.gameObject.transform.localPosition = Vector3.zero;
        effHandle.gameObject.transform.localPosition = target.GetComponent<CapsuleCollider>().center;
        effHandle.PlayStatusEffectVFX();
    }

    public override void OnTick(CharacterBase target, StatusEffectInstance instance)
    {

    }

    public override void OnRemove(CharacterBase target, StatusEffectInstance instance)
    {
        Debug.Log($"출혈끝 {target.name}");

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.StopStatusEffectVFX();
        }
    }

    public override void OnMove(CharacterBase target, StatusEffectInstance instance)
    {
        if (target.curDistance >= moveDistanceTheadhold && instance.Stack > 0)
        {
            target.TakeDamage(((int)Damage));
            instance.Stack--;
            ChatManager.Instance.AddSystemMessage($"{target.name}이(가) 출혈로 {Damage} 데미지를 입었습니다. 남은 스택: {instance.Stack}");
            target.SetDistance(0f);

            StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
            foreach (var effHandle in effHandles)
            {
                if (effHandle.effectID == effectID)
                    effHandle.PlayStatusEffectVFX();
            }
        }

        prePos = target.transform.position; //현재 위치 저장
    }

    public override void OnAttack(CharacterBase target, StatusEffectInstance instance)
    {
        if (instance.Stack <= 0) return;

        target.TakeDamage(((int)Damage));
        instance.Stack--;
        ChatManager.Instance.AddSystemMessage($"{target.name}이(가) 출혈로 {Damage} 데미지를 입었습니다. 남은 스택: {instance.Stack}");

        StatusEffectHandler[] effHandles = target.GetComponentsInChildren<StatusEffectHandler>();
        foreach (var effHandle in effHandles)
        {
            if (effHandle.effectID == effectID)
                effHandle.PlayStatusEffectVFX();
        }
    }
}
