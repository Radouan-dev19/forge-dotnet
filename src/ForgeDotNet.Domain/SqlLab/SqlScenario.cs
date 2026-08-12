namespace ForgeDotNet.Domain.SqlLab;

/// <summary>
/// Ce qu'une session jetable doit provisionner : le schéma et le jeu de données du scénario, plus
/// le schéma visible annoncé à l'apprenant.
/// </summary>
/// <remarks>
/// La réinitialisation ne rejoue pas le script de remise à zéro du contenu : elle détruit la base et
/// le login, puis reprovisionne à partir de ce même jeu de données. L'isolation obtenue est plus
/// forte qu'un simple DROP/CREATE exécuté dans la base existante, et elle ne dépend d'aucun script
/// fourni par le contenu.
/// </remarks>
public sealed record SqlLabProvisioning(
    string ScenarioId,
    string SchemaAndDatasetSql,
    string VisibleSchema)
{
    public static SqlLabProvisioning Create(
        string scenarioId,
        string schemaAndDatasetSql,
        string visibleSchema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaAndDatasetSql);
        ArgumentException.ThrowIfNullOrWhiteSpace(visibleSchema);
        return new SqlLabProvisioning(scenarioId, schemaAndDatasetSql, visibleSchema);
    }
}

/// <summary>
/// Scénario SQL publié, chargé côté serveur uniquement.
/// </summary>
/// <remarks>
/// <see cref="Expectation"/> porte les lignes attendues issues du contrat privé du scénario. Elle ne
/// doit jamais franchir la frontière web : l'apprenant reçoit l'énoncé, le schéma visible et les
/// limites, jamais le résultat de référence.
/// </remarks>
public sealed record SqlScenario(
    string Id,
    int Version,
    string ContentRevision,
    string Title,
    int Difficulty,
    IReadOnlyList<string> Skills,
    int EstimatedMinutes,
    string Statement,
    string VisibleSchema,
    string SchemaAndDatasetSql,
    SqlLabLimits Limits,
    IReadOnlyList<string> EffectAssertions,
    SqlLabExpectedResult Expectation)
{
    public SqlLabProvisioning ToProvisioning() =>
        SqlLabProvisioning.Create(Id, SchemaAndDatasetSql, VisibleSchema);
}
