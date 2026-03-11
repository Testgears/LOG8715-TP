using System.Collections.Generic;
using UnityEngine;

public sealed class ExplosionSystem : ISystem {
    public string Name => nameof(ExplosionSystem);

    public void UpdateSystem()
    {
        var world = GameWorld.Instance;
        var config = ECSController.Instance.Config;

        int explosionSize = config.explosionSize;
        var toExplode = new List<uint>();

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            foreach (var id in world.Query<PositionComponent, SizeComponent>())
            {
                if (world.Has<StaticTag>(id))
                    continue;

                world.TryGet(id, out PositionComponent p);
                world.TryGet(id, out SizeComponent s);

                if (Vector2.Distance(mousePos, p.Value) < s.Value * 0.5f)
                {
                    if (s.Value >= 4)
                    {
                        Explode(world, explosionSize, id);
                    }
                    else
                    {
                        world.DestroyEntity(id);
                        ECSController.Instance.DestroyShape(id);
                    }
                    break;
                }
            }
        }

        foreach (var id in world.Query<SizeComponent, PositionComponent>())
        {
            if (world.Has<StaticTag>(id))
                continue;

            if (world.TryGet(id, out SizeComponent size) && size.Value >= explosionSize)
            {
                toExplode.Add(id);
            }
        }

        foreach (var id in toExplode)
        {
            Explode(world, explosionSize, id);
        }
    }

    public void Explode(World world, int explosionSize, uint id)
    {
        if (!world.TryGet(id, out PositionComponent pos) || !world.TryGet(id, out SizeComponent size))
            return;

        int oldSize = size.Value;
        int newSize = Mathf.Max(1, oldSize / 4);

        Vector2 baseVel = Vector2.zero;
        if (world.TryGet(id, out VelocityComponent velComp))
            baseVel = velComp.Value;

        float speed = baseVel.magnitude;
        if (speed < 0.0001f) speed = 1f;

        Vector2[] dirs = {
            new Vector2( 1,  1).normalized,
            new Vector2( 1, -1).normalized,
            new Vector2(-1,  1).normalized,
            new Vector2(-1, -1).normalized
        };

        for (int k = 0; k < 4; k++)
        {
            uint child = world.CreateEntity();

            world.Add(child, new PositionComponent(pos.Value));
            world.Add(child, new VelocityComponent(dirs[k] * speed));
            world.Add(child, new SizeComponent(newSize));
            world.Add(child, new DynamicTag());
            world.Add(child, new RewindComponent());
            world.Add(child, new ExplosionChildTag());

            ECSController.Instance.CreateShape(child, newSize);
            ECSController.Instance.UpdateShapePosition(child, pos.Value);
            ECSController.Instance.UpdateShapeSize(child, newSize);
        }

        world.DestroyEntity(id);
        ECSController.Instance.DestroyShape(id);
    }
}