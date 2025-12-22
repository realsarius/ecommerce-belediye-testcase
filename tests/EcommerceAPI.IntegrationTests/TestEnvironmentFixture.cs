namespace EcommerceAPI.IntegrationTests;

// ortam değişkenlerini set eden fixture
// WebApplicationFactory calismadan once env ayarlanmis olur

public class TestEnvironmentFixture
{
    public TestEnvironmentFixture()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "12345678901234567890123456789012"); // 32-char for AES-256
        Environment.SetEnvironmentVariable("HASH_PEPPER", "test-pepper-value");
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<TestEnvironmentFixture> { }
