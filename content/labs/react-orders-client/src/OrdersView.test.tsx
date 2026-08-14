import { render, screen, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OrdersService } from "./OrdersService";
import { OrdersView } from "./OrdersView";

// La suite remplace fetch par un espion : aucune requete reseau reelle n'est emise.
const fetchMock = vi.fn();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
});

const TOKEN = "header.payload.signature";

describe("OrdersView", () => {
  it("affiche la liste des commandes une fois authentifie", async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ["order-1001", "order-1002"],
    });
    const service = new OrdersService(() => TOKEN);

    render(<OrdersView service={service} getToken={() => TOKEN} />);

    expect(await screen.findByText("order-1001")).toBeInTheDocument();
    expect(screen.getByText("order-1002")).toBeInTheDocument();

    // Le jeton est bien attache en en-tete Authorization: Bearer.
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect((init.headers as Record<string, string>).Authorization).toBe(`Bearer ${TOKEN}`);
  });

  it("refuse et affiche l'etat connexion requise sans jeton", () => {
    const service = new OrdersService(() => null);

    render(<OrdersView service={service} getToken={() => null} />);

    expect(screen.getByText("Connexion requise")).toBeInTheDocument();
    // Aucun appel reseau tant que l'utilisateur n'est pas authentifie.
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("annule la requete en vol au demontage via AbortController", async () => {
    let capturedSignal: AbortSignal | undefined;
    fetchMock.mockImplementation((_url: string, init: RequestInit) => {
      capturedSignal = init.signal ?? undefined;
      // Promesse jamais resolue : seule l'annulation met fin a la requete.
      return new Promise<never>(() => {});
    });
    const service = new OrdersService(() => TOKEN);

    const { unmount } = render(<OrdersView service={service} getToken={() => TOKEN} />);

    await waitFor(() => expect(capturedSignal).toBeDefined());
    expect(capturedSignal?.aborted).toBe(false);

    unmount();

    expect(capturedSignal?.aborted).toBe(true);
  });
});
