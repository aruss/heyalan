import { expect, test } from "@playwright/test";
import { resolveProviderIconKey } from "../src/app/login/provider-icons";

test("resolveProviderIconKey returns google for google provider", () => {
    const providerIconKey = resolveProviderIconKey("google");

    expect(providerIconKey).toBe("google");
});

test("resolveProviderIconKey returns microsoft for microsoft provider", () => {
    const providerIconKey = resolveProviderIconKey("microsoft");

    expect(providerIconKey).toBe("microsoft");
});

test("resolveProviderIconKey returns square for square provider", () => {
    const providerIconKey = resolveProviderIconKey("square");

    expect(providerIconKey).toBe("square");
});

test("resolveProviderIconKey uses case-insensitive lookup", () => {
    const providerIconKey = resolveProviderIconKey("GoOgLe");

    expect(providerIconKey).toBe("google");
});

test("resolveProviderIconKey returns unknown fallback for unsupported providers", () => {
    const providerIconKey = resolveProviderIconKey("okta");

    expect(providerIconKey).toBe("unknown");
});
