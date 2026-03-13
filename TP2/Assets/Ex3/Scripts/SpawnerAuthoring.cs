using Unity.Entities;
using UnityEngine;

namespace Step4.ECS
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject plantPrefab;
        public GameObject preyPrefab;
        public GameObject predatorPrefab;

        public int plantCount;
        public int preyCount;
        public int predatorCount;

        public float gridSize;

        // Ajoutez ces champs pour l'inspecteur
        public float preySpeed;
        public float predatorSpeed;

        public class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnerConfig
                {
                    PlantPrefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                    PreyPrefab = GetEntity(authoring.preyPrefab, TransformUsageFlags.Dynamic),
                    PredatorPrefab = GetEntity(authoring.predatorPrefab, TransformUsageFlags.Dynamic),

                    PlantCount = authoring.plantCount,
                    PreyCount = authoring.preyCount,
                    PredatorCount = authoring.predatorCount,

                    GridSize = authoring.gridSize,

                    // On lie les valeurs ici
                    PreySpeed = authoring.preySpeed,
                    PredatorSpeed = authoring.predatorSpeed
                });
            }
        }
    }
}