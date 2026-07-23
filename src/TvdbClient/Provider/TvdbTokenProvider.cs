using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tvdb.Abstractions;
using Tvdb.Handlers;

namespace Tvdb.Provider;

/// <summary>
/// Token Provider for TVDB
/// </summary>
/// <param name="options"></param>
/// <param name="logger"></param>
public class TvdbTokenProvider(IOptions<TvdbConfiguration> options, ILogger<TvdbTokenProvider> logger) : ITokenProvider
{
    #region Properties
    /// <inheritdoc/>
    public TvdbConfiguration Config => Options.Value;

    /// <inheritdoc/>
    public IOptions<TvdbConfiguration> Options { get; } = options;

    /// <inheritdoc/>
    public ILogger<TvdbTokenProvider> Logger { get; } = logger;

    /// <inheritdoc/>
    public Token Token { get; internal set; }
    #endregion

    #region Methods
    /// <inheritdoc/>
    public async Task<Token> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        /* Acquire new Token */
        if (Token is null || Token.IsTokenExpired)
        {
            try
            {
                var httpClient = new HttpClient();
                var requestBody = new StringContent(JsonSerializer.Serialize(new LoginRequestBody { Apikey = Config.ApiKey, Pin = Config.Pin }), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

                var response = await httpClient.PostAsync(Config.TokenUrl, requestBody, cancellationToken);
                if (!response.IsSuccessStatusCode) Logger.LogError("Failed acquiring Token");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<ApiResponseWrapper<Token>>(responseBody);
                if(responseData is null)
                {
                    Logger.LogError("Failed acquiring Token.");
                    throw new Exception("Failed acquiring Token.");
                }
                if (responseData is not null && !responseData.IsSuccess)
                {
                    Logger.LogError("Failed acquiring Token. {errorMessage}", responseData.ErrorMessage);
                    throw new Exception($"Failed acquiring Token. {responseData.ErrorMessage}");
                }
                var token = responseData!.Data;
                if (token is null)
                {
                    Logger.LogError("Failed deserializing Token response");
                    throw new Exception("Failed deserializing Token response");
                }
                Token = token;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed acquiring token. {errorMessage}", ex.Message);
                throw;
            }
        }

        return Token;
    }
    #endregion
}
