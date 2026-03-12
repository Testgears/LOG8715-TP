using UnityEngine;

public class Lifetime : MonoBehaviour
{
    private const float StartingLifetimeLowerBound = 5;
    private const float StartingLifetimeUpperBound = 15;

    public float decreasingFactor = 1;
    public bool alwaysReproduce;
    public bool reproduced;

    private float _startingLifetime;
    private float _lifetime;

    // Propriété publique pour permettre au LifetimeManager de mettre à jour la vie
    public float CurrentLifetime
    {
        get => _lifetime;
        set => _lifetime = value;
    }

    // Propriété publique pour l'initialisation du NativeArray dans le Manager
    public float StartingLifetime => _startingLifetime;

    public float GetProgression()
    {
        // Sécurité pour éviter la division par zéro
        if (_startingLifetime <= 0) return 0;
        return Mathf.Clamp01(_lifetime / _startingLifetime);
    }

    void Start()
    {
        InitializeLifetime();
    }

    // Extraction de l'initialisation pour pouvoir la rappeler lors du Respawn
    public void InitializeLifetime()
    {
        reproduced = false;
        decreasingFactor = 1f;
        _startingLifetime = Random.Range(StartingLifetimeLowerBound, StartingLifetimeUpperBound);
        _lifetime = _startingLifetime;
    }

    // Note : L'Update est conservé uniquement pour la désactivation visuelle
    // La réduction de _lifetime est effectuée par UpdateLifetimeJob
    void Update()
    {
        // La condition de mort est maintenant pilotée par la donnée 
        // synchronisée depuis le LifetimeManager
        if (_lifetime <= 0)
        {
            if (reproduced || alwaysReproduce)
            {
                // Le Respawn est géré ici ou par le ApplyResults du Manager
                InitializeLifetime();
                Ex4Spawner.Instance.Respawn(transform);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}