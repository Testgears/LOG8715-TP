using System.Collections.Generic;
using UnityEngine;

public sealed class ColorSystem : ISystem {
    public string Name => nameof(ColorSystem);

    public void UpdateSystem() {
        var world = GameWorld.Instance;

        foreach (var id in world.Query<SizeComponent, PositionComponent>()) {
            if (world.Has<StaticTag>(id)) {
                ECSController.Instance.UpdateShapeColor(id, Color.red);
                continue;
            }

            if (world.Has<JustCollidedTag>(id)) {
                ECSController.Instance.UpdateShapeColor(id, Color.green);
                continue;
            }

            if (world.Has<ExplosionChildTag>(id))
            {
                ECSController.Instance.UpdateShapeColor(id, new Color(1.0f, 0.0f, 0.5f)); // pink
                continue;
            }

            if (world.Has<WillExplodeTag>(id))
            {
                ECSController.Instance.UpdateShapeColor(id, new Color(1.0f, 0.5f, 0.0f)); // orange
                continue;
            }

            if (world.Has<ProtectedComponent>(id)) {
                ECSController.Instance.UpdateShapeColor(id, Color.white);
                continue;
            }

            if (world.Has<EligibleForProtectionComponent>(id)) {
                ECSController.Instance.UpdateShapeColor(id, new Color(0.6f, 0.85f, 1f)); // light blue
                continue;
            }

            if (world.Has<ProtectionCooldownComponent>(id)) {
                ECSController.Instance.UpdateShapeColor(id, Color.yellow);
                continue;
            }

            ECSController.Instance.UpdateShapeColor(id, new Color(0.05f, 0.1f, 0.5f)); // dark navy
        }

        var collidedIds = new List<uint>();
        foreach (var id in world.Query<JustCollidedTag, PositionComponent>())
            collidedIds.Add(id);

        for (int i = 0; i < collidedIds.Count; i++)
            world.Remove<JustCollidedTag>(collidedIds[i]);

        var childIds = new List<uint>();
        foreach (var id in world.Query<ExplosionChildTag, PositionComponent>())
            childIds.Add(id);

        for (int i = 0; i < childIds.Count; i++)
            world.Remove<ExplosionChildTag>(childIds[i]);
    }
}