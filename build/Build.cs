using System.Linq;
using System.Threading.Tasks;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// Fallout build for the TheTVDB v4 client.
///
/// CI (build.yml, auto-generated from the [GitHubActions] attribute) runs Test + Pack
/// on pushes/PRs to main. The <c>Generate</c> target owns codegen in-stack (NSwag's
/// C# API — no CLI, no .nswag config, no external scripts) and is deliberately NOT in
/// CI — run it locally via <c>./build.ps1 Generate</c>. Publishing to NuGet uses
/// trusted publishing (OIDC) via a dedicated workflow (Fallout has no built-in OIDC).
/// </summary>
// AutoGenerate=false: the workflow was generated from this attribute, but its run
// step is bootstrapped via `dotnet run --project build/_build.csproj` instead of the
// `fallout` global tool (Fallout.GlobalTools isn't on nuget.org). The attribute stays
// as the source-of-truth description of the build lane.
[GitHubActions(
    "build",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    FetchDepth = 0,
    OnPushBranches = new[] { "main" },
    OnPullRequestBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Test), nameof(Pack) })]
partial class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Pack);

    static readonly string[] PackableProjects = { "TvdbClient", "TvdbClient.Models", "TvdbClient.Abstractions" };

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";
    AbsolutePath SpecsProject => RootDirectory / "tests" / "TvdbClient.Specs" / "TvdbClient.Specs.csproj";

    Target Test => _ => _
        .Description("Run the *.Specs test suite")
        .Executes(() => DotNetTest(_ => _
            .SetProjectFile(SpecsProject)
            .SetConfiguration("Release")));

    Target Pack => _ => _
        .Description("Pack the three NuGet packages into artifacts/packages")
        .DependsOn(Test)
        .Executes(() =>
        {
            PackagesDirectory.CreateOrCleanDirectory();
            foreach (var project in PackableProjects)
                DotNetPack(_ => _
                    .SetProject(RootDirectory / "src" / project / $"{project}.csproj")
                    .SetConfiguration("Release")
                    .SetOutputDirectory(PackagesDirectory));
        });

    const string SpecUrl = "https://thetvdb.github.io/v4-api/swagger.yml";

    const string ModelsNamespace = "Tvdb.Models";
    const string ClientsNamespace = "Tvdb.Clients";
    const string AbstractionsNamespace = "Tvdb.Abstractions";

    // The generated DTOs live in the API-versioned TvdbClient.Models project;
    // the generated clients live in the generic TvdbClient core project and
    // import the models namespace. (Architecture enforced by NamespaceSeparationSpecs.)
    AbsolutePath ContractsOutput => RootDirectory / "src" / "TvdbClient.Models" / "TvdbModels.cs";
    AbsolutePath ClientsOutput => RootDirectory / "src" / "TvdbClient" / "Clients" / "TvdbClient.cs";

    Target Generate => _ => _
        .Description("Regenerate the TheTVDB v4 client from the live OpenAPI spec")
        .Executes(async () =>
        {
            Log.Information("Downloading spec: {Url}", SpecUrl);
            var document = await OpenApiYamlDocument.FromUrlAsync(SpecUrl);

            var coerced = CoerceIntegerIdPathParameters(document);
            Log.Information("Overlay: coerced {Count} id path param(s) number → int64", coerced);

            var hoisted = HoistInlineParameterEnums(document);
            Log.Information("Overlay: hoisted {Count} inline parameter enum(s) to named schemas", hoisted);

            // DTOs only → Tvdb.Models (TvdbClient.Models project). No client interfaces/exception
            // classes here — those belong with the generic client core.
            var contracts = new CSharpClientGenerator(document,
                    CreateSettings(ModelsNamespace, dtoTypes: true, clientInterfaces: false, exceptionClasses: false))
                .GenerateFile(ClientGeneratorOutputType.Contracts);

            // Clients only → Tvdb.Clients (TvdbClient project), importing the DTOs from Tvdb.Models.
            var clients = new CSharpClientGenerator(document,
                    CreateSettings(ClientsNamespace, dtoTypes: false, clientInterfaces: true, exceptionClasses: true,
                        additionalNamespaceUsages: new[] { ModelsNamespace, AbstractionsNamespace }))
                .GenerateFile(ClientGeneratorOutputType.Full);

            ContractsOutput.WriteAllText(contracts);
            ClientsOutput.WriteAllText(clients);

            Log.Information("Wrote {Contracts} and {Clients}", ContractsOutput, ClientsOutput);
        });

    static CSharpClientGeneratorSettings CreateSettings(
        string @namespace, bool dtoTypes, bool clientInterfaces, bool exceptionClasses,
        string[]? additionalNamespaceUsages = null)
    {
        var settings = new CSharpClientGeneratorSettings
        {
            ClassName = "{controller}Client",
            ClientBaseInterface = "ITvdbClient",
            InjectHttpClient = true,
            DisposeHttpClient = true,
            GenerateClientInterfaces = clientInterfaces,
            GenerateDtoTypes = dtoTypes,
            GenerateExceptionClasses = exceptionClasses,
            ExceptionClass = "ApiException",
            WrapDtoExceptions = true,
            UseBaseUrl = false,
            GenerateBaseUrlProperty = true,
            GenerateSyncMethods = false,
            GenerateOptionalParameters = true,
            OperationNameGenerator = new MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator(),
            AdditionalNamespaceUsages = additionalNamespaceUsages ?? System.Array.Empty<string>(),
        };
        settings.CSharpGeneratorSettings.Namespace = @namespace;
        settings.CSharpGeneratorSettings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
        settings.CSharpGeneratorSettings.ClassStyle = CSharpClassStyle.Poco;
        settings.CSharpGeneratorSettings.GenerateDataAnnotations = true;
        settings.CSharpGeneratorSettings.GenerateOptionalPropertiesAsNullable = true;
        settings.CSharpGeneratorSettings.RequiredPropertiesMustBeDefined = true;
        settings.CSharpGeneratorSettings.DateType = "System.DateTimeOffset";
        settings.CSharpGeneratorSettings.DateTimeType = "System.DateTimeOffset";
        settings.CSharpGeneratorSettings.TimeType = "System.TimeSpan";
        settings.CSharpGeneratorSettings.TimeSpanType = "System.TimeSpan";
        return settings;
    }

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

    /// <summary>
    /// TheTVDB defines several query-parameter enums inline (e.g. the <c>/updates</c>
    /// <c>type</c>/<c>action</c> params), which NSwag names after the raw parameter —
    /// yielding model types like <c>Type</c> and <c>Action</c> that collide with
    /// <c>System.Type</c>/<c>System.Action</c>. Hoist each inline enum into a named
    /// component schema (<c>{OperationId}{ParamName}</c>) so it generates as a clean,
    /// non-colliding type.
    /// </summary>
    static int HoistInlineParameterEnums(OpenApiDocument document)
    {
        var count = 0;
        foreach (var (path, pathItem) in document.Paths)
        foreach (var (method, operation) in pathItem)
        foreach (var parameter in operation.Parameters)
        {
            var schema = parameter.Schema;
            if (schema is null || schema.HasReference || !schema.IsEnumeration)
                continue;

            var opName = string.IsNullOrEmpty(operation.OperationId)
                ? Pascalize(method) + Pascalize(path)
                : Pascalize(operation.OperationId);
            var name = opName + Pascalize(parameter.Name);

            if (!document.Components.Schemas.ContainsKey(name))
                document.Components.Schemas[name] = schema;

            parameter.Schema = new NJsonSchema.JsonSchema { Reference = document.Components.Schemas[name] };
            count++;
        }

        return count;
    }

    /// <summary>PascalCase an identifier, splitting on non-alphanumeric separators.</summary>
    static string Pascalize(string value)
    {
        var parts = value.Split(new[] { '-', '_', '/', '.', '{', '}', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
    }
}
