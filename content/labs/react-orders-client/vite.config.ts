import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// La configuration Vitest tourne dans jsdom : @testing-library/react a besoin d'un DOM
// pour monter les composants. Aucun navigateur reel n'est lance ; c'est un DOM simule.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: false,
    include: ["src/**/*.test.{ts,tsx}"],
  },
});
