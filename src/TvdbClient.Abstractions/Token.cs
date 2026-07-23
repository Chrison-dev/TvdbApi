
namespace Tvdb.Abstractions;

/// <summary>
/// Bearer Token Model
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class Token
{
    /// <summary>
    /// Bearer Token
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// Timestamp of Creation
    /// </summary>
    public DateTime CreationTimestamp { get; } = DateTime.Now;

    /// <summary>
    /// Expiry Date
    /// </summary>
    /// <remarks>TVDB says their tokens last a month</remarks>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime TokenExpiryDate => CreationTimestamp.AddMonths(1);

    /// <summary>
    /// Has this Token expired?
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsTokenExpired => TokenExpiryDate < DateTime.Now;

    /// <summary>
    /// Token Type
    /// </summary>
    /// <remarks>Hardcoded Bearer even though its not quite a Bearer Token but ok</remarks>
    public static string TokenType => "Bearer";
}