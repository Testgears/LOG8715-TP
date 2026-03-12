using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LifetimeManager : MonoBehaviour
{
    // Listes de données persistantes pour éviter les allocations à chaque frame
    private NativeArray<LifetimeData> _plantLifetimes;
    private NativeArray<LifetimeData> _preyLifetimes;
    private NativeArray<LifetimeData> _predatorLifetimes;
    private bool _isInitialized = false;

    void Start()
    {
        //
    }

    void Update()
    {
        // Si le spawner n'a pas encore créé les objets, on ne fait rien
        // On vérifie si le Spawner a fini son travail
        if (!_isInitialized)
        {
            // On vérifie que le Spawner a fini de remplir ses tableaux statiques
            if (Ex4Spawner.PlantLifetimes != null && Ex4Spawner.PlantLifetimes.Length > 0)
            {
                Initialize();
            }
            else
            {
                return; // On attend la frame suivante
            }
        }
        int plantCount = Ex4Spawner.PlantTransforms.Length;
        int preyCount = Ex4Spawner.PreyTransforms.Length;
        int predCount = Ex4Spawner.PredatorTransforms.Length;

        // 1. Préparation des positions (nécessaire car les objets bougent)
        var plantPos = new NativeArray<float3>(plantCount, Allocator.TempJob);
        var preyPos = new NativeArray<float3>(preyCount, Allocator.TempJob);
        var predPos = new NativeArray<float3>(predCount, Allocator.TempJob);

        for (int i = 0; i < plantCount; i++) plantPos[i] = Ex4Spawner.PlantTransforms[i].position;
        for (int i = 0; i < preyCount; i++) preyPos[i] = Ex4Spawner.PreyTransforms[i].position;
        for (int i = 0; i < predCount; i++) predPos[i] = Ex4Spawner.PredatorTransforms[i].position;

        // 2. Configuration et planification des 3 Jobs Parallèles
        float distSq = Ex3Config.TouchingDistance * Ex3Config.TouchingDistance;

        var plantJob = new UpdatePlantLifetimeJob
        {
            PlantPositions = plantPos,
            PreyPositions = preyPos,
            PlantLifetimes = _plantLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq
        };

        var preyJob = new UpdatePreyLifetimeJob
        {
            PreyPositions = preyPos,
            PlantPositions = plantPos,
            PredatorPositions = predPos,
            PreyLifetimes = _preyLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq
        };

        var predJob = new UpdatePredatorLifetimeJob
        {
            PredatorPositions = predPos,
            PreyPositions = preyPos,
            PredatorLifetimes = _predatorLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq
        };

        // Lancement en parallèle (on combine les handles pour attendre les 3 en même temps)
        JobHandle plantHandle = plantJob.Schedule(plantCount, 64);
        JobHandle preyHandle = preyJob.Schedule(preyCount, 64);
        JobHandle predHandle = predJob.Schedule(predCount, 64);

        // On attend que les 3 jobs soient finis
        JobHandle.CombineDependencies(plantHandle, preyHandle, predHandle).Complete();

        // 4. Post-traitement : Appliquer les morts et les respawns
        ApplyResults(Ex4Spawner.PlantTransforms, Ex4Spawner.PlantLifetimes, _plantLifetimes);
        ApplyResults(Ex4Spawner.PreyTransforms, Ex4Spawner.PreyLifetimes, _preyLifetimes);
        ApplyResults(Ex4Spawner.PredatorTransforms, Ex4Spawner.PredatorLifetimes, _predatorLifetimes);

        // 5. Nettoyage des positions temporaires
        plantPos.Dispose();
        preyPos.Dispose();
        predPos.Dispose();
    }

    private void ApplyResults(Transform[] transforms, Lifetime[] lifetimes, NativeArray<LifetimeData> dataArray)
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            if (!transforms[i].gameObject.activeSelf) continue;

            // --- SYNCHRONISATION CRITIQUE ---
            // On renvoie la vie calculée par le Job vers le MonoBehaviour
            // pour que le script 'Plant' puisse la lire via GetProgression()
            lifetimes[i].CurrentLifetime = dataArray[i].CurrentLifetime;

            lifetimes[i].reproduced = dataArray[i].Reproduced;
            lifetimes[i].decreasingFactor = dataArray[i].DecreasingFactor;

            // Logique de mort / respawn
            if (dataArray[i].CurrentLifetime <= 0)
            {

                // Logique de mort / respawn (on réutilise la logique existante de Lifetime.cs)
                if (dataArray[i].CurrentLifetime <= 0)
                {
                    if (dataArray[i].Reproduced || lifetimes[i].alwaysReproduce)
                    {
                        // Reset des données pour le respawn
                        var newData = dataArray[i];
                        newData.CurrentLifetime = newData.StartingLifetime;
                        newData.Reproduced = false;
                        dataArray[i] = newData;

                        Ex4Spawner.Instance.Respawn(transforms[i]);
                    }
                    else
                    {
                        transforms[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SyncFromMonoBehaviours()
    {
        // Helper pour remplir les NativeArrays au tout début
        FillData(_plantLifetimes, Ex4Spawner.PlantLifetimes);
        FillData(_preyLifetimes, Ex4Spawner.PreyLifetimes);
        FillData(_predatorLifetimes, Ex4Spawner.PredatorLifetimes);
    }

    private void FillData(NativeArray<LifetimeData> target, Lifetime[] source)
    {
        for (int i = 0; i < source.Length; i++)
        {
            target[i] = new LifetimeData
            {
                StartingLifetime = source[i].StartingLifetime, // Utilise la propriété publique
                CurrentLifetime = source[i].CurrentLifetime,   // Utilise la propriété publique
                DecreasingFactor = source[i].decreasingFactor,
                Reproduced = false
            };
        }
    }

    private void Initialize()
    {
        // On crée les NativeArrays seulement maintenant
        _plantLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PlantLifetimes.Length, Allocator.Persistent);
        _preyLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PreyLifetimes.Length, Allocator.Persistent);
        _predatorLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PredatorLifetimes.Length, Allocator.Persistent);

        SyncFromMonoBehaviours();
        _isInitialized = true;
        Debug.Log("LifetimeManager Initialized Successfully");
    }

    // N'oubliez pas le OnDestroy pour éviter les fuites de mémoire (Memory Leaks)
    private void OnDestroy()
    {
        if (_plantLifetimes.IsCreated) _plantLifetimes.Dispose();
        if (_preyLifetimes.IsCreated) _preyLifetimes.Dispose();
        if (_predatorLifetimes.IsCreated) _predatorLifetimes.Dispose();
    }
}