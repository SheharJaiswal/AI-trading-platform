namespace AiTrading.Application.Tests;

public sealed class LiveExecutionCredentialReferenceTests
{
    [Fact]
    public void Create_Returns_Trimmed_Reference_Metadata()
    {
        var reference = LiveExecutionCredentialReference.Create(
            " broker ",
            " secret://live/account ",
            " account-1 ",
            " production ");

        Assert.Equal("broker", reference.Provider);
        Assert.Equal("secret://live/account", reference.SecretReference);
        Assert.Equal("account-1", reference.AccountId);
        Assert.Equal("production", reference.Environment);
    }

    [Theory]
    [InlineData(null, "secret", "account", "production", "provider")]
    [InlineData("", "secret", "account", "production", "provider")]
    [InlineData("broker", null, "account", "production", "secret reference")]
    [InlineData("broker", "", "account", "production", "secret reference")]
    [InlineData("broker", "secret", null, "production", "account context")]
    [InlineData("broker", "secret", "", "production", "account context")]
    [InlineData("broker", "secret", "account", null, "environment context")]
    [InlineData("broker", "secret", "account", "", "environment context")]
    public void Create_Rejects_Missing_Metadata(
        string? provider,
        string? secretReference,
        string? accountId,
        string? environment,
        string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LiveExecutionCredentialReference.Create(provider!, secretReference!, accountId!, environment!));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reference_Does_Not_Contain_Credential_Value()
    {
        var reference = LiveExecutionCredentialReference.Create(
            "broker",
            "secret://live/account",
            "account-1",
            "production");

        var properties = typeof(LiveExecutionCredentialReference)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Secret", properties, StringComparer.Ordinal);
        Assert.Contains(nameof(reference.SecretReference), properties, StringComparer.Ordinal);
    }
}
