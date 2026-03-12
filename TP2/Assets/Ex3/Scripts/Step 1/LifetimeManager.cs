using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LifetimeManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float touchingDistance = 1.0f;
    [Range(1, 10)][SerializeField] private int updateRate = 4;

    private NativeArray<LifetimeData> _plantLifetimes;
    private NativeArray<LifetimeData> _preyLifetimes;
    private NativeArray<LifetimeData> _predatorLifetimes;
    private bool _isInitialized = false;

    void Update()
    {
        if (!_isInitialized)
        {
            if (Ex4Spawner.PlantLifetimes != null && Ex4Spawner.PlantLifetimes.Length > 0) Initialize();
            else return;
        }

        //Avant de lancer les jobs, on récupère l'état actuel des MonoBehaviours
        // Cela permet de détecter si une plante vient de respawn (vie remise à 10 par ex)
        SyncFromMonoBehaviours();

        // 1. Préparation des positions
        var plantPos = GetPositions(Ex4Spawner.PlantTransforms);
        var preyPos = GetPositions(Ex4Spawner.PreyTransforms);
        var predPos = GetPositions(Ex4Spawner.PredatorTransforms);

        float distSq = touchingDistance * touchingDistance;

        // 2. Configuration des Jobs
        var plantJob = new UpdatePlantLifetimeJob
        {
            PlantPositions = plantPos,
            PreyPositions = preyPos,
            PlantLifetimes = _plantLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq,
            FrameCount = Time.frameCount,
            UpdateRate = updateRate
        };

        var preyJob = new UpdatePreyLifetimeJob
        {
            PreyPositions = preyPos,
            PlantPositions = plantPos,
            PredatorPositions = predPos,
            PreyLifetimes = _preyLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq,
            FrameCount = Time.frameCount,
            UpdateRate = updateRate
        };

        var predJob = new UpdatePredatorLifetimeJob
        {
            PredatorPositions = predPos,
            PreyPositions = preyPos,
            PredatorLifetimes = _predatorLifetimes,
            DeltaTime = Time.deltaTime,
            TouchingDistanceSq = distSq,
            FrameCount = Time.frameCount,
            UpdateRate = updateRate
        };

        // 3. Planification en parallèle
        JobHandle p1 = plantJob.Schedule(_plantLifetimes.Length, 64);
        JobHandle p2 = preyJob.Schedule(_preyLifetimes.Length, 64);
        JobHandle p3 = predJob.Schedule(_predatorLifetimes.Length, 64);

        JobHandle.CombineDependencies(p1, p2, p3).Complete();

        // 4. Synchronisation vers les MonoBehaviours
        ApplyResults(Ex4Spawner.PlantLifetimes, _plantLifetimes);
        ApplyResults(Ex4Spawner.PreyLifetimes, _preyLifetimes);
        ApplyResults(Ex4Spawner.PredatorLifetimes, _predatorLifetimes);

        // Nettoyage temporaire
        plantPos.Dispose();
        preyPos.Dispose();
        predPos.Dispose();
    }

    private void SyncFromMonoBehaviours()
    {
        FillData(_plantLifetimes, Ex4Spawner.PlantLifetimes);
        FillData(_preyLifetimes, Ex4Spawner.PreyLifetimes);
        FillData(_predatorLifetimes, Ex4Spawner.PredatorLifetimes);
    }

    private NativeArray<float3> GetPositions(Transform[] transforms)
    {
        var posArray = new NativeArray<float3>(transforms.Length, Allocator.TempJob);
        for (int i = 0; i < transforms.Length; i++)
            posArray[i] = transforms[i].gameObject.activeInHierarchy ? (float3)transforms[i].position : new float3(float.MaxValue);
        return posArray;
    }

    private void ApplyResults(Lifetime[] mono, NativeArray<LifetimeData> data)
    {
        for (int i = 0; i < mono.Length; i++)
        {
            mono[i].CurrentLifetime = data[i].CurrentLifetime;
            mono[i].reproduced = data[i].Reproduced;
            // On peut aussi sync le decreasingFactor si besoin pour le debug
        }
    }

    private void Initialize()
    {
        _plantLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PlantLifetimes.Length, Allocator.Persistent);
        _preyLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PreyLifetimes.Length, Allocator.Persistent);
        _predatorLifetimes = new NativeArray<LifetimeData>(Ex4Spawner.PredatorLifetimes.Length, Allocator.Persistent);

        FillData(_plantLifetimes, Ex4Spawner.PlantLifetimes);
        FillData(_preyLifetimes, Ex4Spawner.PreyLifetimes);
        FillData(_predatorLifetimes, Ex4Spawner.PredatorLifetimes);

        _isInitialized = true;
    }

    private void FillData(NativeArray<LifetimeData> target, Lifetime[] source)
    {
        for (int i = 0; i < source.Length; i++)
        {
            target[i] = new LifetimeData
            {
                StartingLifetime = source[i].StartingLifetime,
                CurrentLifetime = source[i].CurrentLifetime, // Crucial : on prend la vie réelle
                DecreasingFactor = 1f,
                Reproduced = source[i].reproduced
            };
        }
    }

    private void OnDestroy()
    {
        if (_plantLifetimes.IsCreated) _plantLifetimes.Dispose();
        if (_preyLifetimes.IsCreated) _preyLifetimes.Dispose();
        if (_predatorLifetimes.IsCreated) _predatorLifetimes.Dispose();
    }
}