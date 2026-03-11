using UnityEngine;

public sealed class SizeComponent : IComponent {
    public int Value;
    public SizeComponent(int value) => Value = value;
}
