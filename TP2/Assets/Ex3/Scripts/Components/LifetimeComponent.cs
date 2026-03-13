using Unity.Entities;
public struct LifetimeComponent : IComponentData
{
    public float CurrentLifetime;
    public float StartingLifetime;
    public float DecreasingFactor;
    public bool Reproduced;
}