using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ForgeDotNet.Domain.Diagnostic;

public static class DiagnosticSampler
{
    private static readonly DiagnosticDomain[][] SectionDomains =
    [
        [DiagnosticDomain.Logic, DiagnosticDomain.CSharp, DiagnosticDomain.Reading],
        [DiagnosticDomain.Debugging, DiagnosticDomain.Sql, DiagnosticDomain.Http],
        [DiagnosticDomain.Git, DiagnosticDomain.Testing, DiagnosticDomain.English],
    ];

    private static readonly string[] SectionTitles =
    [
        "Raisonnement et code",
        "Diagnostic technique",
        "Livraison et communication",
    ];

    public static DiagnosticPlan CreatePlan(DiagnosticBank bank, DiagnosticMode mode, int seed)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ValidateCoverage(bank);

        var sections = new List<DiagnosticPlanSection>(SectionDomains.Length);
        for (int sectionIndex = 0; sectionIndex < SectionDomains.Length; sectionIndex++)
        {
            var selected = new List<DiagnosticQuestion>();
            foreach (DiagnosticDomain domain in SectionDomains[sectionIndex])
            {
                if (mode == DiagnosticMode.Initial)
                {
                    for (int difficulty = 1; difficulty <= 3; difficulty++)
                    {
                        selected.Add(SelectOne(bank, domain, difficulty, seed));
                    }
                }
                else
                {
                    selected.Add(SelectOne(bank, domain, difficulty: 2, seed));
                }
            }

            DiagnosticQuestion[] ordered = selected
                .OrderBy(question => StableKey(seed, $"order:{sectionIndex}:{question.Id}"), StringComparer.Ordinal)
                .ToArray();
            sections.Add(new DiagnosticPlanSection(
                sectionIndex,
                SectionTitles[sectionIndex],
                Array.AsReadOnly(ordered)));
        }

        return new DiagnosticPlan(mode, seed, sections.AsReadOnly());
    }

    private static DiagnosticQuestion SelectOne(
        DiagnosticBank bank,
        DiagnosticDomain domain,
        int difficulty,
        int seed) => bank.Questions
            .Where(question => question.Domain == domain && question.Difficulty == difficulty)
            .OrderBy(
                question => StableKey(
                    seed,
                    $"select:{DiagnosticDomains.GetId(domain)}:{difficulty}:{question.Id}"),
                StringComparer.Ordinal)
            .First();

    private static void ValidateCoverage(DiagnosticBank bank)
    {
        foreach (DiagnosticDomain domain in DiagnosticDomains.All)
        {
            for (int difficulty = 1; difficulty <= 3; difficulty++)
            {
                if (!bank.Questions.Any(question =>
                    question.Domain == domain && question.Difficulty == difficulty))
                {
                    throw new InvalidDataException(
                        $"La banque ne couvre pas {DiagnosticDomains.GetId(domain)} au niveau {difficulty}.");
                }
            }

            int mediumCount = bank.Questions.Count(question =>
                question.Domain == domain && question.Difficulty == 2);
            if (mediumCount < 2)
            {
                throw new InvalidDataException(
                    $"La banque doit proposer deux variantes intermédiaires pour {DiagnosticDomains.GetId(domain)}.");
            }
        }
    }

    private static string StableKey(int seed, string value)
    {
        byte[] input = Encoding.UTF8.GetBytes(seed.ToString(CultureInfo.InvariantCulture) + ":" + value);
        return Convert.ToHexString(SHA256.HashData(input));
    }
}
