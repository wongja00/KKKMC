using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//콤보 체인 데이터
[CreateAssetMenu(fileName = "ComboChain", menuName = "Scriptable Objects/Combat/ComboChain")]
public class ComboChain : ScriptableObject
{
    [Header("콤보 정보")]
    public string comboName = "Basic Combo";
    public int comboID = 0;

    [Header("공격 시퀀스")]
    public List<ComboStep> steps = new List<ComboStep>();

    [Header("콤보 보너스")]
    public float damageMultiplier = 1.0f;
    public float styleMultiplier = 1.0f; //스타일 점수 배율

    [Header("조건")]
    public bool requiresAirborne = false;
    public bool requiesEnemyHit = false;
    public int minComboCount = 0; // 최소 콤보 수 필요
}

[Serializable]
public class ComboStep
{
    public AttackData attackData;
    public float inputWindow = 0.3f;//이 단계 입력 가능 시간
    public bool canCancel = true; //취소 가능 여부
    public ComboCancelType cancelType = ComboCancelType.Normal;
}

public enum ComboCancelType

{
    Normal, //일반 취소
    Special, //특수 취소(특수 자원 소모)
    Dodge //회피 취소
}
