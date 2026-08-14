import { useEffect, useState } from "react";
import { OrdersApiError, OrdersService, TokenGetter } from "./OrdersService";

// Etat fini de la vue : connexion requise, chargement, liste prete, annule, ou erreur.
export type OrdersState =
  | { status: "login-required" }
  | { status: "loading" }
  | { status: "ready"; orders: string[] }
  | { status: "cancelled" }
  | { status: "error"; message: string };

/**
 * Hook de chargement des commandes. Sans jeton, il reste sur "login-required" et
 * ne declenche aucun appel. Avec jeton, il charge via le service et annule la
 * requete en vol si le composant est demonte, grace a un AbortController.
 */
export function useOrders(service: OrdersService, hasToken: boolean): OrdersState {
  const [state, setState] = useState<OrdersState>(
    hasToken ? { status: "loading" } : { status: "login-required" },
  );

  useEffect(() => {
    if (!hasToken) {
      setState({ status: "login-required" });
      return;
    }

    const controller = new AbortController();
    setState({ status: "loading" });

    service
      .listOrders(controller.signal)
      .then((orders) => {
        if (!controller.signal.aborted) {
          setState({ status: "ready", orders });
        }
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          setState({ status: "cancelled" });
          return;
        }
        const message =
          error instanceof OrdersApiError
            ? error.kind
            : error instanceof Error
              ? error.message
              : "unknown";
        setState({ status: "error", message });
      });

    // Nettoyage : le demontage annule la requete encore en vol.
    return () => controller.abort();
  }, [service, hasToken]);

  return state;
}

export interface OrdersViewProps {
  service: OrdersService;
  getToken: TokenGetter;
}

/**
 * Vue des commandes derriere une session par jeton. Elle affiche un etat "connexion
 * requise" tant qu'aucun jeton n'est disponible, et la liste une fois authentifiee.
 */
export function OrdersView({ service, getToken }: OrdersViewProps): JSX.Element {
  const hasToken = Boolean(getToken());
  const state = useOrders(service, hasToken);

  switch (state.status) {
    case "login-required":
      return (
        <p role="status" className="orders-locked">
          Connexion requise
        </p>
      );
    case "loading":
      return (
        <p role="status" className="orders-loading">
          Chargement...
        </p>
      );
    case "cancelled":
      return (
        <p role="status" className="orders-cancelled">
          Chargement annule.
        </p>
      );
    case "error":
      return (
        <p role="alert" className="orders-error">
          Erreur : {state.message}
        </p>
      );
    case "ready":
      return (
        <ul className="orders-list">
          {state.orders.map((order) => (
            <li key={order}>{order}</li>
          ))}
        </ul>
      );
  }
}
