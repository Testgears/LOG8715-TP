using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<SpawnerConfig>(out var config)) return;

            float cellSize = 5.0f; // Taille de cellule pour le hash spatial

            var plantQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PlantTag>().Build();
            var preyQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, VelocityComponent, PreyTag>().Build();
            var predQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, VelocityComponent, PredatorTag>().Build();

            // Extraction des positions
            var plantPos = plantQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var preyPos = preyQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            // --- Logique pour les PROIES (Cherchent les Plantes) ---
            var plantMap = new NativeParallelMultiHashMap<int, int>(plantPos.Length, state.WorldUpdateAllocator);
            state.Dependency = new HashPositionsJob 
            { 
                Transforms = plantPos, CellSize = cellSize, SpatialMap = plantMap.AsParallelWriter() 
            }.Schedule(plantPos.Length, 64, state.Dependency);

            state.Dependency = new SeekTargetJob
            {
                Targets = plantPos,
                SpatialMap = plantMap,
                CellSize = cellSize,
                Speed = config.PreySpeed
            }.ScheduleParallel(preyQuery, state.Dependency);

            // --- Logique pour les PRÉDATEURS (Cherchent les Proies) ---
            var preyMap = new NativeParallelMultiHashMap<int, int>(preyPos.Length, state.WorldUpdateAllocator);
            state.Dependency = new HashPositionsJob 
            { 
                Transforms = preyPos, CellSize = cellSize, SpatialMap = preyMap.AsParallelWriter() 
            }.Schedule(preyPos.Length, 64, state.Dependency);

            state.Dependency = new SeekTargetJob
            {
                Targets = preyPos,
                SpatialMap = preyMap,
                CellSize = cellSize,
                Speed = config.PredatorSpeed
            }.ScheduleParallel(predQuery, state.Dependency);
        }

        [BurstCompile]
        public struct HashPositionsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<LocalTransform> Transforms;
            public float CellSize;
            public NativeParallelMultiHashMap<int, int>.ParallelWriter SpatialMap;

            public void Execute(int i)
            {
                int3 grid = (int3)math.floor(Transforms[i].Position / CellSize);
                int hash = ((grid.x) * 73856093) ^ ((grid.y) * 19349663);
                SpatialMap.Add(hash, i);
            }
        }
    }

    [BurstCompile]
    public partial struct SeekTargetJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> Targets;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialMap;
        public float CellSize;
        public float Speed;

        void Execute(ref VelocityComponent vel, in LocalTransform transform)
        {
            float3 seekerPos = transform.Position;
            float3 closestPos = seekerPos;
            float minDistSq = float.MaxValue;
            bool found = false;

            // SÉCURITÉ 1 : Si aucune cible n'existe sur la carte
            if (Targets.Length == 0)
            {
                vel.Linear = float3.zero;
                return;
            }

            int3 grid = (int3)math.floor(seekerPos / CellSize);
            
            // Recherche dans les 9 cellules voisines
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                    if (SpatialMap.TryGetFirstValue(hash, out int tIdx, out var it))
                    {
                        do
                        {
                            float dSq = math.distancesq(seekerPos, Targets[tIdx].Position);
                            if (dSq < minDistSq && dSq > 0.001f)
                            {
                                minDistSq = dSq;
                                closestPos = Targets[tIdx].Position;
                                found = true;
                            }
                        } while (SpatialMap.TryGetNextValue(out tIdx, ref it));
                    }
                }
            }

            // SÉCURITÉ 2 : Si une cible est trouvée, on fonce, sinon on s'arrête
            if (found)
            {
                vel.Linear = math.normalize(closestPos - seekerPos) * Speed;
            }
            else
            {
                vel.Linear = float3.zero;
            }
        }
    }
}