using UnityEngine;

public abstract class EffectLogic : ScriptableObject
{
    public abstract void Apply(GameManager gameManager, PlayerController targetPlayer);
}