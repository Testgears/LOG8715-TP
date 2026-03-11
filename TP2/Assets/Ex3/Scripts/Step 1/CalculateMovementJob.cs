using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CalculateMovementJob : IJob
{
    [ReadOnly] public NativeArray<float3> Seekers;
    [ReadOnly] public NativeArray<float3> Targets;
    public NativeArray<float3> OutVelocities;
    public float Speed;

    public void Execute()
    {
        for (int i = 0; i < Seekers.Length; i++)
        {
            float3 seekerPos = Seekers[i];

            // Sécurité : Si le chercheur lui-même est désactivé (position à l'infini), vitesse nulle
            if (seekerPos.x > 1000000f)
            {
                OutVelocities[i] = float3.zero;
                continue;
            }

            float3 closestTargetPos = seekerPos;
            float closestDistSq = float.MaxValue;
            bool foundTarget = false;

            // Recherche de la cible la plus proche
            for (int j = 0; j < Targets.Length; j++)
            {
                float3 targetPos = Targets[j];

                // On calcule la distance
                float distSq = math.distancesq(seekerPos, targetPos);

                // Conditions pour accepter la cible :
                // 1. Plus proche que la précédente
                // 2. La cible n'est pas à l'infini (donc elle est active dans la scène)
                // 3. distSq > 0.001f : évite de se cibler soi-même si Seekers et Targets sont les mêmes
                if (distSq < closestDistSq && targetPos.x < 1000000f && distSq > 0.001f)
                {
                    closestDistSq = distSq;
                    closestTargetPos = targetPos;
                    foundTarget = true;
                }
            }

            // Calcul de la direction et application de la vitesse
            if (foundTarget)
            {
                // math.normalize est sûr ici car distSq > 0.001f garanti par le if ci-dessus
                float3 direction = math.normalize(closestTargetPos - seekerPos);
                OutVelocities[i] = direction * Speed;
            }
            else
            {
                // Aucune cible valide trouvée
                OutVelocities[i] = float3.zero;
            }
        }
    }
}