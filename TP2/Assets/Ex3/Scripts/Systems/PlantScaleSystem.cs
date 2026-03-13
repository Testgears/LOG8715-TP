using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    // S'exécute après le calcul de vie pour avoir le scale à jour visuellement
    [UpdateAfter(typeof(LifetimeSystem))]
    public partial struct PlantScaleSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new PlantScaleJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct PlantScaleJob : IJobEntity
    {
        // On ne cible que les entités avec le PlantTag
        void Execute(ref LocalTransform transform, in LifetimeComponent life, in PlantTag tag)
        {
            if (life.StartingLifetime > 0)
            {
                float lifeRatio = math.saturate(life.CurrentLifetime / life.StartingLifetime);
                transform.Scale = lifeRatio;
            }
            else
            {
                transform.Scale = 0f;
            }
        }
    }
}