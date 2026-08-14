import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { OrdersComponent } from "./orders.component";
import { TokenStore } from "./token.store";

const ORDERS_URL = "http://localhost:5000/orders";
const TOKEN = "header.payload.signature";

describe("OrdersComponent", () => {
  let httpMock: HttpTestingController;
  let tokens: TokenStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OrdersComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(TokenStore);
  });

  afterEach(() => httpMock.verify());

  it("affiche l'etat connexion requise et n'appelle pas l'API sans jeton", () => {
    tokens.set(null);

    const fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain("Connexion requise");
    httpMock.expectNone(ORDERS_URL);
  });

  it("liste les commandes une fois authentifie", () => {
    tokens.set(TOKEN);

    const fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();

    const request = httpMock.expectOne(ORDERS_URL);
    expect(request.request.method).toBe("GET");
    request.flush(["order-1001", "order-1002"]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain("order-1001");
    expect(text).toContain("order-1002");
  });

  it("se desabonne au demontage : une reponse tardive ne touche plus le composant", () => {
    tokens.set(TOKEN);

    const fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();

    const request = httpMock.expectOne(ORDERS_URL);
    fixture.destroy();

    // Emission apres destruction : takeUntil a deja coupe l'abonnement.
    request.flush(["order-late"]);
    expect(fixture.componentInstance.orders).toEqual([]);
  });
});
