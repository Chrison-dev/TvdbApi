using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using Serilog;

/// <summary>
/// Fallout build for the TheTVDB v4 client.
///
/// The <c>Generate</c> target owns codegen in-stack (NSwag's C# API — no CLI, no
/// .nswag config, no external scripts). It downloads the live v4 OpenAPI spec,
/// applies the overlay in-memory (TheTVDB types integer resource ids as
/// <c>number</c> → C# <c>double</c>; we coerce the path-id params to
/// integer/int64 → <c>long</c>), then emits the clients + DTOs. It is deliberately
/// NOT wired into any CI workflow — run it locally via <c>./build.ps1 Generate</c>.
/// </summary>
partial class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Generate);

    const string SpecUrl = "https://thetvdb.github.io/v4-api/swagger.yml";

    AbsolutePath ClientProjectDirectory => RootDirectory / "src" / "TvdbClient";

    Target Generate => _ => _
        .Description("Regenerate the TheTVDB v4 client from the live OpenAPI spec")
        .Executes(async () =>
        {
            Log.Information("Downloading spec: {Url}", SpecUrl);
            var document = await OpenApiYamlDocument.FromUrlAsync(SpecUrl);

            var coerced = CoerceIntegerIdPathParameters(document);
            Log.Information("Overlay: coerced {Count} id path param(s) number → int64", coerced);

            var settings = new CSharpClientGeneratorSettings
            {
                ClassName = "{controller}Client",
                ClientBaseInterface = "ITvdbClient",
                InjectHttpClient = true,
                DisposeHttpClient = true,
                GenerateClientInterfaces = true,
                GenerateExceptionClasses = true,
                ExceptionClass = "ApiException",
                WrapDtoExceptions = true,
                UseBaseUrl = false,
                GenerateBaseUrlProperty = true,
                GenerateSyncMethods = false,
                GenerateOptionalParameters = true,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator(),
            };
            settings.CSharpGeneratorSettings.Namespace = "Tvdb.Models";
            settings.CSharpGeneratorSettings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
            settings.CSharpGeneratorSettings.ClassStyle = CSharpClassStyle.Poco;
            settings.CSharpGeneratorSettings.GenerateDataAnnotations = true;
            settings.CSharpGeneratorSettings.GenerateOptionalPropertiesAsNullable = true;
            settings.CSharpGeneratorSettings.RequiredPropertiesMustBeDefined = true;
            settings.CSharpGeneratorSettings.DateType = "System.DateTimeOffset";
            settings.CSharpGeneratorSettings.DateTimeType = "System.DateTimeOffset";
            settings.CSharpGeneratorSettings.TimeType = "System.TimeSpan";
            settings.CSharpGeneratorSettings.TimeSpanType = "System.TimeSpan";

            var generator = new CSharpClientGenerator(document, settings);

            var clients = generator.GenerateFile(ClientGeneratorOutputType.Implementation);
            var contracts = generator.GenerateFile(ClientGeneratorOutputType.Contracts);

            var clientsPath = ClientProjectDirectory / "Clients" / "TvdbClient.cs";
            var contractsPath = ClientProjectDirectory / "Models" / "TvdbModels.cs";
            clientsPath.WriteAllText(clients);
            contractsPath.WriteAllText(contracts);

            Log.Information("Wrote {Clients} and {Contracts}", clientsPath, contractsPath);
        });

    /// <summary>
    /// TheTVDB types integer resource ids as <c>number</c>, which NSwag maps to
    /// <c>double</c>. Coerce the path-id parameters to integer/int64. Narrow by
    /// design: the only other <c>number</c> fields are the genuinely-float
    /// <c>score</c> properties, which must stay <c>double</c>.
    /// </summary>
    static int CoerceIntegerIdPathParameters(OpenApiDocument document)
    {
        var count = 0;
        foreach (var pathItem in document.Paths.Values)
        foreach (var operation in pathItem.Values)
        foreach (var parameter in operation.Parameters
                     .Where(p => p.Kind == OpenApiParameterKind.Path))
        {
            var schema = parameter.Schema;
            if (schema is { Type: JsonObjectType.Number })
            {
                schema.Type = JsonObjectType.Integer;
                schema.Format = "int64";
                count++;
            }
        }

        return count;
    }
}
