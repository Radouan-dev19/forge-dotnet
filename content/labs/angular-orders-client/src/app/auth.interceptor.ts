import { Injectable } from "@angular/core";
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from "@angular/common/http";
import { Observable } from "rxjs";
import { TokenStore } from "./token.store";

/**
 * Intercepteur qui attache le jeton en en-tete Authorization: Bearer.
 *
 * Centraliser ce geste dans un intercepteur evite de repeter l'en-tete dans chaque
 * service : toute requete sortante passant par HttpClient est enrichie ici. Sans jeton,
 * la requete part inchangee et le serveur repondra 401.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly tokens: TokenStore) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    const token = this.tokens.get();
    if (!token) {
      return next.handle(request);
    }

    const authorized = request.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
    return next.handle(authorized);
  }
}
