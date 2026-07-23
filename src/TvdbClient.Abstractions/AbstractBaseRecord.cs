namespace Tvdb.Abstractions;

/// <summary>
/// Base Record for all TVDB Records
/// </summary>

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public abstract class AbstractBaseRecord
{
    /// <summary>
    /// backing field for <see cref="AdditionalProperties"/>"/>
    /// </summary>
    private IDictionary<string, object>? _additionalProperties;

    /// <summary>
    /// Contains additional Properties that havent been mapped yet
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get => _additionalProperties ??= new Dictionary<string, object>();
        set => _additionalProperties = value;
    }
}
