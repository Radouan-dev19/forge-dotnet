using Microsoft.Playwright;

namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>
/// Session navigateur d'un persona : un Chromium réel piloté par Playwright, aucun client HTTP en
/// processus. C'est précisément ce que les 61 tests E2E ne faisaient pas.
/// </summary>
public sealed class PersonaSession : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IPage Page { get; private set; } = null!;

    public static async Task<PersonaSession> LaunchAsync()
    {
        var session = new PersonaSession();
        session._playwright = await Playwright.CreateAsync();
        session._browser = await session._playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        IBrowserContext context = await session._browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            Locale = "fr-FR",
        });
        session.Page = await context.NewPageAsync();
        session.Page.SetDefaultTimeout(20_000);
        return session;
    }

    /// <summary>
    /// Navigue puis attend le circuit interactif : chaque chargement complet ouvre la connexion
    /// WebSocket de Blazor Server, et un clic avant cette connexion tombe sur du DOM prérendu inerte.
    /// </summary>
    public async Task GoAsync(string baseUrl, string path)
    {
        try
        {
            await Page.RunAndWaitForWebSocketAsync(
                async () => await Page.GotoAsync(baseUrl + path, new PageGotoOptions { WaitUntil = WaitUntilState.Load }),
                new PageRunAndWaitForWebSocketOptions { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            // Page servie sans circuit : on continue, les assertions diront ce qu'il en est.
        }

        // Court délai de poignée de main : la connexion ouverte précède l'attachement des gestionnaires.
        await Page.WaitForTimeoutAsync(500);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}
