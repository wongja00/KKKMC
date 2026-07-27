using System;

public static class EventBus
{
    public static Action<CombatEventType, CombatEventData> OnCombatEvent;
}
