using Unity.Entities;
using Unity.Mathematics;

namespace Step4.ECS
{
    public struct SpawnerConfig : IComponentData
    {
        public Entity PlantPrefab;
        public Entity PreyPrefab;
        public Entity PredatorPrefab;

        public int PlantCount;
        public int PreyCount;
        public int PredatorCount;

        public float GridSize;

        // AJOUTEZ CES DEUX LIGNES :
        public float PreySpeed;
        public float PredatorSpeed;
    }
}