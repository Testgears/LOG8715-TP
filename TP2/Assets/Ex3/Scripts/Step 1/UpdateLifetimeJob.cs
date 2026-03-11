using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateLifetimeJob : IJob
{
    // Données d'entrée (Positions)
    [ReadOnly] public NativeArray<float3> PlantPositions;
    [ReadOnly] public NativeArray<float3> PreyPositions;
    [ReadOnly] public NativeArray<float3> PredatorPositions;

    // Données d'entrée/sortie (États de vie)
    public NativeArray<LifetimeData> PlantLifetimes;
    public NativeArray<LifetimeData> PreyLifetimes;
    public NativeArray<LifetimeData> PredatorLifetimes;

    public float DeltaTime;
    public float TouchingDistanceSq;

    public void Execute()
    {
        // 1. Logique des PLANTES
        for (int i = 0; i < PlantPositions.Length; i++)
        {
            var data = PlantLifetimes[i];
            data.DecreasingFactor = 1.0f;
            // Si touchée par une proie -> vitesse doublée
            for (int j = 0; j < PreyPositions.Length; j++)
            {
                if (math.distancesq(PlantPositions[i], PreyPositions[j]) < TouchingDistanceSq)
                {
                    data.DecreasingFactor *= 2.0f;
                    break;
                }
            }
            UpdateLife(ref data);
            PlantLifetimes[i] = data;
        }

        // 2. Logique des PROIES
        for (int i = 0; i < PreyPositions.Length; i++)
        {
            var data = PreyLifetimes[i];
            data.DecreasingFactor = 1.0f;

            // Touchée par plante -> vitesse / 2
            for (int j = 0; j < PlantPositions.Length; j++)
            {
                if (math.distancesq(PreyPositions[i], PlantPositions[j]) < TouchingDistanceSq)
                {
                    data.DecreasingFactor /= 2.0f;
                    break;
                }
            }
            // Touchée par prédateur -> vitesse * 2
            for (int j = 0; j < PredatorPositions.Length; j++)
            {
                if (math.distancesq(PreyPositions[i], PredatorPositions[j]) < TouchingDistanceSq)
                {
                    data.DecreasingFactor *= 2.0f;
                    break;
                }
            }
            // Touchée par proie -> reproduction
            for (int j = 0; j < PreyPositions.Length; j++)
            {
                if (i != j && math.distancesq(PreyPositions[i], PreyPositions[j]) < TouchingDistanceSq)
                {
                    data.Reproduced = true;
                    break;
                }
            }
            UpdateLife(ref data);
            PreyLifetimes[i] = data;
        }

        // 3. Logique des PREDATEURS
        for (int i = 0; i < PredatorPositions.Length; i++)
        {
            var data = PredatorLifetimes[i];
            data.DecreasingFactor = 1.0f;

            // Touché par prédateur -> reproduction
            for (int j = 0; j < PredatorPositions.Length; j++)
            {
                if (i != j && math.distancesq(PredatorPositions[i], PredatorPositions[j]) < TouchingDistanceSq)
                {
                    data.Reproduced = true;
                    break;
                }
            }
            // Touché par proie -> vitesse / 2
            for (int j = 0; j < PreyPositions.Length; j++)
            {
                if (math.distancesq(PredatorPositions[i], PreyPositions[j]) < TouchingDistanceSq)
                {
                    data.DecreasingFactor /= 2.0f;
                }
            }
            UpdateLife(ref data);
            PredatorLifetimes[i] = data;
        }
    }

    private void UpdateLife(ref LifetimeData data)
    {
        data.CurrentLifetime -= DeltaTime * data.DecreasingFactor;
    }
}