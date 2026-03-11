using UnityEngine;

public sealed class PositionComponent : IComponent {
    public Vector2 Value;
    public PositionComponent(Vector2 value) => Value = value;
}
