using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    public partial struct LifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float distSq = 1.0f;
            int frameTick = (int)(SystemAPI.Time.ElapsedTime * 60);
            float cellSize = 5.0f; // Keep consistent with MovementSystem

            var plantQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PlantTag>().Build();
            var preyQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PreyTag>().Build();
            var predQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PredatorTag>().Build();

            var plantTransforms = plantQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var preyTransforms = preyQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var predTransforms = predQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            // --- 1. Create Spatial Maps ---
            var plantMap = new NativeParallelMultiHashMap<int, int>(plantTransforms.Length, state.WorldUpdateAllocator);
            var preyMap = new NativeParallelMultiHashMap<int, int>(preyTransforms.Length, state.WorldUpdateAllocator);
            var predMap = new NativeParallelMultiHashMap<int, int>(predTransforms.Length, state.WorldUpdateAllocator);

            // --- 2. Schedule Hash Jobs ---
            var hashPlants = new HashPositionsJob
            {
                Transforms = plantTransforms,
                CellSize = cellSize,
                SpatialMap = plantMap.AsParallelWriter()
            }.Schedule(plantTransforms.Length, 64, state.Dependency);

            var hashPreys = new HashPositionsJob
            {
                Transforms = preyTransforms,
                CellSize = cellSize,
                SpatialMap = preyMap.AsParallelWriter()
            }.Schedule(preyTransforms.Length, 64, state.Dependency);

            var hashPreds = new HashPositionsJob
            {
                Transforms = predTransforms,
                CellSize = cellSize,
                SpatialMap = predMap.AsParallelWriter()
            }.Schedule(predTransforms.Length, 64, state.Dependency);

            var combinedHashDep = JobHandle.CombineDependencies(hashPlants, hashPreys, hashPreds);

            // --- 3. Schedule Life Update Jobs (Chained dependencies!) ---
            var preyDep = new UpdatePreyLifeJob
            {
                Plants = plantTransforms,
                Predators = predTransforms,
                PlantMap = plantMap,
                PredatorMap = predMap,
                CellSize = cellSize,
                DT = deltaTime,
                DistSq = distSq,
                Frame = frameTick
            }.ScheduleParallel(combinedHashDep);

            var plantDep = new UpdatePlantLifeJob
            {
                Preys = preyTransforms,
                PreyMap = preyMap,
                CellSize = cellSize,
                DT = deltaTime,
                DistSq = distSq,
                Frame = frameTick
            }.ScheduleParallel(preyDep); // <-- CHAINED HERE

            var predDep = new UpdatePredatorLifeJob
            {
                Preys = preyTransforms,
                PreyMap = preyMap,
                CellSize = cellSize,
                DT = deltaTime,
                DistSq = distSq,
                Frame = frameTick
            }.ScheduleParallel(plantDep); // <-- CHAINED HERE

            state.Dependency = predDep; // <-- Return the final dependency
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
    public partial struct UpdatePreyLifeJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> Plants, Predators;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> PlantMap, PredatorMap;
        public float CellSize, DT, DistSq;
        public int Frame;

        void Execute([EntityIndexInQuery] int i, ref LifetimeComponent life, in LocalTransform transform, in PreyTag tag)
        {
            if (i % 4 == Frame % 4)
            {
                life.DecreasingFactor = 1.0f;
                int3 grid = (int3)math.floor(transform.Position / CellSize);

                // Check Plants (Boost Life)
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                        if (PlantMap.TryGetFirstValue(hash, out int tIdx, out var it))
                        {
                            do
                            {
                                if (math.distancesq(transform.Position, Plants[tIdx].Position) < DistSq)
                                    life.DecreasingFactor /= 2.0f;
                            } while (PlantMap.TryGetNextValue(out tIdx, ref it));
                        }
                    }
                }

                // Check Predators (Drain Life)
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                        if (PredatorMap.TryGetFirstValue(hash, out int tIdx, out var it))
                        {
                            do
                            {
                                if (math.distancesq(transform.Position, Predators[tIdx].Position) < DistSq)
                                    life.DecreasingFactor *= 2.0f;
                            } while (PredatorMap.TryGetNextValue(out tIdx, ref it));
                        }
                    }
                }
            }
            life.CurrentLifetime -= DT * life.DecreasingFactor;
        }
    }

    [BurstCompile]
    public partial struct UpdatePlantLifeJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> Preys;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> PreyMap;
        public float CellSize, DT, DistSq;
        public int Frame;

        void Execute([EntityIndexInQuery] int i, ref LifetimeComponent life, in LocalTransform transform, in PlantTag tag)
        {
            if (i % 4 == Frame % 4)
            {
                life.DecreasingFactor = 1.0f;
                int3 grid = (int3)math.floor(transform.Position / CellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                        if (PreyMap.TryGetFirstValue(hash, out int tIdx, out var it))
                        {
                            do
                            {
                                if (math.distancesq(transform.Position, Preys[tIdx].Position) < DistSq)
                                    life.DecreasingFactor *= 2.0f;
                            } while (PreyMap.TryGetNextValue(out tIdx, ref it));
                        }
                    }
                }
            }
            life.CurrentLifetime -= DT * life.DecreasingFactor;
        }
    }

    [BurstCompile]
    public partial struct UpdatePredatorLifeJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> Preys;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> PreyMap;
        public float CellSize, DT, DistSq;
        public int Frame;

        void Execute([EntityIndexInQuery] int i, ref LifetimeComponent life, in LocalTransform transform, in PredatorTag tag)
        {
            if (i % 4 == Frame % 4)
            {
                life.DecreasingFactor = 1.0f;
                int3 grid = (int3)math.floor(transform.Position / CellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                        if (PreyMap.TryGetFirstValue(hash, out int tIdx, out var it))
                        {
                            do
                            {
                                if (math.distancesq(transform.Position, Preys[tIdx].Position) < DistSq)
                                    life.DecreasingFactor /= 2.0f;
                            } while (PreyMap.TryGetNextValue(out tIdx, ref it));
                        }
                    }
                }
            }
            life.CurrentLifetime -= DT * life.DecreasingFactor;
        }
    }
}