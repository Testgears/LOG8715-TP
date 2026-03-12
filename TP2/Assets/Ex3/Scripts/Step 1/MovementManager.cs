using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    void Update()
    {
        // Vérification de sécurité pour l'initialisation du Spawner
        if (Ex4Spawner.PlantTransforms == null || Ex4Spawner.PreyTransforms == null) return;

        // 1. Gérer les Proies -> ciblent les Plantes
        RunMovementJob(Ex4Spawner.PreyTransforms, Ex4Spawner.PlantTransforms,
                       Ex4Spawner.PreyVelocities, Ex3Config.PreySpeed);

        // 2. Gérer les Prédateurs -> ciblent les Proies
        RunMovementJob(Ex4Spawner.PredatorTransforms, Ex4Spawner.PreyTransforms,
                       Ex4Spawner.PredatorVelocities, Ex3Config.PredatorSpeed);
    }

    private void RunMovementJob(Transform[] seekers, Transform[] targets, Velocity[] vels, float speed)
    {
        if (seekers.Length == 0 || targets.Length == 0) return;

        int seekerCount = seekers.Length;
        int targetCount = targets.Length;

        var seekerPositions = new NativeArray<float3>(seekerCount, Allocator.TempJob);
        var targetPositions = new NativeArray<float3>(targetCount, Allocator.TempJob);
        var results = new NativeArray<float3>(seekerCount, Allocator.TempJob);

        // Copie des positions des chercheurs (seulement s'ils sont actifs)
        for (int i = 0; i < seekerCount; i++)
        {
            if (seekers[i].gameObject.activeSelf)
                seekerPositions[i] = seekers[i].position;
            else
                // Si le chercheur est mort, on le met loin pour qu'il ne calcule rien d'utile
                seekerPositions[i] = new float3(float.MaxValue);
        }

        // Copie des cibles (IMPORTANT : ignore les objets inactifs)
        for (int j = 0; j < targetCount; j++)
        {
            if (targets[j].gameObject.activeSelf)
                targetPositions[j] = targets[j].position;
            else
                // On met une position infinie pour que cette cible soit ignorée par le calcul du plus proche
                targetPositions[j] = new float3(float.MaxValue);
        }

        var job = new CalculateMovementJob
        {
            Seekers = seekerPositions,
            Targets = targetPositions,
            OutVelocities = results,
            Speed = speed
        };

        JobHandle handle = job.Schedule(seekerCount, 64);
        handle.Complete();

        // Application des résultats aux scripts Velocity
        for (int i = 0; i < seekerCount; i++)
        {
            // On n'applique la vitesse que si l'objet est actif
            if (seekers[i].gameObject.activeSelf)
            {
                vels[i].velocity = results[i];
            }
        }

        seekerPositions.Dispose();
        targetPositions.Dispose();
        results.Dispose();
    }
}