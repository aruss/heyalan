import "server-only";

import { once } from "node:events";
import pino, {
  type Logger,
  type LoggerOptions,
  type TransportTargetOptions,
} from "pino";
import type { Options as OpenTelemetryTransportOptions } from "pino-opentelemetry-transport";
import { getTelemetryRuntimeConfig } from "@/lib/telemetry-config";

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

const telemetryRuntimeConfig = getTelemetryRuntimeConfig();

const toLoggerOptions = (): LoggerOptions => {
  return {
    base: undefined,
    level: telemetryRuntimeConfig.resolvedLogLevel,
    redact: {
      censor: REDACTED_LOG_VALUE,
      paths: [...REDACT_PATHS],
    },
    timestamp: pino.stdTimeFunctions.isoTime,
  };
};

const toTransportOptions = (): OpenTelemetryTransportOptions => {
  const transportOptions: OpenTelemetryTransportOptions = {
    loggerName: telemetryRuntimeConfig.serviceName,
    resourceAttributes: telemetryRuntimeConfig.resourceAttributes,
    serviceVersion: telemetryRuntimeConfig.serviceVersion,
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
export const loggerRuntimeConfig = telemetryRuntimeConfig;

const waitForTransportEvent = async (eventName: "error" | "ready"): Promise<void> => {
  await once(transport, eventName);
};

const transportReadyPromise = Promise.race([
  waitForTransportEvent("ready"),
  waitForTransportEvent("error"),
]);

export const createLogger = (bindings: LoggerBindings): Logger => {
  return logger.child(bindings);
};

export const waitForLoggerTransportReady = async (): Promise<void> => {
  await transportReadyPromise;
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

if (loggerRuntimeConfig.invalidLogLevelWarning !== null) {
  logger.warn(
    {
      configuredLogLevel: loggerRuntimeConfig.invalidLogLevelWarning.configuredLogLevel,
      eventName: "webapp_invalid_log_level",
      resolvedLogLevel: loggerRuntimeConfig.invalidLogLevelWarning.resolvedLogLevel,
    },
    "Invalid LOG_LEVEL configured. Falling back to info.",
  );
}
