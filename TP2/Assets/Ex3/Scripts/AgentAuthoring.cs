using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum AgentType { Plant, Prey, Predator }

public class AgentAuthoring : MonoBehaviour
{
    public AgentType type;
    public float startingLifetime = 10f;
    public float speed = 2f;

    // Le Baker est la classe interne qui fait la conversion
    class Baker : Baker<AgentAuthoring>
    {
        public override void Bake(AgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            switch (authoring.type)
            {
                case AgentType.Plant:
                    AddComponent<PlantTag>(entity);
                    break;
                case AgentType.Prey:
                    AddComponent<PreyTag>(entity);
                    // Log pour vérifier que c'est bien exécuté
                    // UnityEngine.Debug.Log("Baking d'une Proie terminé"); 
                    break;
                case AgentType.Predator:
                    AddComponent<PredatorTag>(entity);
                    break;
            }

            // Initialisation de la Velocity
            AddComponent(entity, new VelocityComponent
            {
                Linear = float3.zero,
                Speed = authoring.speed
            });

            // Initialisation du Lifetime (Remplace LifetimeData.cs)
            AddComponent(entity, new LifetimeComponent
            {
                StartingLifetime = authoring.startingLifetime,
                CurrentLifetime = authoring.startingLifetime,
                DecreasingFactor = 1f,
                Reproduced = false
            });
        }
    }
}