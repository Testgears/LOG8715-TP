using UnityEngine;
using System.Collections.Generic;

public class RewindGlobalState : IComponent
{
    public float CooldownRemaining = 3.0f;
    public Queue<List<EntitySnapshot>> WorldHistory = new Queue<List<EntitySnapshot>>();
}