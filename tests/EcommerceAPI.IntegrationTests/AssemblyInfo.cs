using Xunit;

// Disable parallel test execution to avoid:
// 1. Rate limiter state conflicts
// 2. Shared WebApplicationFactory state issues
// 3. Database isolation problems
[assembly: CollectionBehavior(DisableTestParallelization = true)]
