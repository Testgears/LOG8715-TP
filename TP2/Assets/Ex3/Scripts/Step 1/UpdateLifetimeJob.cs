using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

// --- JOB POUR LES PLANTES ---
[BurstCompile]
public struct UpdatePlantLifetimeJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> PlantPositions;
    [ReadOnly] public NativeArray<float3> PreyPositions;
    public NativeArray<LifetimeData> PlantLifetimes;
    public float DeltaTime;
    public float TouchingDistanceSq;

    public void Execute(int i)
    {
        var data = PlantLifetimes[i];
        data.DecreasingFactor = 1.0f;
        for (int j = 0; j < PreyPositions.Length; j++)
        {
            if (math.distancesq(PlantPositions[i], PreyPositions[j]) < TouchingDistanceSq)
            {
                data.DecreasingFactor *= 2.0f;
                break;
            }
        }
        data.CurrentLifetime -= DeltaTime * data.DecreasingFactor;
        PlantLifetimes[i] = data;
    }
}

// --- JOB POUR LES PROIES ---
[BurstCompile]
public struct UpdatePreyLifetimeJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> PreyPositions;
    [ReadOnly] public NativeArray<float3> PlantPositions;
    [ReadOnly] public NativeArray<float3> PredatorPositions;
    public NativeArray<LifetimeData> PreyLifetimes;
    public float DeltaTime;
    public float TouchingDistanceSq;

    public void Execute(int i)
    {
        var data = PreyLifetimes[i];
        data.DecreasingFactor = 1.0f;

        for (int j = 0; j < PlantPositions.Length; j++)
            if (math.distancesq(PreyPositions[i], PlantPositions[j]) < TouchingDistanceSq) { data.DecreasingFactor /= 2.0f; break; }

        for (int j = 0; j < PredatorPositions.Length; j++)
            if (math.distancesq(PreyPositions[i], PredatorPositions[j]) < TouchingDistanceSq) { data.DecreasingFactor *= 2.0f; break; }

        for (int j = 0; j < PreyPositions.Length; j++)
            if (i != j && math.distancesq(PreyPositions[i], PreyPositions[j]) < TouchingDistanceSq) { data.Reproduced = true; break; }

        data.CurrentLifetime -= DeltaTime * data.DecreasingFactor;
        PreyLifetimes[i] = data;
    }
}

// --- JOB POUR LES PREDATEURS ---
[BurstCompile]
public struct UpdatePredatorLifetimeJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> PredatorPositions;
    [ReadOnly] public NativeArray<float3> PreyPositions;
    public NativeArray<LifetimeData> PredatorLifetimes;
    public float DeltaTime;
    public float TouchingDistanceSq;

    public void Execute(int i)
    {
        var data = PredatorLifetimes[i];
        data.DecreasingFactor = 1.0f;

        for (int j = 0; j < PredatorPositions.Length; j++)
            if (i != j && math.distancesq(PredatorPositions[i], PredatorPositions[j]) < TouchingDistanceSq) { data.Reproduced = true; break; }

        for (int j = 0; j < PreyPositions.Length; j++)
            if (math.distancesq(PredatorPositions[i], PreyPositions[j]) < TouchingDistanceSq) { data.DecreasingFactor /= 2.0f; }

        data.CurrentLifetime -= DeltaTime * data.DecreasingFactor;
        PredatorLifetimes[i] = data;
    }
}