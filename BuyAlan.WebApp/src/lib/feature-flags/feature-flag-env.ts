import "server-only";

import {
  featureFlagRegistry,
  type FeatureFlagDefinition,
  type FeatureFlagKey,
} from "@/lib/feature-flags/feature-flag-registry";
import { createLogger } from "@/lib/logger";

export type FeatureFlagEnvironment = NodeJS.ProcessEnv;

const TRUE_NUMERIC_FLAG_VALUE = "1";
const FALSE_NUMERIC_FLAG_VALUE = "0";
const FEATURE_FLAGS_ENV_KEY = "FEATURE_FLAGS";
const FEATURE_FLAG_ENTRY_SEPARATOR = ";";
const FEATURE_FLAG_KEY_VALUE_SEPARATOR = "=";
const featureFlagLogger = createLogger({
  module: "feature-flags",
});

type ParsedFeatureFlagMap = Partial<Record<FeatureFlagKey, boolean>>;

const normalizeFeatureFlagToken = (rawValue: string): string => {
  return rawValue.trim().toLowerCase();
};

const warnInvalidFeatureFlagsSegment = (
  rawSegment: string,
  reason: string,
): void => {
  featureFlagLogger.warning(
    {
      eventName: "feature_flags_invalid_segment",
      featureFlagsEnvKey: FEATURE_FLAGS_ENV_KEY,
      rawSegment,
      reason,
    },
    "Ignoring invalid feature flag segment",
  );
};

const warnUnknownFeatureFlagKey = (rawKey: string): void => {
  featureFlagLogger.warning(
    {
      eventName: "feature_flags_unknown_key",
      featureFlagsEnvKey: FEATURE_FLAGS_ENV_KEY,
      rawKey,
    },
    "Ignoring unknown feature flag key",
  );
};

const warnDuplicateFeatureFlagKey = (key: FeatureFlagKey): void => {
  featureFlagLogger.warning(
    {
      eventName: "feature_flags_duplicate_key",
      featureFlagsEnvKey: FEATURE_FLAGS_ENV_KEY,
      key,
    },
    "Duplicate feature flag key detected; using the last value",
  );
};

const isFeatureFlagKey = (value: string): value is FeatureFlagKey => {
  return value in featureFlagRegistry;
};

const parseFeatureFlagBooleanValue = (rawValue: string): boolean | null => {
  const normalizedValue = normalizeFeatureFlagToken(rawValue);

  if (normalizedValue === TRUE_NUMERIC_FLAG_VALUE) {
    return true;
  }

  if (normalizedValue === FALSE_NUMERIC_FLAG_VALUE) {
    return false;
  }

  return null;
};

export const getFeatureFlagsEnvKey = (): string => {
  return FEATURE_FLAGS_ENV_KEY;
};

export const parseFeatureFlagsEnvironment = (
  environment: FeatureFlagEnvironment = process.env,
): ParsedFeatureFlagMap => {
  const rawFeatureFlags = environment[FEATURE_FLAGS_ENV_KEY];
  const parsedFeatureFlags: ParsedFeatureFlagMap = {};

  if (rawFeatureFlags == null || rawFeatureFlags.trim() === "") {
    return parsedFeatureFlags;
  }

  const rawSegments = rawFeatureFlags.split(FEATURE_FLAG_ENTRY_SEPARATOR);

  for (const rawSegment of rawSegments) {
    const trimmedSegment = rawSegment.trim();

    if (trimmedSegment === "") {
      continue;
    }

    const separatorIndex = trimmedSegment.indexOf(FEATURE_FLAG_KEY_VALUE_SEPARATOR);
    const lastSeparatorIndex = trimmedSegment.lastIndexOf(FEATURE_FLAG_KEY_VALUE_SEPARATOR);

    if (separatorIndex <= 0 || separatorIndex !== lastSeparatorIndex) {
      warnInvalidFeatureFlagsSegment(
        rawSegment,
        `Expected ${JSON.stringify("featureName=1")} or ${JSON.stringify("featureName=0")}.`,
      );
      continue;
    }

    const rawKey = trimmedSegment.slice(0, separatorIndex).trim();
    const rawValue = trimmedSegment.slice(separatorIndex + 1).trim();

    if (!isFeatureFlagKey(rawKey)) {
      warnUnknownFeatureFlagKey(rawKey);
      continue;
    }

    const parsedValue = parseFeatureFlagBooleanValue(rawValue);

    if (parsedValue === null) {
      warnInvalidFeatureFlagsSegment(
        rawSegment,
        `Expected value 1 or 0 for key ${JSON.stringify(rawKey)}.`,
      );
      continue;
    }

    if (parsedFeatureFlags[rawKey] != null) {
      warnDuplicateFeatureFlagKey(rawKey);
    }

    parsedFeatureFlags[rawKey] = parsedValue;
  }

  return parsedFeatureFlags;
};

export const parseFeatureFlagValue = (
  definition: FeatureFlagDefinition<FeatureFlagKey>,
  environment: FeatureFlagEnvironment = process.env,
): boolean => {
  const parsedFeatureFlags = parseFeatureFlagsEnvironment(environment);
  const parsedValue = parsedFeatureFlags[definition.key];

  if (parsedValue == null) {
    return definition.defaultValue;
  }

  return parsedValue;
};

export const resolveFeatureFlagValue = (
  key: FeatureFlagKey,
  environment: FeatureFlagEnvironment = process.env,
): boolean => {
  const definition = featureFlagRegistry[key];

  return parseFeatureFlagValue(definition, environment);
};
