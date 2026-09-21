namespace AiTrading.Application;

/// <summary>
/// Identifies externally managed credentials without carrying secret material.
/// The reference may be persisted as configuration metadata; the credential value must remain outside trading-domain state.
/// </summary>
public sealed record LiveExecutionCredentialReference(
    string Provider,
    string SecretReference,
    string AccountId,
    string Environment)
{
    public static LiveExecutionCredentialReference Create(
        string provider,
        string secretReference,
        string accountId,
        string environment)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Credential provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(secretReference))
            throw new ArgumentException("Credential secret reference is required.", nameof(secretReference));
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Credential account context is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Credential environment context is required.", nameof(environment));

        return new(
            provider.Trim(),
            secretReference.Trim(),
            accountId.Trim(),
            environment.Trim());
    }
}
