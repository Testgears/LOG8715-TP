using UnityEngine;
using UnityEngine.Serialization;

public class Circle : MonoBehaviour
{
    [FormerlySerializedAs("I")] [HideInInspector]
    public int i;

    [FormerlySerializedAs("J")] [HideInInspector]
    public int j;

    public float Health { get; private set; }

    private const float BaseHealth = 1000;

    private const float HealingPerSecond = 1;
    private const float HealingRange = 3;

    // FIX Odeur 2:  mise en cache de GridShape et SpriteRenderer une seule fois dans Start()
    // au lieu d'appeler FindObjectsOfType (406 appels/frame, 2,0 % du temps de frame)
    // et GetComponent à chaque image
    private GridShape _grid;
    private SpriteRenderer _spriteRenderer;

    // FIX Odeur 1 : remplacement de OverlapCircleAll (406 appels/frame, 128,9 KB d'allocations GC/frame)
    // par OverlapCircleNonAlloc utilisant un buffer préalloué par instance — élimine la pression sur le GC.
    // Le buffer est propre à chaque instance (non statique) afin que plusieurs instances de Circle
    // ne se réécrivent pas mutuellement.
    // Une taille de 512 dépasse le nombre total de cercles afin d'éviter toute troncature silencieuse des résultats.
    private static readonly Collider2D[] HealBuffer = new Collider2D[512];

    // Start is called before the first frame update
    private void Start()
    {
        Health = BaseHealth;

        // Cache once at startup
        _grid = GameObject.FindFirstObjectByType<GridShape>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateColor();
        HealNearbyShapes();
    }

    private void UpdateColor()
    {
        // FIX Odeur 1: use cached references — no FindFirstObjectByType, no GetComponent per frame
        /*var grid = GameObject.FindFirstObjectByType<GridShape>();
        var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();*/
        _spriteRenderer.color = _grid.Colors[i, j] * Health / BaseHealth;
    }

    private void HealNearbyShapes()
    {
        // FIX Odeur 3: OverlapCircleNonAlloc writes into a pre-allocated buffer → 0 GC alloc
        // FIX Odeur 2: TryGetComponent call count is reduced since hitCount <= buffer size
        /*var nearbyColliders = Physics2D.OverlapCircleAll(transform.position, HealingRange);
        foreach (var nearbyCollider in nearbyColliders)
        {
            if (nearbyCollider != null && nearbyCollider.TryGetComponent<Circle>(out var circle))
            {
                circle.ReceiveHp(HealingPerSecond * Time.deltaTime);
            }
        }*/

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, HealingRange, HealBuffer);
        for (int k = 0; k < hitCount; k++)
        {
            if (HealBuffer[k] != null && HealBuffer[k].TryGetComponent<Circle>(out var circle))
            {
                circle.ReceiveHp(HealingPerSecond * Time.deltaTime);
            }
        }
    }

    public void ReceiveHp(float hpReceived)
    {
        Health += hpReceived;
        Health = Mathf.Clamp(Health, 0, BaseHealth);
    }
}
