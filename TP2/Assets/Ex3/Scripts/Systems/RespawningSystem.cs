using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(LifetimeSystem))]
    public partial struct RespawningSystem : ISystem
    {
        // 1. Declare the lookups as fields
        private ComponentLookup<PlantTag> plantLookup;
        private ComponentLookup<PreyTag> preyLookup;
        private ComponentLookup<PredatorTag> predLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 2. Initialize them ONCE in OnCreate (true = IsReadOnly)
            plantLookup = state.GetComponentLookup<PlantTag>(true);
            preyLookup = state.GetComponentLookup<PreyTag>(true);
            predLookup = state.GetComponentLookup<PredatorTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<SpawnerConfig>()) return;

            var config = SystemAPI.GetSingleton<SpawnerConfig>();
            uint baseSeed = (uint)(SystemAPI.Time.ElapsedTime * 1000) + 1;
            float cellSize = 5.0f;
            float safeDistSq = 1.5f;

            // 3. Update the lookups at the beginning of the frame
            plantLookup.Update(ref state);
            preyLookup.Update(ref state);
            predLookup.Update(ref state);

            var allEntitiesQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAny<PlantTag, PreyTag, PredatorTag>().Build();
            var allTransforms = allEntitiesQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            var occupiedMap = new NativeParallelMultiHashMap<int, float3>(allTransforms.Length, state.WorldUpdateAllocator);

            var hashJob = new HashAllPositionsJob
            {
                Transforms = allTransforms,
                CellSize = cellSize,
                SpatialMap = occupiedMap.AsParallelWriter()
            }.Schedule(allTransforms.Length, 64, state.Dependency);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            state.Dependency = new SafeRespawnJob
            {
                GridSize = config.GridSize,
                BaseSeed = baseSeed,
                PlantLookup = plantLookup, // Pass the cached lookups
                PreyLookup = preyLookup,
                PredatorLookup = predLookup,
                OccupiedMap = occupiedMap,
                CellSize = cellSize,
                SafeDistSq = safeDistSq,
                ECB = ecb
            }.ScheduleParallel(hashJob);
        }
    }

    [BurstCompile]
    public struct HashAllPositionsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<LocalTransform> Transforms;
        public float CellSize;
        public NativeParallelMultiHashMap<int, float3>.ParallelWriter SpatialMap;

        public void Execute(int i)
        {
            float3 pos = Transforms[i].Position;
            int3 grid = (int3)math.floor(pos / CellSize);
            int hash = ((grid.x) * 73856093) ^ ((grid.y) * 19349663);
            SpatialMap.Add(hash, pos);
        }
    }

    [BurstCompile]
    public partial struct SafeRespawnJob : IJobEntity
    {
        public float GridSize;
        public uint BaseSeed;

        [ReadOnly] public ComponentLookup<PlantTag> PlantLookup;
        [ReadOnly] public ComponentLookup<PreyTag> PreyLookup;
        [ReadOnly] public ComponentLookup<PredatorTag> PredatorLookup;
        [ReadOnly] public NativeParallelMultiHashMap<int, float3> OccupiedMap;

        public float CellSize;
        public float SafeDistSq;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([EntityIndexInQuery] int i, ref LocalTransform transform, ref LifetimeComponent life, Entity entity)
        {
            if (life.CurrentLifetime <= 0)
            {
                bool isPlant = PlantLookup.HasComponent(entity);
                bool isPrey = PreyLookup.HasComponent(entity);
                bool isPredator = PredatorLookup.HasComponent(entity);

                if (isPlant || ((isPrey || isPredator) && life.Reproduced))
                {
                    uint hashValue = math.hash(new uint2((uint)i, BaseSeed));
                    var random = new Random(math.max(1, hashValue));

                    float height = math.round(math.sqrt(GridSize / 1.77f));
                    float width = math.round(GridSize / height);

                    float3 finalPos = float3.zero;
                    bool foundSafeSpot = false;
                    int maxAttempts = 10;

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        float3 randomPos = new float3(
                            random.NextFloat(-width * 0.5f, width * 0.5f),
                            random.NextFloat(-height * 0.5f, height * 0.5f),
                            0f
                        );

                        bool isSafe = true;
                        int3 grid = (int3)math.floor(randomPos / CellSize);

                        for (int x = -1; x <= 1 && isSafe; x++)
                        {
                            for (int y = -1; y <= 1 && isSafe; y++)
                            {
                                int hash = ((grid.x + x) * 73856093) ^ ((grid.y + y) * 19349663);
                                if (OccupiedMap.TryGetFirstValue(hash, out float3 otherPos, out var it))
                                {
                                    do
                                    {
                                        if (math.distancesq(randomPos, otherPos) < SafeDistSq)
                                        {
                                            isSafe = false;
                                            break;
                                        }
                                    } while (OccupiedMap.TryGetNextValue(out otherPos, ref it));
                                }
                            }
                        }

                        if (isSafe)
                        {
                            finalPos = randomPos;
                            foundSafeSpot = true;
                            break;
                        }
                    }

                    if (!foundSafeSpot)
                    {
                        finalPos = new float3(
                            random.NextFloat(-width * 0.5f, width * 0.5f),
                            random.NextFloat(-height * 0.5f, height * 0.5f),
                            0f
                        );
                    }

                    transform.Position = finalPos;
                    life.CurrentLifetime = life.StartingLifetime;
                    life.Reproduced = false;
                }
                else
                {
                    ECB.DestroyEntity(i, entity);
                }
            }
        }
    }
}