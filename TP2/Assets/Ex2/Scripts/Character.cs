using UnityEngine;

public class Character : MonoBehaviour
{
    private Vector3 _velocity = Vector3.zero;

    private Vector3 _acceleration = Vector3.zero;

    private const float AccelerationMagnitude = 2;

    private const float MaxVelocityMagnitude = 5;

    private const float DamagePerSecond = 50;

    private const float DamageRange = 10;

    // FIX Odeur 1 : remplacement de OverlapCircleAll (10 appels/frame, 20,1 KB d'allocations GC/frame)
    // par OverlapCircleNonAlloc utilisant des buffers préalloués par instance et élimine la pression sur le GC.
    // Des buffers séparés sont utilisés par méthode afin que chaque requête écrive indépendamment
    // sans écraser les résultats de l'autre.
    // Les buffers sont propres à chaque instance (non statiques) afin que plusieurs instances de Character
    // ne se réécrivent pas mutuellement.
    // Une taille de 512 dépasse le nombre total de cercles afin d'éviter toute troncature silencieuse des résultats.
    private static readonly Collider2D[] DamageBuffer = new Collider2D[512];
    private readonly Collider2D[] AccelerationBuffer = new Collider2D[512];

    private void Update()
    {
        Move();
        DamageNearbyShapes();
        UpdateAcceleration();
    }

    private void Move()
    {
        _velocity += _acceleration * Time.deltaTime;
        if (_velocity.magnitude > MaxVelocityMagnitude)
        {
            _velocity = _velocity.normalized * MaxVelocityMagnitude;
        }
        transform.position += _velocity * Time.deltaTime;
    }

    private void UpdateAcceleration()
    {
        var direction = Vector3.zero;

        /*var nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DamageRange);
        foreach (var nearbyCollider in nearbyColliders)
        {
            if (nearbyCollider.TryGetComponent<Circle>(out var circle))
            {
                direction += (circle.transform.position - transform.position) * circle.Health;
            }
        }*/
    
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, DamageRange, AccelerationBuffer);
        for (int k = 0; k < hitCount; k++)
        {
            if (AccelerationBuffer[k].TryGetComponent<Circle>(out var circle))
            {
                direction += (circle.transform.position - transform.position) * circle.Health;
            }
        }
        _acceleration = direction.normalized * AccelerationMagnitude;
    }

    private void DamageNearbyShapes()
    {
        /*var nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DamageRange);

        // Si aucun cercle proche, on retourne a (0,0,0)
        if (nearbyColliders.Length == 0)
        {
            transform.position = Vector3.zero;
        }

        foreach(var nearbyCollider in nearbyColliders)
        {
            if (nearbyCollider.TryGetComponent<Circle>(out var circle))
            {
                circle.ReceiveHp(-DamagePerSecond * Time.deltaTime);
            }
        }*/
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, DamageRange, DamageBuffer);
 
        // Si aucun cercle proche, on retourne a (0,0,0)
        if (hitCount == 0)
        {
            transform.position = Vector3.zero;
        }
 
        for (int k = 0; k < hitCount; k++)
        {
            if (DamageBuffer[k].TryGetComponent<Circle>(out var circle))
            {
                circle.ReceiveHp(-DamagePerSecond * Time.deltaTime);
            }
        }
    }
}
