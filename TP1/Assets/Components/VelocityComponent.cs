using UnityEngine;

public sealed class VelocityComponent : IComponent {
    public Vector2 Value;
    public VelocityComponent(Vector2 value) => Value = value;
}
