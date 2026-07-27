using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//개별 공격 기술 데이터
[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/Combat/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("기본 정보")]
    public string attackName = "Attack";
    public int attackID = 0;

    [Header("기본 정보")]
    public AnimationClip animationClip;
    public float animationSpeed = 1.0f; //애니메이션 재생 속도

    [Header("타이밍")]
    //public float startupTime = 0.1f;//선딜
    //public float activeTime = 0.2f; //판정시간
    //public float recoveryTime = 0.3f;//후딜
    public List<HitBoxWindow> hitBoxTimes = new List<HitBoxWindow>();//히트박스 판정시간 0~1까지 NormalTime
    //public float totalDuration => startupTime + activeTime + recoveryTime;

    [Header("판정")]
    public float damage = 10f;
    public float distance = 5f;//판정 거리
    public float knockbackForce = 5f;
    public Vector3 hitboxOffset = Vector3.zero;
    public Vector3 hitboxSize = Vector3.one;
    public LayerMask hitLayerMask;

    [Header("이벤트 타임라인")]
    public List<AttackEvent> events = new List<AttackEvent>();

    [Header("입력")]
    public AttackInputType inputType = AttackInputType.Light;
    public float inputWindow = 0.3f;// 다음 공격 입력 가능 시간

    [Header("이동")]
    public bool canMoveDuringAttack = false;
    public bool canRotateDuringAttack = false;
    public float movementSpeedMultiplier = 0.5f;
    
    [Header("전진 이동")]
    public float forwardMovementOnAttack = 0f;

    [Header("콤보 연결")]
    public List<int> canChainTo = new List<int>();//연결 가능한 공격ID들

    [Header("이펙트들")]
    public List<CharacterEffect> effectPrefabs = new List<CharacterEffect>();//이펙트들
    
    [Header("상태이상효과들")]
    public List<StatusEffectBase> statusEffects = new List<StatusEffectBase>();//상태이상효과들

}


[Serializable]
public class AttackEvent
{
    public string eventName;
    [Range(0f, 1f)] public float triggerTime; // 애니메이션 시작 후 몇 초에 발생(0~1정규화하는게 좋을듯)
    public AttackEventType eventType;
    public Vector3 position;
    public Vector3 rotation;

    //이벤트별 추가 데이터
    public float floatValue;
    public int intValue;
    public string stringValue;
}

[Serializable]
public class HitBoxWindow
{
    [Range(0f, 1f)] public float start = 0.1f;
    [Range(0f, 1f)] public float end = 0.2f;
}

[Serializable]
public class CharacterEffect
{
    public GameObject prefab;
    public EffectSocketPart part;
    public string effectName;
}

public enum AttackEventType
{
    DamageStart, //데미지 판정 시작
    DamageEnd,  //데미지 판정 끝
    SpawnEffect, // 이펙트 생성
    PlaySound, // 사운드 재생
    ScreenShake, // 화면 흔들
    LaunchProjectile,// 투사체 발사
    Teleport,// 순간이동
    Custom//커스텀 이벤트
}

public enum AttackInputType
{
    Light, //약공
    Heavy,//강공
    Special,//특공
    Dodge,//회피
    Guard,//가드
    Jump//점프
}

public enum BTAttackType
{
    Melee,
    Range,
    Special
}
