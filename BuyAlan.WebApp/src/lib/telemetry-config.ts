import type pino from "pino";
import {
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_VERSION,
  SEMRESATTRS_DEPLOYMENT_ENVIRONMENT,
} from "@opentelemetry/semantic-conventions";

const DEFAULT_LOG_LEVEL: pino.LevelWithSilent = "info";
const DEFAULT_SERVICE_NAME = "buyalan-webapp";
const DEFAULT_SERVICE_VERSION = "0.0.0";
const DEFAULT_OTLP_PROTOCOL = "http/protobuf";
const INVALID_OTLP_ENDPOINT_LOG_VALUE = "[invalid-otlp-endpoint]";

const DOTNET_TO_PINO_LOG_LEVELS = {
  critical: "fatal",
  debug: "debug",
  error: "error",
  information: "info",
  none: "silent",
  trace: "trace",
  warning: "warn",
} as const satisfies Record<string, pino.LevelWithSilent>;

const PINO_LOG_LEVELS = {
  debug: "debug",
  error: "error",
  fatal: "fatal",
  info: "info",
  silent: "silent",
  trace: "trace",
  warn: "warn",
} as const satisfies Record<string, pino.LevelWithSilent>;

export type ResolvedLogLevelConfig = {
  configuredLogLevel: string | undefined;
  isFallback: boolean;
  normalizedLogLevel: string | undefined;
  resolvedLogLevel: pino.LevelWithSilent;
};

export type TelemetryRuntimeConfig = {
  configuredLogLevel: string | undefined;
  deploymentEnvironment: string | undefined;
  invalidLogLevelWarning: {
    configuredLogLevel: string;
    resolvedLogLevel: pino.LevelWithSilent;
  } | null;
  normalizedLogLevel: string | undefined;
  otlpEndpoint: string | undefined;
  otlpProtocol: string;
  resourceAttributes: Record<string, string>;
  resolvedLogLevel: pino.LevelWithSilent;
  serviceName: string;
  serviceVersion: string;
};

const readEnv = (name: string): string | undefined => {
  const value = process.env[name];

  if (value == null) {
    return undefined;
  }

  const trimmedValue = value.trim();

  if (trimmedValue === "") {
    return undefined;
  }

  return trimmedValue;
};

export const resolveLogLevel = (
  configuredLogLevel: string | undefined,
): ResolvedLogLevelConfig => {
  const normalizedLogLevel = configuredLogLevel?.trim().toLowerCase();

  if (normalizedLogLevel == null || normalizedLogLevel === "") {
    return {
      configuredLogLevel,
      isFallback: false,
      normalizedLogLevel,
      resolvedLogLevel: DEFAULT_LOG_LEVEL,
    };
  }

  const pinoLogLevel = PINO_LOG_LEVELS[normalizedLogLevel as keyof typeof PINO_LOG_LEVELS];

  if (pinoLogLevel !== undefined) {
    return {
      configuredLogLevel,
      isFallback: false,
      normalizedLogLevel,
      resolvedLogLevel: pinoLogLevel,
    };
  }

  const dotnetLogLevel =
    DOTNET_TO_PINO_LOG_LEVELS[
      normalizedLogLevel as keyof typeof DOTNET_TO_PINO_LOG_LEVELS
    ];

  if (dotnetLogLevel !== undefined) {
    return {
      configuredLogLevel,
      isFallback: false,
      normalizedLogLevel,
      resolvedLogLevel: dotnetLogLevel,
    };
  }

  return {
    configuredLogLevel,
    isFallback: true,
    normalizedLogLevel,
    resolvedLogLevel: DEFAULT_LOG_LEVEL,
  };
};

const sanitizeOtlpEndpoint = (endpoint: string | undefined): string | undefined => {
  if (endpoint == null) {
    return undefined;
  }

  try {
    const url = new URL(endpoint);

    if (url.username !== "") {
      url.username = "";
    }

    if (url.password !== "") {
      url.password = "";
    }

    return url.toString();
  } catch {
    if (endpoint.includes("@")) {
      return INVALID_OTLP_ENDPOINT_LOG_VALUE;
    }

    return endpoint;
  }
};

export const getTelemetryRuntimeConfig = (): TelemetryRuntimeConfig => {
  const configuredLogLevel = readEnv("LOG_LEVEL");
  const resolvedLogLevelConfig = resolveLogLevel(configuredLogLevel);
  const serviceName = readEnv("OTEL_SERVICE_NAME") ?? DEFAULT_SERVICE_NAME;
  const serviceVersion = readEnv("APP_VERSION") ?? DEFAULT_SERVICE_VERSION;
  const deploymentEnvironment = readEnv("NODE_ENV");
  const otlpEndpoint = sanitizeOtlpEndpoint(
    readEnv("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT") ?? readEnv("OTEL_EXPORTER_OTLP_ENDPOINT"),
  );
  const otlpProtocol =
    readEnv("OTEL_EXPORTER_OTLP_LOGS_PROTOCOL") ??
    readEnv("OTEL_EXPORTER_OTLP_PROTOCOL") ??
    DEFAULT_OTLP_PROTOCOL;
  const resourceAttributes: Record<string, string> = {
    [ATTR_SERVICE_NAME]: serviceName,
    [ATTR_SERVICE_VERSION]: serviceVersion,
  };

  if (deploymentEnvironment !== undefined) {
    resourceAttributes[SEMRESATTRS_DEPLOYMENT_ENVIRONMENT] = deploymentEnvironment;
  }

  return {
    configuredLogLevel: resolvedLogLevelConfig.configuredLogLevel,
    deploymentEnvironment,
    invalidLogLevelWarning:
      resolvedLogLevelConfig.isFallback &&
      resolvedLogLevelConfig.configuredLogLevel !== undefined
        ? {
            configuredLogLevel: resolvedLogLevelConfig.configuredLogLevel,
            resolvedLogLevel: resolvedLogLevelConfig.resolvedLogLevel,
          }
        : null,
    normalizedLogLevel: resolvedLogLevelConfig.normalizedLogLevel,
    otlpEndpoint,
    otlpProtocol,
    resourceAttributes,
    resolvedLogLevel: resolvedLogLevelConfig.resolvedLogLevel,
    serviceName,
    serviceVersion,
  };
};
