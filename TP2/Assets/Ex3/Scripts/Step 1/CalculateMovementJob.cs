using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateMovementJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Seekers;
    [ReadOnly] public NativeArray<float3> Targets;
    public NativeArray<float3> OutVelocities;
    public float Speed;

    // L'index est fourni par Unity, on traite un seul élément à la fois
    public void Execute(int index)
    {
        float3 seekerPos = Seekers[index]; // On remplace 'i' par 'index'

        if (seekerPos.x > 1000000f)
        {
            OutVelocities[index] = float3.zero;
            return; // 'continue' devient 'return' car on sort de la méthode pour cet index
        }

        float3 closestTargetPos = seekerPos;
        float closestDistSq = float.MaxValue;
        bool foundTarget = false;

        for (int j = 0; j < Targets.Length; j++)
        {
            float3 targetPos = Targets[j];
            float distSq = math.distancesq(seekerPos, targetPos);

            if (distSq < closestDistSq && targetPos.x < 1000000f && distSq > 0.001f)
            {
                closestDistSq = distSq;
                closestTargetPos = targetPos;
                foundTarget = true;
            }
        }

        if (foundTarget)
        {
            if (closestDistSq < 0.01f) // 0.01f car c'est la distance au carré (0.1 * 0.1)
            {
                OutVelocities[index] = float3.zero;
            }
            else
            {
                float3 direction = math.normalize(closestTargetPos - seekerPos);
                OutVelocities[index] = direction * Speed;
            }
        }
        else
        {
            OutVelocities[index] = float3.zero;
        }
    }
}