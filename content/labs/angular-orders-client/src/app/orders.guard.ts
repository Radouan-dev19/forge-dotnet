import { inject } from "@angular/core";
import { CanActivateFn, Router, UrlTree } from "@angular/router";
import { TokenStore } from "./token.store";

/**
 * Garde canActivate de la route des commandes.
 *
 * Sans jeton, la garde renvoie un UrlTree vers /login : l'utilisateur est redirige vers la
 * connexion, ce qui reproduit cote navigation le 401 du serveur. Avec jeton, elle autorise.
 * La verification fine de la portee reste au serveur (403) et a l'aide de vue.
 */
export const ordersGuard: CanActivateFn = (): boolean | UrlTree => {
  const tokens = inject(TokenStore);
  const router = inject(Router);

  if (tokens.get()) {
    return true;
  }
  return router.parseUrl("/login");
};
