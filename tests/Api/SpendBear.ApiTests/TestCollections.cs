using Xunit;

namespace SpendBear.ApiTests;

/// <summary>
/// Defines test collections to control test execution order.
/// Tests in the same collection run sequentially, avoiding Serilog and resource conflicts.
/// </summary>
[CollectionDefinition("API Tests", DisableParallelization = true)]
public class ApiTestsCollection
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to be the place to apply [CollectionDefinition] and collection attributes.
}
