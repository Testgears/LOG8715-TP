using System.Collections.Generic;
using UnityEngine;

public sealed class RewindSystem : ISystem
{
    public string Name => nameof(RewindSystem);

    public static RewindSystem Instance { get; private set; }

    public RewindSystem() { Instance = this; }

    public void UpdateSystem()
    {
        var world = GameWorld.Instance;
        float dt = Time.deltaTime;

        RewindGlobalState globalState = null;
        foreach (var id in world.Query<RewindGlobalState, PositionComponent>())
        {
            if (world.TryGet(id, out globalState)) break;
        }

        if (globalState == null) return;

        if (globalState.CooldownRemaining > 0)
        {
            globalState.CooldownRemaining -= dt;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (globalState.CooldownRemaining > 0)
            {
                Debug.Log($"Attendez le cooldown : {globalState.CooldownRemaining:F1}s");
            }
            else
            {
                ExecuteRewind(world, globalState);
                globalState.CooldownRemaining = 3.0f;
            }
        }

        //RecordHistory(world, globalState);
    }

    public void RecordHistory(World world, RewindGlobalState globalState)
    {
        List<EntitySnapshot> currentFrameSnapshot = new List<EntitySnapshot>();

        foreach (var id in world.Query<RewindComponent, PositionComponent>())
        {
            var components = world.GetComponents(id);
            var clones = new List<IComponent>();

            foreach (var comp in components)
            {
                // Create a copy of the component's data, not a pointer
                var method = typeof(object).GetMethod("MemberwiseClone",
                             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                clones.Add((IComponent)method.Invoke(comp, null));
            }

            currentFrameSnapshot.Add(new EntitySnapshot { Components = clones });
        }

        globalState.WorldHistory.Enqueue(currentFrameSnapshot);

        int maxHistory = (int)((3.0f / Time.deltaTime) * 4);
        if (globalState.WorldHistory.Count > maxHistory) globalState.WorldHistory.Dequeue();
    }

    private void ExecuteRewind(World world, RewindGlobalState globalState)
    {
        if (globalState.WorldHistory.Count == 0) return;

        List<EntitySnapshot> oldState = globalState.WorldHistory.Peek();

        List<uint> toDestroy = new List<uint>();
        foreach (var id in world.Query<RewindComponent, PositionComponent>()) toDestroy.Add(id);

        foreach (var id in toDestroy)
        {
            ECSController.Instance.DestroyShape(id);
            world.DestroyEntity(id);
        }

        foreach (var snapshot in oldState)
        {
            uint newId = world.CreateEntity();
            foreach (var comp in snapshot.Components)
            {
                world.Add(newId, comp);
            }

            if (world.TryGet(newId, out SizeComponent size))
            {
                ECSController.Instance.CreateShape(newId, size.Value);
            }

            if (world.TryGet(newId, out PositionComponent pos))
            {
                ECSController.Instance.UpdateShapePosition(newId, pos.Value);
            }
        }

        globalState.WorldHistory.Clear();
    }
}