using System;
using Unity.Behavior;

[BlackboardEnum]
public enum EnemyState
{
	Idle,
	Attack,
	Chase,
	Dodge,
	Stun
}
