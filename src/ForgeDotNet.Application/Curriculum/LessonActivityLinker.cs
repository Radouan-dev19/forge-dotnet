namespace ForgeDotNet.Application.Curriculum;

/// <summary>
/// Transforme, dans les sections d'une leçon, chaque identifiant d'activité cité en code inline en
/// lien direct vers la page qui la sert. Partagé par les deux lecteurs — socle junior et piste senior —
/// pour qu'aucun parcours ne garde des citations inertes.
/// </summary>
public static class LessonActivityLinker
{
    public static async ValueTask<IReadOnlyList<LessonSectionView>> LinkAsync(
        IReadOnlyList<LessonSectionView> sections,
        ILessonActivityLinkResolver? resolver,
        CancellationToken cancellationToken)
    {
        if (resolver is null)
        {
            return sections;
        }

        // Un identifiant est résolu une seule fois par leçon.
        var resolved = new Dictionary<string, LessonActivityLink?>(StringComparer.Ordinal);
        var linkedSections = new List<LessonSectionView>(sections.Count);
        foreach (LessonSectionView section in sections)
        {
            var blocks = new List<LessonBlockView>(section.Blocks.Count);
            foreach (LessonBlockView block in section.Blocks)
            {
                blocks.Add(block switch
                {
                    LessonParagraphView paragraph => paragraph with
                    {
                        Inlines = await LinkInlinesAsync(paragraph.Inlines),
                    },
                    LessonListView list => list with
                    {
                        Items = Array.AsReadOnly(await Task.WhenAll(
                            list.Items.Select(item => LinkInlinesAsync(item).AsTask()))),
                    },
                    _ => block,
                });
            }

            linkedSections.Add(section with { Blocks = Array.AsReadOnly(blocks.ToArray()) });
        }

        return Array.AsReadOnly(linkedSections.ToArray());

        async ValueTask<IReadOnlyList<LessonInlineView>> LinkInlinesAsync(IReadOnlyList<LessonInlineView> inlines)
        {
            var result = new LessonInlineView[inlines.Count];
            for (int index = 0; index < inlines.Count; index++)
            {
                LessonInlineView inline = inlines[index];
                if (inline.Kind == LessonInlineKind.Code)
                {
                    if (!resolved.TryGetValue(inline.Text, out LessonActivityLink? link))
                    {
                        link = await resolver.ResolveAsync(inline.Text, cancellationToken);
                        resolved[inline.Text] = link;
                    }

                    if (link is not null)
                    {
                        inline = new LessonInlineView(LessonInlineKind.ActivityLink, inline.Text, link.Href, link.OpenInNewTab);
                    }
                }

                result[index] = inline;
            }

            return Array.AsReadOnly(result);
        }
    }
}
