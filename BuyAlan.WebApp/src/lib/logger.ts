import "server-only";

import pino, {
  type Logger,
  type LoggerOptions,
  type TransportTargetOptions,
} from "pino";
import type { Options as OpenTelemetryTransportOptions } from "pino-opentelemetry-transport";

const DEFAULT_LOG_LEVEL = "info";
const DEFAULT_SERVICE_NAME = "buyalan-webapp";
const DEFAULT_SERVICE_VERSION = "0.1.0";
const LOGGER_FLUSH_TIMEOUT_MS = 1000;
const REDACTED_LOG_VALUE = "[redacted]";
const REDACT_PATHS = [
  "authorization",
  "cookie",
  "headers.authorization",
  "headers.cookie",
  "password",
  "set-cookie",
  "token",
] as const;

export type LoggerBindings = Record<
  string,
  boolean | number | string | null | undefined
>;

export type SerializedError = {
  errorCode?: string;
  errorMessage: string;
  errorName?: string;
  errorType: string;
  stack?: string;
};

const toServiceName = (): string => {
  const serviceName = process.env.OTEL_SERVICE_NAME;

  if (serviceName == null || serviceName.trim() === "") {
    return DEFAULT_SERVICE_NAME;
  }

  return serviceName;
};

const toServiceVersion = (): string => {
  const serviceVersion = process.env.APP_VERSION;

  if (serviceVersion == null || serviceVersion.trim() === "") {
    return DEFAULT_SERVICE_VERSION;
  }

  return serviceVersion;
};

const toDeploymentEnvironment = (): string | undefined => {
  const deploymentEnvironment = process.env.NODE_ENV;

  if (deploymentEnvironment == null || deploymentEnvironment.trim() === "") {
    return undefined;
  }

  return deploymentEnvironment;
};

const toLoggerOptions = (): LoggerOptions => {
  return {
    base: undefined,
    level: process.env.LOG_LEVEL ?? DEFAULT_LOG_LEVEL,
    redact: {
      censor: REDACTED_LOG_VALUE,
      paths: [...REDACT_PATHS],
    },
    timestamp: pino.stdTimeFunctions.isoTime,
  };
};

const toTransportOptions = (): OpenTelemetryTransportOptions => {
  const serviceName = toServiceName();
  const serviceVersion = toServiceVersion();
  const deploymentEnvironment = toDeploymentEnvironment();
  const resourceAttributes: Record<string, string> = {
    "service.name": serviceName,
  };

  resourceAttributes["service.version"] = serviceVersion;

  if (deploymentEnvironment !== undefined) {
    resourceAttributes["deployment.environment"] = deploymentEnvironment;
  }

  const transportOptions: OpenTelemetryTransportOptions = {
    loggerName: serviceName,
    resourceAttributes,
    serviceVersion,
  };

  return transportOptions;
};

const transportTargets: TransportTargetOptions[] = [
  {
    options: {
      destination: 1,
    },
    target: "pino/file",
  },
  {
    options: toTransportOptions(),
    target: "pino-opentelemetry-transport",
  },
];

const transport = pino.transport({
  targets: transportTargets,
});

export const logger = pino(toLoggerOptions(), transport);

export const createLogger = (bindings: LoggerBindings): Logger => {
  return logger.child(bindings);
};

export const serializeError = (error: unknown): SerializedError => {
  if (error instanceof Error) {
    const serializedError: SerializedError = {
      errorMessage: error.message,
      errorName: error.name,
      errorType: "Error",
    };

    if (error.stack != null && error.stack !== "") {
      serializedError.stack = error.stack;
    }

    const errorWithCode = error as Error & { code?: unknown };

    if (typeof errorWithCode.code === "string" && errorWithCode.code !== "") {
      serializedError.errorCode = errorWithCode.code;
    }

    return serializedError;
  }

  if (typeof error === "string") {
    return {
      errorMessage: error,
      errorType: "string",
    };
  }

  if (
    typeof error === "number" ||
    typeof error === "boolean" ||
    typeof error === "bigint" ||
    typeof error === "symbol"
  ) {
    return {
      errorMessage: String(error),
      errorType: typeof error,
    };
  }

  return {
    errorMessage: "A non-Error value was thrown.",
    errorType: error === null ? "null" : Array.isArray(error) ? "array" : typeof error,
  };
};

const toSettledPromise = (
  executor: (resolve: () => void) => void,
  timeoutMs: number,
): Promise<void> => {
  return new Promise((resolve) => {
    let isSettled = false;

    const resolveOnce = (): void => {
      if (isSettled) {
        return;
      }

      isSettled = true;
      clearTimeout(timeoutHandle);
      resolve();
    };

    const timeoutHandle = setTimeout(resolveOnce, timeoutMs);

    executor(resolveOnce);
  });
};

export const flushLogger = async (): Promise<void> => {
  await toSettledPromise((resolve) => {
    logger.flush(() => {
      resolve();
    });
  }, LOGGER_FLUSH_TIMEOUT_MS);
};

export const shutdownLoggerTransport = async (): Promise<void> => {
  await toSettledPromise((resolve) => {
    transport.once("close", () => {
      resolve();
    });

    transport.once("error", () => {
      resolve();
    });

    transport.end();
  }, LOGGER_FLUSH_TIMEOUT_MS);
};
