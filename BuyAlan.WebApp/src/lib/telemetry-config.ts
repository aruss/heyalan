import {
  ATTR_SERVICE_NAME,
  ATTR_SERVICE_VERSION,
  SEMRESATTRS_DEPLOYMENT_ENVIRONMENT,
} from "@opentelemetry/semantic-conventions";

const DEFAULT_LOG_LEVEL = "Information";
const DEFAULT_OTLP_LOGS_PROTOCOL = "grpc";
const DEFAULT_SERVICE_NAME = "buyalan-webapp";
const DEFAULT_SERVICE_VERSION = "0.0.0";
const GRPC_ENDPOINT_LOGS_SUFFIX = "/v1/logs";
const INVALID_OTLP_ENDPOINT_LOG_VALUE = "[invalid-otlp-endpoint]";
const SUPPORTED_OTLP_LOGS_PROTOCOL = "grpc";

const DOTNET_LOG_LEVELS = {
  critical: "Critical",
  debug: "Debug",
  error: "Error",
  information: "Information",
  none: "None",
  trace: "Trace",
  warning: "Warning",
} as const satisfies Record<string, string>;

export type DotNetLogLevel =
  (typeof DOTNET_LOG_LEVELS)[keyof typeof DOTNET_LOG_LEVELS];

export type ResolvedLogLevelConfig = {
  configuredLogLevel: string | undefined;
  isFallback: boolean;
  normalizedLogLevel: string | undefined;
  resolvedLogLevel: DotNetLogLevel;
};

export type TelemetryRuntimeConfig = {
  configuredLogLevel: string | undefined;
  deploymentEnvironment: string | undefined;
  invalidLogLevelWarning: {
    configuredLogLevel: string;
    resolvedLogLevel: DotNetLogLevel;
  } | null;
  normalizedLogLevel: string | undefined;
  otlpEndpoint: string | undefined;
  otlpLogsExportEnabled: boolean;
  otlpLogsExportWarning: {
    configuredProtocol: string;
    reason: "unsupported-protocol";
  } | null;
  otlpProtocol: string;
  resourceAttributes: Record<string, string>;
  resolvedLogLevel: DotNetLogLevel;
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

const normalizeGrpcEndpointPath = (url: URL): URL => {
  if (url.pathname.endsWith(GRPC_ENDPOINT_LOGS_SUFFIX)) {
    const strippedPath = url.pathname.slice(
      0,
      url.pathname.length - GRPC_ENDPOINT_LOGS_SUFFIX.length,
    );

    url.pathname = strippedPath === "" ? "/" : strippedPath;
  }

  return url;
};

const sanitizeOtlpEndpoint = (endpoint: string | undefined): string | undefined => {
  if (endpoint == null) {
    return undefined;
  }

  try {
    const url = normalizeGrpcEndpointPath(new URL(endpoint));

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

  const dotNetLogLevel =
    DOTNET_LOG_LEVELS[normalizedLogLevel as keyof typeof DOTNET_LOG_LEVELS];

  if (dotNetLogLevel !== undefined) {
    return {
      configuredLogLevel,
      isFallback: false,
      normalizedLogLevel,
      resolvedLogLevel: dotNetLogLevel,
    };
  }

  return {
    configuredLogLevel,
    isFallback: true,
    normalizedLogLevel,
    resolvedLogLevel: DEFAULT_LOG_LEVEL,
  };
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
    DEFAULT_OTLP_LOGS_PROTOCOL;
  const normalizedOtlpProtocol = otlpProtocol.toLowerCase();
  const otlpLogsExportEnabled = normalizedOtlpProtocol === SUPPORTED_OTLP_LOGS_PROTOCOL;
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
    otlpLogsExportEnabled,
    otlpLogsExportWarning: otlpLogsExportEnabled
      ? null
      : {
          configuredProtocol: otlpProtocol,
          reason: "unsupported-protocol",
        },
    otlpProtocol,
    resourceAttributes,
    resolvedLogLevel: resolvedLogLevelConfig.resolvedLogLevel,
    serviceName,
    serviceVersion,
  };
};
