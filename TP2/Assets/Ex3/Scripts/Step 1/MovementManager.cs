using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    [Range(1, 10)][SerializeField] private int updateRate = 4;

    void Update()
    {
        if (Ex4Spawner.PlantTransforms == null || Ex4Spawner.PreyTransforms == null) return;

        RunMovementJob(Ex4Spawner.PreyTransforms, Ex4Spawner.PlantTransforms,
                       Ex4Spawner.PreyVelocities, Ex3Config.PreySpeed);

        RunMovementJob(Ex4Spawner.PredatorTransforms, Ex4Spawner.PreyTransforms,
                       Ex4Spawner.PredatorVelocities, Ex3Config.PredatorSpeed);
    }

    private void RunMovementJob(Transform[] seekers, Transform[] targets, Velocity[] vels, float speed)
    {
        if (seekers.Length == 0 || targets.Length == 0) return;

        int seekerCount = seekers.Length;
        int targetCount = targets.Length;
        float cellSize = 5f;

        var seekerPos = new NativeArray<float3>(seekerCount, Allocator.TempJob);
        var targetPos = new NativeArray<float3>(targetCount, Allocator.TempJob);
        var currentVels = new NativeArray<float3>(seekerCount, Allocator.TempJob);
        var results = new NativeArray<float3>(seekerCount, Allocator.TempJob);
        var spatialMap = new NativeParallelMultiHashMap<int, int>(targetCount, Allocator.TempJob);

        for (int i = 0; i < seekerCount; i++)
        {
            seekerPos[i] = seekers[i].gameObject.activeSelf ? (float3)seekers[i].position : new float3(float.MaxValue);
            currentVels[i] = vels[i].velocity; // Capture de la vitesse actuelle
        }
        for (int j = 0; j < targetCount; j++)
            targetPos[j] = targets[j].gameObject.activeSelf ? (float3)targets[j].position : new float3(float.MaxValue);

        var hashJob = new HashPositionsJob { Positions = targetPos, CellSize = cellSize, SpatialMap = spatialMap.AsParallelWriter() };
        JobHandle hashHandle = hashJob.Schedule(targetCount, 64);

        var moveJob = new CalculateMovementJob
        {
            Seekers = seekerPos,
            Targets = targetPos,
            SpatialMap = spatialMap,
            CurrentVelocities = currentVels,
            OutVelocities = results,
            CellSize = cellSize,
            Speed = speed,
            FrameCount = Time.frameCount,
            UpdateRate = updateRate
        };

        JobHandle moveHandle = moveJob.Schedule(seekerCount, 64, hashHandle);
        moveHandle.Complete();

        for (int i = 0; i < seekerCount; i++)
            if (seekers[i].gameObject.activeSelf) vels[i].velocity = results[i];

        seekerPos.Dispose();
        targetPos.Dispose();
        currentVels.Dispose();
        results.Dispose();
        spatialMap.Dispose();
    }
}