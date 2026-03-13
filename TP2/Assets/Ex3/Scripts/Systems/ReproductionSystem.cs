using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    [UpdateBefore(typeof(RespawningSystem))] // Important: Before death verification
    public partial struct ReproductionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float cellSize = 5.0f; // Keep consistent with other systems
            float collisionDistSq = 0.8f; // Contact distance squared

            // 1. Gather Entities and Transforms
            var preyQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PreyTag>().Build();
            var predQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PredatorTag>().Build();

            var preyTransforms = preyQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var preyEntities = preyQuery.ToEntityArray(state.WorldUpdateAllocator);

            var predTransforms = predQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var predEntities = predQuery.ToEntityArray(state.WorldUpdateAllocator);

            // 2. Create Spatial Maps
            var preyMap = new NativeParallelMultiHashMap<int, int>(preyTransforms.Length, state.WorldUpdateAllocator);
            var predMap = new NativeParallelMultiHashMap<int, int>(predTransforms.Length, state.WorldUpdateAllocator);

            // 3. Schedule Hash Jobs
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

            var combinedHashDep = JobHandle.CombineDependencies(hashPreys, hashPreds);

            // 4. Schedule Reproduction Jobs (Chained dependencies!)
            var preyDep = new PreyReproductionJob
            {
                OtherPreys = preyTransforms,
                OtherEntities = preyEntities,
                PreyMap = preyMap,
                CellSize = cellSize,
                DistSq = collisionDistSq
            }.ScheduleParallel(combinedHashDep);

            var predDep = new PredatorReproductionJob
            {
                OtherPreds = predTransforms,
                OtherEntities = predEntities,
                PredMap = predMap,
                CellSize = cellSize,
                DistSq = collisionDistSq
            }.ScheduleParallel(preyDep); // <-- CHAINED HERE: depends on preyDep

            state.Dependency = predDep; // <-- Return the final dependency
        }
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

    [BurstCompile]
    public partial struct PreyReproductionJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> OtherPreys;
        [ReadOnly] public NativeArray<Entity> OtherEntities;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> PreyMap;
        public float CellSize;
        public float DistSq;

        void Execute(Entity e, ref LifetimeComponent life, in LocalTransform transform, in PreyTag tag)
        {
            if (life.Reproduced) return; // Skip if already reproduced

            int3 grid = (int3)math.floor(transform.Position / CellSize);

            // Check 9 neighboring spatial cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                    if (PreyMap.TryGetFirstValue(hash, out int tIdx, out var it))
                    {
                        do
                        {
                            if (e == OtherEntities[tIdx]) continue; // Don't reproduce with self

                            if (math.distancesq(transform.Position, OtherPreys[tIdx].Position) < DistSq)
                            {
                                life.Reproduced = true;
                                return; // Early exit, saving more CPU cycles!
                            }
                        } while (PreyMap.TryGetNextValue(out tIdx, ref it));
                    }
                }
            }
        }
    }

    [BurstCompile]
    public partial struct PredatorReproductionJob : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> OtherPreds;
        [ReadOnly] public NativeArray<Entity> OtherEntities;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> PredMap;
        public float CellSize;
        public float DistSq;

        void Execute(Entity e, ref LifetimeComponent life, in LocalTransform transform, in PredatorTag tag)
        {
            if (life.Reproduced) return;

            int3 grid = (int3)math.floor(transform.Position / CellSize);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                    if (PredMap.TryGetFirstValue(hash, out int tIdx, out var it))
                    {
                        do
                        {
                            if (e == OtherEntities[tIdx]) continue;

                            if (math.distancesq(transform.Position, OtherPreds[tIdx].Position) < DistSq)
                            {
                                life.Reproduced = true;
                                return;
                            }
                        } while (PredMap.TryGetNextValue(out tIdx, ref it));
                    }
                }
            }
        }
    }
}