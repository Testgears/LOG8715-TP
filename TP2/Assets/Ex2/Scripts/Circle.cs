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

    private GridShape grid;
    private SpriteRenderer spriteRenderer;

    private readonly Collider2D[] nearbyCollidersBuffer = new Collider2D[32];

    // Start is called before the first frame update
    private void Start()
    {
        Health = BaseHealth;
        grid = GameObject.FindFirstObjectByType<GridShape>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateColor();
        HealNearbyShapes();
    }

    private void UpdateColor()
    {
        spriteRenderer.color = grid.Colors[i, j] * Health / BaseHealth;
    }

    private void HealNearbyShapes()
    {
        Vector3 position = transform.position;
        float healAmount = HealingPerSecond * Time.deltaTime;

        int count = Physics2D.OverlapCircle(position, HealingRange, new ContactFilter2D().NoFilter(), nearbyCollidersBuffer);

        for (int k = 0; k < count; k++)
        {
            var nearbyCollider = nearbyCollidersBuffer[k];
            if (nearbyCollider != null && nearbyCollider.TryGetComponent<Circle>(out var circle))
            {
                circle.ReceiveHp(healAmount);
            }
        }
    }

    public void ReceiveHp(float hpReceived)
    {
        Health += hpReceived;
        Health = Mathf.Clamp(Health, 0, BaseHealth);
    }
}