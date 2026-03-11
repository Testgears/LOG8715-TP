using System;
using System.Collections.Generic;


public class World {
    private uint _nextId = 1;

    private readonly Dictionary<Type, Dictionary<uint, IComponent>> _components = new();

    public uint CreateEntity() {
        return _nextId++;
    }

    public void Add<T>(uint entityId, T component) where T : class, IComponent {
        var type = typeof(T);

        if (!_components.ContainsKey(type))
            _components[type] = new Dictionary<uint, IComponent>();

        _components[type][entityId] = component;
    }

    public bool TryGet<T>(uint entityId, out T component) where T : class, IComponent {
        component = null;

        var type = typeof(T);
        if (!_components.ContainsKey(type))
            return false;

        if (_components[type].TryGetValue(entityId, out var value)) {
            component = value as T;
            return component != null;
        }

        return false;
    }

    public bool Has<T>(uint entityId) where T : class, IComponent {
        var type = typeof(T);
        return _components.ContainsKey(type) && _components[type].ContainsKey(entityId);
    }

    public void Remove<T>(uint entityId) where T : IComponent {
        var type = typeof(T);

        if (_components.TryGetValue(type, out var store)) {
            store.Remove(entityId);
        }
    }

    public IEnumerable<uint> Query<T1, T2>()
        where T1 : class, IComponent
        where T2 : class, IComponent {
        var t1 = typeof(T1);
        var t2 = typeof(T2);

        if (!_components.ContainsKey(t1) || !_components.ContainsKey(t2))
            yield break;

        foreach (var id in _components[t1].Keys) {
            if (_components[t2].ContainsKey(id))
                yield return id;
        }
    }

    public void DestroyEntity(uint entityId) {
        foreach (var store in _components.Values)
            store.Remove(entityId);
    }

    public void Add(uint entityId, IComponent component)
    {
        var type = component.GetType();
        if (!_components.ContainsKey(type)) _components[type] = new Dictionary<uint, IComponent>();
        _components[type][entityId] = component;
    }

    public List<IComponent> GetComponents(uint entityId)
    {
        var list = new List<IComponent>();
        foreach (var store in _components.Values)
        {
            if (store.TryGetValue(entityId, out var comp)) list.Add(comp);
        }
        return list;
    }
}
