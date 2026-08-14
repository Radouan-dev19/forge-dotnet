import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

/**
 * Service d'acces a la ressource /orders de l'API api-jwt-bearer.
 *
 * Le service ne se preoccupe pas du jeton : l'intercepteur AuthInterceptor attache
 * l'en-tete Authorization: Bearer sur la requete sortante. Le service se contente
 * de decrire l'appel et son type de retour.
 */
@Injectable({ providedIn: "root" })
export class OrdersService {
  // L'API renvoie un tableau d'identifiants de commande, par exemple ["order-1001", ...].
  private readonly baseUrl = "http://localhost:5000";

  constructor(private readonly http: HttpClient) {}

  listOrders(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/orders`);
  }
}
