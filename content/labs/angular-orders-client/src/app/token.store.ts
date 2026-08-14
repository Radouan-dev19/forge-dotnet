import { Injectable } from "@angular/core";

/**
 * Detenteur du jeton courant, cote client.
 *
 * Le magasin ne fabrique aucun jeton : il en recoit un (par exemple apres une connexion)
 * et l'expose par un getter. L'intercepteur et la garde de route lisent ici, ce qui evite
 * de disperser la source du jeton dans toute l'application.
 */
@Injectable({ providedIn: "root" })
export class TokenStore {
  private token: string | null = null;

  set(value: string | null): void {
    this.token = value;
  }

  get(): string | null {
    return this.token;
  }
}
