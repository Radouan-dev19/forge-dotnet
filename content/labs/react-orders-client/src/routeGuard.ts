// Garde de route cote client : decide, a partir d'un jeton decode, si la route des
// commandes est autorisee, exige une connexion, ou est interdite faute de droit.
//
// La decision reproduit la distinction du serveur :
//   - pas de jeton ou jeton invalide/expire  -> "redirect" (vers la connexion, comme un 401) ;
//   - jeton valide mais portee absente        -> "forbidden" (comme un 403) ;
//   - jeton valide avec la portee requise      -> "allow".
//
// Le decodage ne verifie PAS la signature : c'est un geste cote client pour router
// l'interface. La verification cryptographique reste l'affaire du serveur (api-jwt-bearer).

export type GuardDecision = "allow" | "redirect" | "forbidden";

export interface DecodedToken {
  scope?: string;
  exp?: number;
}

function base64UrlDecode(segment: string): string {
  const normalized = segment.replace(/-/g, "+").replace(/_/g, "/");
  if (typeof atob === "function") {
    return atob(normalized);
  }
  // Repli hors navigateur (par exemple sous Node lors des tests).
  return Buffer.from(normalized, "base64").toString("binary");
}

/** Decode la charge utile d'un JWT sans en verifier la signature. */
export function decodeToken(token: string | null): DecodedToken | null {
  if (!token) {
    return null;
  }
  const parts = token.split(".");
  if (parts.length !== 3) {
    return null;
  }
  try {
    return JSON.parse(base64UrlDecode(parts[1])) as DecodedToken;
  } catch {
    return null;
  }
}

/** Lit les portees d'un jeton au format OAuth : une chaine, portees separees par des espaces. */
export function tokenScopes(decoded: DecodedToken | null): string[] {
  return (decoded?.scope ?? "").split(" ").filter((scope) => scope.length > 0);
}

export function guardOrdersRoute(
  token: string | null,
  requiredScope: string = "orders.read",
  now: number = Date.now(),
): GuardDecision {
  const decoded = decodeToken(token);
  if (!decoded) {
    return "redirect";
  }
  if (decoded.exp !== undefined && decoded.exp * 1000 <= now) {
    return "redirect";
  }
  if (!tokenScopes(decoded).includes(requiredScope)) {
    return "forbidden";
  }
  return "allow";
}
