
namespace Tvdb.Abstractions;

/// <summary>
/// Provider for Tokens
/// </summary>
public interface ITokenProvider
{
    #region Properties
    /// <summary>
    /// The current <see cref="Token"/>
    /// </summary>
    Token Token { get; }
    #endregion

    #region Methods
    /// <summary>
    /// Acquire a <see cref="Token"/>
    /// </summary>
    /// <returns><see cref="Token"/></returns>
    public Task<Token> AcquireTokenAsync(CancellationToken cancellationToken = default);
    #endregion
}