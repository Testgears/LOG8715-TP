using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnSystem : ISystem {
    public string Name => nameof(SpawnSystem);

    private bool _spawned = false;

    public static readonly List<uint> SpawnedEntities = new();

    public void UpdateSystem() {
        if (_spawned) return;
        _spawned = true;

        var config = ECSController.Instance.Config; 
        var world = GameWorld.Instance;

        if (config.circleInstancesToSpawn == null || config.circleInstancesToSpawn.Count == 0) {
            Debug.LogWarning("[SpawnSystem] No circles to spawn in config.circleInstancesToSpawn");
            return;
        }

        foreach (var shape in config.circleInstancesToSpawn) {
            uint id = world.CreateEntity();
            SpawnedEntities.Add(id);

            world.Add(id, new PositionComponent(shape.initialPosition));
            world.Add(id, new SizeComponent(shape.initialSize));
            world.Add(id, new RewindComponent());

            bool isStatic = shape.initialVelocity.sqrMagnitude <= 0.000001f;

            if (isStatic) {
                world.Add(id, new StaticTag());
            } else {
                world.Add(id, new VelocityComponent(shape.initialVelocity));
                world.Add(id, new DynamicTag());
            }

            ECSController.Instance.CreateShape(id, shape.initialSize);
            ECSController.Instance.UpdateShapePosition(id, shape.initialPosition);
            ECSController.Instance.UpdateShapeSize(id, shape.initialSize);
        }

        uint globalTimerId = world.CreateEntity();
        world.Add(globalTimerId, new RewindGlobalState());
        world.Add(globalTimerId, new PositionComponent(Vector2.zero ));

        Debug.Log($"[SpawnSystem] Spawned {config.circleInstancesToSpawn.Count} circles.");
    }
}
