using System.Collections.Generic;
using UnityEngine;

public sealed class ProtectionSystem : ISystem {
    public string Name => nameof(ProtectionSystem);

    public void UpdateSystem() {
        var world = GameWorld.Instance;
        var config = ECSController.Instance.Config;

        int protectionSize = config.protectionSize;
        float protectionDuration = config.protectionDuration;
        float protectionCooldown = config.protectionCooldown;

        float dt = Time.deltaTime;

        var protectedIds = new List<uint>();
        foreach (var id in world.Query<ProtectedComponent, SizeComponent>())
            protectedIds.Add(id);

        for (int i = 0; i < protectedIds.Count; i++) {
            uint id = protectedIds[i];

            if (!world.TryGet(id, out ProtectedComponent prot))
                continue;

            prot.TimeLeft -= dt;
            if (prot.TimeLeft <= 0f) {
                world.Remove<ProtectedComponent>(id);

                world.Add(id, new ProtectionCooldownComponent { TimeLeft = protectionCooldown });
            }
        }

        var cooldownIds = new List<uint>();
        foreach (var id in world.Query<ProtectionCooldownComponent, SizeComponent>())
            cooldownIds.Add(id);

        for (int i = 0; i < cooldownIds.Count; i++) {
            uint id = cooldownIds[i];

            if (!world.TryGet(id, out ProtectionCooldownComponent cd))
                continue;

            cd.TimeLeft -= dt;
            if (cd.TimeLeft <= 0f) {
                world.Remove<ProtectionCooldownComponent>(id);
            }
        }

        // Maybe we can integrate this transformation in the previous loops since every circle has a size / Position
        // More efficient vs maybe less clean code
        var sizeIds = new List<uint>();
        foreach (var id in world.Query<SizeComponent, PositionComponent>())
            sizeIds.Add(id);

        for (int i = 0; i < sizeIds.Count; i++) {
            uint id = sizeIds[i];

            if (!world.TryGet(id, out SizeComponent size))
                continue;

            if (world.Has<StaticTag>(id))
                continue;

            if (world.Has<ProtectedComponent>(id) || world.Has<ProtectionCooldownComponent>(id)) {
                if (world.Has<EligibleForProtectionComponent>(id))
                    world.Remove<EligibleForProtectionComponent>(id);

                continue;
            }

            if (size.Value <= protectionSize) {
                if (!world.Has<EligibleForProtectionComponent>(id))
                    world.Add(id, new EligibleForProtectionComponent { SameSizeCollisionCount = 0 });
            } else {
                if (world.Has<EligibleForProtectionComponent>(id))
                    world.Remove<EligibleForProtectionComponent>(id);
            }
        }
    }
}