using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Step4.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(MovementSystem))]
    public partial struct MovementApplySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // Job simple et parallèle pour appliquer le mouvement
            state.Dependency = new ApplyVelocityJob { DeltaTime = dt }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct ApplyVelocityJob : IJobEntity
    {
        public float DeltaTime;

        void Execute(ref LocalTransform transform, in VelocityComponent vel)
        {
            // Applique le vecteur linéaire calculé par le SeekTargetJob
            transform.Position += vel.Linear * DeltaTime;
        }
    }
}