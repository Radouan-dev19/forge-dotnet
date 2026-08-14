namespace ForgeBlazorJwtClient;

/// <summary>
/// Contrat de lecture des commandes derriere une session authentifiee par jeton Bearer.
/// L'implementation reelle porterait le jeton dans l'en-tete Authorization ; le laboratoire
/// se concentre sur la logique du composant, aussi l'appel reseau est abstrait ici.
/// </summary>
public interface IOrdersClient
{
    Task<IReadOnlyList<string>> GetOrdersAsync(CancellationToken ct);
}
