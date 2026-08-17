using System.Collections.Concurrent;

namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>
/// Shared, thread-safe key/value memory used by agents to communicate indirectly.
/// Agents must never reference each other directly; all coordination happens through memory.
/// </summary>
public sealed class WorkingMemory
{
    private readonly ConcurrentDictionary<string, object?> _store = new();

    public void Set(string key, object? value) => _store[key] = value;

    public T? Get<T>(string key)
    {
        if (_store.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    public bool TryGet(string key, out object? value) => _store.TryGetValue(key, out value);

    public bool ContainsKey(string key) => _store.ContainsKey(key);

    public IReadOnlyDictionary<string, object?> Snapshot() => _store.ToDictionary(kv => kv.Key, kv => kv.Value);
}

/// <summary>
/// The context flowing through a single goal execution: memory, history, and metadata.
/// Agents interact with the outside world only through this context.
/// </summary>
public sealed class AgentExecutionContext
{
    public AgentGoal Goal { get; init; } = null!;

    public WorkingMemory Memory { get; } = new();

    public ExecutionHistory History { get; } = new();

    public ReplanningHistory ReplanningHistory { get; } = new();

    public List<AgentContribution> Contributions { get; } = new();

    public int CurrentIteration { get; set; }

    public int MaxIterations { get; set; } = 5;

    public void SetMemory(string key, object? value) => Memory.Set(key, value);

    public T? GetMemory<T>(string key) => Memory.Get<T>(key);
}
