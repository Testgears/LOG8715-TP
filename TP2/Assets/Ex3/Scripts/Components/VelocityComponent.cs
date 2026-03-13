using Unity.Entities;
using Unity.Mathematics;
public struct VelocityComponent : IComponentData
{
    public float3 Linear;
    public float Speed;
}