using UnityEngine;

public sealed class WallBounceSystem : ISystem {
    public string Name => nameof(WallBounceSystem);

    public void UpdateSystem() {
        var world = GameWorld.Instance;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth;
        float maxX = camPos.x + halfWidth;
        float minY = camPos.y - halfHeight;
        float maxY = camPos.y + halfHeight;

        foreach (var id in world.Query<PositionComponent, VelocityComponent>()) {
            if (!world.Has<DynamicTag>(id))
                continue;

            if (!world.TryGet(id, out PositionComponent pos) ||
                !world.TryGet(id, out VelocityComponent vel) ||
                !world.TryGet(id, out SizeComponent size))
                continue;

            float r = size.Value * 0.5f; 

            bool bounced = false;

            if (pos.Value.x - r < minX) { pos.Value.x = minX + r; vel.Value.x = -vel.Value.x; bounced = true; } else if (pos.Value.x + r > maxX) { pos.Value.x = maxX - r; vel.Value.x = -vel.Value.x; bounced = true; }

            if (pos.Value.y - r < minY) { pos.Value.y = minY + r; vel.Value.y = -vel.Value.y; bounced = true; } else if (pos.Value.y + r > maxY) { pos.Value.y = maxY - r; vel.Value.y = -vel.Value.y; bounced = true; }

            if (bounced) { 
                ECSController.Instance.UpdateShapePosition(id, pos.Value);
                if (!world.Has<JustCollidedTag>(id))
                    world.Add(id, new JustCollidedTag());
            }
        }
    }
}