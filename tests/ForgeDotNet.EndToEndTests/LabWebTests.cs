using System.Net;
using ForgeDotNet.Application.Labs;
using ForgeDotNet.Domain.Labs;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeDotNet.EndToEndTests;

/// <summary>
/// Prouve que les six laboratoires sont atteignables depuis l'application, et que leur page dit ce
/// qu'ils ne prouvent pas.
/// </summary>
/// <remarks>
/// Avant ce rattachement, <c>grep -rn "content/labs" src/</c> ne renvoyait rien : les six
/// laboratoires — les seuls artefacts réellement exécutables du dépôt — étaient invisibles du produit.
/// Un apprenant qui suivait le parcours ne les rencontrait jamais. Ces tests figent l'inverse : la
/// liste les montre tous, chaque page se rend, et la mention de preuve déclarée est présente, car c'est
/// elle qui empêche de confondre un laboratoire avec une preuve de maîtrise.
/// </remarks>
public sealed class LabWebTests : IClassFixture<ForgeWebApplicationFactory>
{
    private readonly ForgeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LabWebTests(ForgeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task TheLabCatalogueExposesTheElevenPublishedLaboratories()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<ILabSource>();

        IReadOnlyList<Lab> labs = await source.ListAsync();

        Assert.Equal(11, labs.Count);
        Assert.Equal(
            ["api-mini-erp", "api-jwt-bearer", "testing-strategy", "git-review", "container-delivery", "ci-delivery", "azure-operations", "oauth-local-idp", "angular-orders-client", "react-orders-client", "blazor-jwt-client"],
            labs.Select(lab => lab.Id));
    }

    /// <summary>
    /// Chaque laboratoire déclare la politique de preuve que le schéma impose, sans exception.
    /// </summary>
    [Fact]
    public async Task EveryLaboratoryDeclaresItsSuccessAsLearnerDeclared()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<ILabSource>();

        IReadOnlyList<Lab> labs = await source.ListAsync();

        Assert.All(labs, lab => Assert.True(lab.IsLearnerDeclared, lab.Id));
        Assert.All(labs, lab => Assert.Equal(Lab.LearnerDeclaredPolicy, lab.EvidencePolicy));
    }

    /// <summary>
    /// Un laboratoire sans objectifs, sans commandes ou sans limites ne serait pas exploitable.
    /// </summary>
    [Fact]
    public async Task EveryLaboratoryCarriesObjectivesCommandsAndAnnouncedLimits()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<ILabSource>();

        foreach (Lab lab in await source.ListAsync())
        {
            Assert.True(lab.Objectives.Count >= 2, lab.Id);
            Assert.NotEmpty(lab.Commands);
            Assert.NotEmpty(lab.Limits);
            Assert.NotEmpty(lab.Brief);
            Assert.All(lab.Objectives, objective => Assert.NotEmpty(objective.ObservableProof));
        }
    }

    [Fact]
    public async Task TheLabListPageStatesThatSuccessIsDeclaredAndProvesNothing()
    {
        var content = await _client.GetStringAsync("/labs");

        Assert.Contains("Laboratoires", content, StringComparison.Ordinal);
        Assert.Contains("hors du bac à sable", content, StringComparison.Ordinal);
        Assert.Contains("aucun laboratoire ne produit de preuve de maîtrise", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("api-mini-erp")]
    [InlineData("api-jwt-bearer")]
    [InlineData("oauth-local-idp")]
    [InlineData("azure-operations")]
    [InlineData("ci-delivery")]
    [InlineData("container-delivery")]
    [InlineData("git-review")]
    [InlineData("testing-strategy")]
    [InlineData("angular-orders-client")]
    [InlineData("react-orders-client")]
    [InlineData("blazor-jwt-client")]
    public async Task EveryLabPageRendersItsBriefObjectivesAndLimits(string labId)
    {
        using var response = await _client.GetAsync($"/labs/{labId}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Attendu 200 pour '/labs/{labId}', reçu {(int)response.StatusCode}.");
        Assert.Contains("Brief", content, StringComparison.Ordinal);
        Assert.Contains("Objectifs et preuve observable", content, StringComparison.Ordinal);
        Assert.Contains("Limites annoncées", content, StringComparison.Ordinal);
        Assert.Contains("Commandes à exécuter vous-même", content, StringComparison.Ordinal);

        // La phrase qui empêche la confusion doit être sur la page du laboratoire lui-même, pas
        // seulement sur la liste : c'est celle-ci qu'un apprenant ouvre pour travailler.
        Assert.Contains("Votre réussite est déclarée", content, StringComparison.Ordinal);
    }

    /// <summary>
    /// Un identifiant inconnu rend une page lisible, pas une erreur du serveur.
    /// </summary>
    [Fact]
    public async Task AnUnknownLabIdentifierIsRefusedWithoutFailingTheRequest()
    {
        using var response = await _client.GetAsync("/labs/laboratoire-inexistant");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Aucun laboratoire publié ne porte cet identifiant", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheMainNavigationLinksToTheLaboratories()
    {
        var content = await _client.GetStringAsync("/");

        Assert.Contains("href=\"labs\"", content, StringComparison.Ordinal);
    }
}
