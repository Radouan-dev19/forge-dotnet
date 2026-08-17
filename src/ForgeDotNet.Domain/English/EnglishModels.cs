namespace ForgeDotNet.Domain.English;

/// <summary>Terme métier fourni avec la carte, et ce qu'il recouvre réellement.</summary>
public sealed record EnglishTerm(string Term, string Meaning);

/// <summary>
/// Carte d'anglais professionnel : une situation, ce qu'il faut produire, et de quoi s'auto-relire.
/// </summary>
/// <remarks>
/// <para>
/// Les 51 cartes vont par paires — une écrite, une orale — sur la même situation. C'est ce
/// double format que la grille d'anglais de <c>docs/HUMAN_REVIEW.md</c> demande au relecteur : les
/// <see cref="ExpectedElements"/> doivent apparaître à l'écrit, puis être énoncés à l'oral sans lire
/// un texte préparé. Les présenter appariées sert donc directement la revue.
/// </para>
/// <para>
/// Aucune preuve de maîtrise n'en découle. L'expression demande un lecteur, et la composante Anglais
/// figure parmi les exigences que la politique classe en jugement humain.
/// </para>
/// </remarks>
public sealed record EnglishActivity(
    string Id,
    int Version,
    string Title,
    string Level,
    int DurationMinutes,
    IReadOnlyList<string> Skills,
    string Situation,
    IReadOnlyList<string> Instructions,
    IReadOnlyList<EnglishTerm> Vocabulary,
    IReadOnlyList<string> ExpectedElements,
    string ModelAnswer,
    IReadOnlyList<string> CommonMistakes,
    IReadOnlyList<string> Variants)
{
    /// <summary>Vrai lorsque la carte demande une production orale plutôt qu'écrite.</summary>
    public bool IsSpoken => Id.EndsWith("-spoken", StringComparison.Ordinal);

    /// <summary>
    /// Clé commune aux deux cartes d'une même situation, servant à les apparier.
    /// </summary>
    /// <remarks>
    /// La convention de nommage — <c>english-card-NN-written</c> et <c>english-card-NN-spoken</c> — est
    /// la seule chose qui relie les deux moitiés d'une situation ; aucun champ du manifeste ne le fait.
    /// Une carte dont le suffixe manquerait resterait donc seule, ce qui est le comportement voulu :
    /// mieux vaut une carte non appariée qu'un appariement inventé.
    /// </remarks>
    public string PairKey =>
        IsSpoken ? Id[..^"-spoken".Length]
        : Id.EndsWith("-written", StringComparison.Ordinal) ? Id[..^"-written".Length]
        : Id;
}
