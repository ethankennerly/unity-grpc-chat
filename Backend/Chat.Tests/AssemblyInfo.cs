using Xunit;

/// <summary>
/// Keep backend tests simple and deterministic.
/// </summary>
[assembly: CollectionBehavior(DisableTestParallelization = true)]
