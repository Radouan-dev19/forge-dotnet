using System.Security.Claims;

namespace ForgeJwtLab.Security;

/// <summary>
/// Lecture de la revendication de portées, au format OAuth : une chaîne unique dont les
/// portées sont séparées par des espaces.
/// </summary>
public static class ScopeRequirement
{
    public const string ReadOrders = "orders.read";
    public const string WriteOrders = "orders.write";

    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        // Un porteur peut présenter plusieurs revendications scope ; chacune peut porter
        // plusieurs portées. La comparaison est stricte : la casse distingue deux portées.
        foreach (Claim claim in user.FindAll("scope"))
        {
            foreach (string granted in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(granted, scope, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
