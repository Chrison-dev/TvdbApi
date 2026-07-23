using System.Linq;
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TvdbClient.Specs;

/// <summary>
/// Fitness tests locking the 3-project separation in place: one API-versioned
/// project (the generated models) and two generic ones. Regenerating the models
/// must never be able to reach back into the client core.
/// </summary>
public class ArchitectureSpecs
{
    static readonly Assembly Models = typeof(Tvdb.Models.SeriesBaseRecord).Assembly;
    static readonly Assembly Abstractions = typeof(Tvdb.Abstractions.ITvdbClient).Assembly;
    static readonly Assembly Core = typeof(Tvdb.Clients.LoginClient).Assembly;

    [Fact]
    public void Models_is_the_leaf_and_depends_on_no_other_Tvdb_project()
    {
        var result = Types.InAssembly(Models)
            .Should().NotHaveDependencyOnAny("Tvdb.Abstractions", "Tvdb.Clients")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "the API-versioned models are regenerated in isolation; offenders: "
                     + string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>()));
    }

    [Fact]
    public void Abstractions_does_not_depend_on_the_client_core()
    {
        var result = Types.InAssembly(Abstractions)
            .Should().NotHaveDependencyOn("Tvdb.Clients")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "the generic contracts must not depend on the client implementation; offenders: "
                     + string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>()));
    }

    [Fact]
    public void The_generated_models_namespace_lives_only_in_the_models_assembly()
    {
        var strays = Types.InAssembly(Abstractions).That().ResideInNamespace("Tvdb.Models").GetTypes()
            .Concat(Types.InAssembly(Core).That().ResideInNamespace("Tvdb.Models").GetTypes())
            .Select(t => t.FullName)
            .ToList();

        strays.Should().BeEmpty(
            "the generated-model namespace Tvdb.Models must belong to TvdbClient.Models alone");
    }
}
