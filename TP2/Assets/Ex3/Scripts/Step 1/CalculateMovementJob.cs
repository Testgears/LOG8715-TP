using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateMovementJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Seekers;
    [ReadOnly] public NativeArray<float3> Targets;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialMap;
    [ReadOnly] public NativeArray<float3> CurrentVelocities; // Vitesse de la frame précédente

    public NativeArray<float3> OutVelocities;
    public float CellSize;
    public float Speed;
    public int FrameCount; // Time.frameCount
    public int UpdateRate; // ex: 4 pour 1/4 des agents par frame

    public void Execute(int index)
    {
        float3 seekerPos = Seekers[index];
        if (seekerPos.x > 1000000f) { OutVelocities[index] = float3.zero; return; }

        // --- TIME-SLICING ---
        // Si ce n'est pas le tour de cet agent, il continue sur sa lancée
        if (index % UpdateRate != FrameCount % UpdateRate)
        {
            OutVelocities[index] = CurrentVelocities[index];
            return;
        }

        // --- LOGIQUE DE RECHERCHE (SPATIAL HASHING) ---
        float3 closestTargetPos = seekerPos;
        float closestDistSq = float.MaxValue;
        bool foundTarget = false;

        int3 centerGridPos = (int3)math.floor(seekerPos / CellSize);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int3 neighborGridPos = centerGridPos + new int3(x, y, 0);
                int hash = (neighborGridPos.x * 73856093) ^ (neighborGridPos.y * 19349663) ^ (neighborGridPos.z * 83492791);

                if (SpatialMap.TryGetFirstValue(hash, out int targetIndex, out var it))
                {
                    do
                    {
                        float3 targetPos = Targets[targetIndex];
                        float distSq = math.distancesq(seekerPos, targetPos);

                        if (distSq < closestDistSq && distSq > 0.001f)
                        {
                            closestDistSq = distSq;
                            closestTargetPos = targetPos;
                            foundTarget = true;
                        }
                    } while (SpatialMap.TryGetNextValue(out targetIndex, ref it));
                }
            }
        }

        if (foundTarget && closestDistSq > 0.01f)
            OutVelocities[index] = math.normalize(closestTargetPos - seekerPos) * Speed;
        else
            OutVelocities[index] = float3.zero;
    }
}