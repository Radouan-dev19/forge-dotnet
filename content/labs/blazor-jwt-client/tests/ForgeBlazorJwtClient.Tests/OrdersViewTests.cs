using Bunit;
using Bunit.TestDoubles;
using ForgeBlazorJwtClient;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeBlazorJwtClient.Tests;

/// <summary>
/// Preuve serveur du laboratoire : rend le composant Blazor reel dans la suite de tests
/// de la solution, via bUnit, sans navigateur ni conteneur. Les trois faits couvrent la
/// garde de route, le refus non authentifie et l'annulation d'un chargement en vol.
/// </summary>
public sealed class OrdersViewTests : Bunit.TestContext
{
    [Fact]
    public void RendersOrdersWhenAuthenticated()
    {
        TestAuthorizationContext auth = this.AddTestAuthorization();
        auth.SetAuthorized("agent");
        Services.AddSingleton<IOrdersClient>(new FakeOrdersClient(new[] { "A-1", "A-2" }));

        IRenderedComponent<OrdersView> cut = RenderComponent<OrdersView>();

        Assert.Contains("A-1", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("A-2", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void RefusesWhenNotAuthenticated()
    {
        TestAuthorizationContext auth = this.AddTestAuthorization();
        auth.SetNotAuthorized();
        FakeOrdersClient client = new(new[] { "A-1" });
        Services.AddSingleton<IOrdersClient>(client);

        IRenderedComponent<OrdersView> cut = RenderComponent<OrdersView>();

        Assert.Contains("Connexion requise", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public void CancelsAnInFlightLoad()
    {
        TestAuthorizationContext auth = this.AddTestAuthorization();
        auth.SetAuthorized("agent");

        TaskCompletionSource<IReadOnlyList<string>> gate = new();
        CancellationToken captured = default;
        DelegatingOrdersClient client = new(ct =>
        {
            captured = ct;
            // Le chargement ne se debloque que sur annulation du jeton.
            ct.Register(() => gate.TrySetCanceled(ct));
            return gate.Task;
        });
        Services.AddSingleton<IOrdersClient>(client);

        IRenderedComponent<OrdersView> cut = RenderComponent<OrdersView>();
        cut.WaitForState(() => cut.Markup.Contains("Chargement...", StringComparison.Ordinal));

        cut.Find("button.orders-cancel").Click();

        cut.WaitForState(() => cut.Markup.Contains("Chargement annulé.", StringComparison.Ordinal));
        Assert.Contains("Chargement annulé.", cut.Markup, StringComparison.Ordinal);
        Assert.True(captured.IsCancellationRequested);
    }

    private sealed class FakeOrdersClient : IOrdersClient
    {
        private readonly IReadOnlyList<string> _orders;

        public FakeOrdersClient(IReadOnlyList<string> orders) => _orders = orders;

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<string>> GetOrdersAsync(CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(_orders);
        }
    }

    private sealed class DelegatingOrdersClient : IOrdersClient
    {
        private readonly Func<CancellationToken, Task<IReadOnlyList<string>>> _handler;

        public DelegatingOrdersClient(Func<CancellationToken, Task<IReadOnlyList<string>>> handler) =>
            _handler = handler;

        public Task<IReadOnlyList<string>> GetOrdersAsync(CancellationToken ct) => _handler(ct);
    }
}
