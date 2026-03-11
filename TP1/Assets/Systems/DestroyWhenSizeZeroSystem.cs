using System.Collections.Generic;

public sealed class DestroyWhenSizeZeroSystem : ISystem {
    public string Name => nameof(DestroyWhenSizeZeroSystem);

    public void UpdateSystem() {
        var world = GameWorld.Instance;

        var toDestroy = new List<uint>();

        foreach (var id in world.Query<PositionComponent, SizeComponent>()) {
            if (!world.TryGet(id, out SizeComponent size))
                continue;

            if (size.Value <= 0)
                toDestroy.Add(id);
        }

        foreach (var id in toDestroy) {
            world.DestroyEntity(id);
            ECSController.Instance.DestroyShape(id);
        }
    }
}