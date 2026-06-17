using Xunit;

// Disable parallel test execution to prevent static Application.Current state from interfering across tests
[assembly: CollectionBehavior(DisableTestParallelization = true)]
