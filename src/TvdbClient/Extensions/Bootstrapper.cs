using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tvdb.Abstractions;
using Tvdb.Provider;
using Tvdb.Handlers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for TVDB Client
/// </summary>
public static class Bootstrapper
{
    /// <summary>
    /// Add the TVDB Client to the configuration
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddTvdbClient(this IConfigurationBuilder builder)
    {
        var config = builder
            .AddJsonFile("TvdbClientConfig.json", optional: true)
            .Build();

        return builder;
    }

    /// <summary>
    /// Add the TVDB Client to the service collection
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddTvdbClient(this IServiceCollection builder, IConfiguration config)
    {
        /* Inject TVDB Clients */
        builder.Configure<TvdbConfiguration>(config.GetRequiredSection("TvdbConfiguration"));
        builder.TryAddSingleton<ITokenProvider, TvdbTokenProvider>();
        builder.TryAddTransient<TokenAuthorizationHeaderHandler>();

        string baseUrl = config.GetValue<string>("TvdbConfiguration:BaseUrl") ?? "https://api4.thetvdb.com/v4";
        builder
            .AddHttpClient(Tvdb.Constants.TvdbConstants.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(baseUrl.EnsureTrailingSlash());
            })
            .AddHttpMessageHandler<TokenAuthorizationHeaderHandler>();

        /* Inject all Tvdb Clients at once */
        builder.Scan(scan => scan
        .FromCallingAssembly()
        .AddClasses(classes => classes.AssignableTo<Tvdb.Abstractions.ITvdbClient>())
        .AsMatchingInterface()
        .AsHttpClient(Tvdb.Constants.TvdbConstants.HttpClientName)
        );

        return builder;
    }

    /// <summary>
    /// Ensure that the input string ends on a slash
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static string EnsureTrailingSlash(this string inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return string.Empty;

        if (!inputString.EndsWith('/')) inputString += '/';
        return inputString;
    }
}
