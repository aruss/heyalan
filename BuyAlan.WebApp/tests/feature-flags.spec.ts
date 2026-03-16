import { expect, test } from "@playwright/test";
import { parseFeatureFlagsEnvironment } from "../src/lib/feature-flags/server";
import { getFeatureFlagSnapshot, isFeatureEnabled } from "../src/lib/feature-flags/server";

const createEnvironment = (featureFlags: string): NodeJS.ProcessEnv => {
  return {
    FEATURE_FLAGS: featureFlags,
    NODE_ENV: "test",
  };
};

test("FEATURE_FLAGS=teamMembers=1 resolves teamMembers to true", () => {
  const environment = createEnvironment("teamMembers=1");

  expect(parseFeatureFlagsEnvironment(environment)).toEqual({
    teamMembers: true,
  });
  expect(getFeatureFlagSnapshot(environment).teamMembers).toBe(true);
  expect(isFeatureEnabled("teamMembers", environment)).toBe(true);
});

test("FEATURE_FLAGS=teamMembers=0 resolves teamMembers to false", () => {
  const environment = createEnvironment("teamMembers=0");

  expect(parseFeatureFlagsEnvironment(environment)).toEqual({
    teamMembers: false,
  });
  expect(getFeatureFlagSnapshot(environment).teamMembers).toBe(false);
  expect(isFeatureEnabled("teamMembers", environment)).toBe(false);
});

test("missing teamMembers falls back to the default value", () => {
  const environment = createEnvironment("landingPricing=1");

  expect(parseFeatureFlagsEnvironment(environment).teamMembers).toBeUndefined();
  expect(getFeatureFlagSnapshot(environment).teamMembers).toBe(true);
  expect(isFeatureEnabled("teamMembers", environment)).toBe(true);
});
