using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct HashPositionsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    public float CellSize;
    // ParallelWriter permet à plusieurs threads d'ajouter des données simultanément
    public NativeParallelMultiHashMap<int, int>.ParallelWriter SpatialMap;

    public void Execute(int index)
    {
        float3 pos = Positions[index];

        // On ignore les entités mortes (position à l'infini)
        if (pos.x > 1000000f || math.isnan(pos.x)) return;

        // Calcul de la cellule (Grid Coordinates)
        int3 gridPos = new int3((int)math.floor(pos.x / CellSize),
                                (int)math.floor(pos.y / CellSize),
                                (int)math.floor(pos.z / CellSize));

        // Génération d'un hash unique pour cette cellule
        int hash = (gridPos.x * 73856093) ^ (gridPos.y * 19349663) ^ (gridPos.z * 83492791);

        SpatialMap.Add(hash, index);
    }
}