import { Component, OnDestroy, OnInit } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { OrdersService } from "./orders.service";
import { TokenStore } from "./token.store";

/**
 * Vue des commandes derriere une session par jeton.
 *
 * Sans jeton, la vue affiche "Connexion requise" et n'appelle pas le service. Avec jeton,
 * elle charge la liste. L'abonnement est ferme au demontage via takeUntil sur un Subject
 * "destroyed" : aucune reponse tardive ne vient toucher un composant deja detruit.
 */
@Component({
  selector: "app-orders",
  standalone: true,
  imports: [CommonModule],
  template: `
    <p class="orders-locked" *ngIf="!authenticated">Connexion requise</p>
    <ul class="orders-list" *ngIf="authenticated">
      <li *ngFor="let order of orders">{{ order }}</li>
    </ul>
  `,
})
export class OrdersComponent implements OnInit, OnDestroy {
  private readonly destroyed = new Subject<void>();

  orders: string[] = [];
  authenticated = false;

  constructor(
    private readonly ordersService: OrdersService,
    private readonly tokens: TokenStore,
  ) {}

  ngOnInit(): void {
    this.authenticated = Boolean(this.tokens.get());
    if (!this.authenticated) {
      // Garde de vue : identite non prouvee, aucun appel au service protege.
      return;
    }

    this.ordersService
      .listOrders()
      .pipe(takeUntil(this.destroyed))
      .subscribe((orders) => (this.orders = orders));
  }

  ngOnDestroy(): void {
    // Cloture de l'abonnement : takeUntil complete a la premiere emission de destroyed.
    this.destroyed.next();
    this.destroyed.complete();
  }
}
