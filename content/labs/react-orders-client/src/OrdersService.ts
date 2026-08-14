// Client d'acces a la ressource /orders de l'API api-jwt-bearer.
//
// Le service ne connait pas la fabrique du jeton : il recoit un getter et attache
// le jeton en en-tete Authorization: Bearer a chaque requete. La distinction des
// statuts de l'API est preservee cote client : 401 (identite non prouvee) et 403
// (droit manquant) deviennent deux erreurs distinctes.

export type TokenGetter = () => string | null;

const DEFAULT_BASE_URL = "http://localhost:5000";

/** Erreur portant le statut HTTP renvoye par l'API protegee. */
export class OrdersApiError extends Error {
  constructor(
    public readonly kind: "login-required" | "unauthorized" | "forbidden" | "unexpected",
    public readonly status: number,
  ) {
    super(`orders-api-error:${kind}:${status}`);
    this.name = "OrdersApiError";
  }
}

export class OrdersService {
  constructor(
    private readonly getToken: TokenGetter,
    private readonly baseUrl: string = DEFAULT_BASE_URL,
  ) {}

  /**
   * Recupere la liste des commandes. Le signal permet d'annuler une requete en vol
   * via un AbortController detenu par l'appelant.
   */
  async listOrders(signal?: AbortSignal): Promise<string[]> {
    const token = this.getToken();
    if (!token) {
      // Garde cote client : sans jeton, aucun appel reseau n'est tente.
      throw new OrdersApiError("login-required", 0);
    }

    const response = await fetch(`${this.baseUrl}/orders`, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
        Accept: "application/json",
      },
      signal,
    });

    if (response.status === 401) {
      throw new OrdersApiError("unauthorized", 401);
    }
    if (response.status === 403) {
      throw new OrdersApiError("forbidden", 403);
    }
    if (!response.ok) {
      throw new OrdersApiError("unexpected", response.status);
    }

    // L'API renvoie un tableau d'identifiants de commande, par exemple ["order-1001", ...].
    return (await response.json()) as string[];
  }
}
