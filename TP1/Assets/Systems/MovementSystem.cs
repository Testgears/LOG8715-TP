using UnityEngine;

public sealed class MovementSystem : ISystem {
    public string Name => nameof(MovementSystem);

    public void UpdateSystem()
    {
        var world = GameWorld.Instance;
        float dt = Time.deltaTime;

        RewindGlobalState globalState = null;
        foreach (var id in world.Query<RewindGlobalState, PositionComponent>())
        {
            if (world.TryGet(id, out globalState)) break;
        }


        for (int step = 0; step < 4; step++)
        {
            foreach (var id in world.Query<PositionComponent, VelocityComponent>())
            {
                if (!world.TryGet(id, out PositionComponent pos) ||
                    !world.TryGet(id, out VelocityComponent vel)) continue;

                // Left moves 4 times per frame, right moves only on the 1st step
                if (pos.Value.x < 0 || step == 0)
                {
                    pos.Value += vel.Value * dt;
                    ECSController.Instance.UpdateShapePosition(id, pos.Value);
                }
            }

            // Record the state of the world after every simulation step (need to be based on x4 FPS so that the rewind can get the exact frame)
            if (RewindSystem.Instance != null && globalState != null)
            {
                RewindSystem.Instance.RecordHistory(world, globalState);
            }
        }
    }

}
