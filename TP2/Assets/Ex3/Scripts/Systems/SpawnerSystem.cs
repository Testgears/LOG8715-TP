using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine; // Nécessaire pour Debug.Log


using UnityMathematicsRandom = Unity.Mathematics.Random;

namespace Step4.ECS
{
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<SpawnerConfig>(out var config))
            {
                Debug.LogWarning("SpawnerSystem : SpawnerConfig non trouvé !");
                return;
            }

            Debug.Log($"SpawnerSystem : Lancement du spawn. Plantes: {config.PlantCount}");

            // Vérification immédiate des Prefabs reçus de la config
            bool hasPreyTag = state.EntityManager.HasComponent<PreyTag>(config.PreyPrefab);
            UnityEngine.Debug.Log($"Spawner : Prefab Proie a le Tag ? {hasPreyTag}");

            state.Enabled = false;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // On utilise l'alias ici
            var random = new UnityMathematicsRandom(123);

            int height = (int)math.round(math.sqrt(config.GridSize / 1.77f));
            int width = (int)math.round(config.GridSize / height);

            // On passe 'ref random' normalement
            SpawnGroup(ecb, config.PlantPrefab, config.PlantCount, width, height, ref random);
            SpawnGroup(ecb, config.PreyPrefab, config.PreyCount, width, height, ref random);
            SpawnGroup(ecb, config.PredatorPrefab, config.PredatorCount, width, height, ref random);

            ecb.Playback(state.EntityManager);
        }

        // Assurez-vous que la signature utilise aussi l'alias
        private void SpawnGroup(EntityCommandBuffer ecb, Entity prefab, int count, int w, int h, ref UnityMathematicsRandom random)
        {
            for (int i = 0; i < count; i++)
            {
                var entity = ecb.Instantiate(prefab);
                float3 pos = new float3(random.NextInt(-w / 2, w / 2), random.NextInt(-h / 2, h / 2), 0);
                ecb.SetComponent(entity, LocalTransform.FromPosition(pos));
            }
        }
    }
}