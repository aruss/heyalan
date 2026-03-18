import "server-only";

import { logs, SeverityNumber } from "@opentelemetry/api-logs";
import { OTLPLogExporter } from "@opentelemetry/exporter-logs-otlp-grpc";
import { resourceFromAttributes } from "@opentelemetry/resources";
import {
  BatchLogRecordProcessor,
  LoggerProvider,
} from "@opentelemetry/sdk-logs";
import {
  type DotNetLogLevel,
  getTelemetryRuntimeConfig,
} from "@/lib/telemetry-config";

const LOGGER_FLUSH_TIMEOUT_MS = 1000;
const OTEL_LOGGER_GLOBAL_KEY = "__buyalanWebAppOtelLogger__";

export type LogAttributeValue = boolean | number | string | null | undefined;
export type LoggerBindings = Record<string, LogAttributeValue>;
export type SerializedError = {
  errorCode?: string;
  errorMessage: string;
  errorName?: string;
  errorType: string;
  stack?: string;
};

type LogFields = LoggerBindings;
type LogLevelPriority = Record<DotNetLogLevel, number>;
type LoggerMethodArguments =
  | [message: string]
  | [message: string, fields: LogFields]
  | [fields: LogFields, message: string];

export type WebAppLogger = {
  critical: (...arguments_: LoggerMethodArguments) => void;
  debug: (...arguments_: LoggerMethodArguments) => void;
  error: (...arguments_: LoggerMethodArguments) => void;
  information: (...arguments_: LoggerMethodArguments) => void;
  isLevelEnabled: (level: DotNetLogLevel) => boolean;
  log: (level: DotNetLogLevel, message: string, fields?: LogFields) => void;
  trace: (...arguments_: LoggerMethodArguments) => void;
  warning: (...arguments_: LoggerMethodArguments) => void;
};

type OtelLoggerState = {
  provider: LoggerProvider;
  logger: ReturnType<typeof logs.getLogger>;
};

type GlobalLoggerState = typeof globalThis & {
  [OTEL_LOGGER_GLOBAL_KEY]?: OtelLoggerState;
};

const globalLoggerState = globalThis as GlobalLoggerState;
const loggerRuntimeConfig = getTelemetryRuntimeConfig();
const LOG_LEVEL_PRIORITY: LogLevelPriority = {
  Critical: 5,
  Debug: 1,
  Error: 4,
  Information: 2,
  None: 6,
  Trace: 0,
  Warning: 3,
};
const OTEL_SEVERITY_NUMBER: Record<Exclude<DotNetLogLevel, "None">, SeverityNumber> = {
  Critical: SeverityNumber.FATAL,
  Debug: SeverityNumber.DEBUG,
  Error: SeverityNumber.ERROR,
  Information: SeverityNumber.INFO,
  Trace: SeverityNumber.TRACE,
  Warning: SeverityNumber.WARN,
};

const writeBootstrapError = (
  message: string,
  fields: Record<string, LogAttributeValue> = {},
): void => {
  console.error(`[webapp] ${message}`, fields);
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

const createLoggerProvider = (): LoggerProvider => {
  if (!loggerRuntimeConfig.otlpLogsExportEnabled) {
    writeBootstrapError(
      "Unsupported OTLP logs protocol configured. Disabling OTEL log export.",
      {
        configuredProtocol:
          loggerRuntimeConfig.otlpLogsExportWarning?.configuredProtocol,
        eventName: "webapp_otel_logs_protocol_unsupported",
      },
    );

    return new LoggerProvider({
      resource: resourceFromAttributes(loggerRuntimeConfig.resourceAttributes),
    });
  }

  try {
    const exporterOptions =
      loggerRuntimeConfig.otlpEndpoint == null
        ? undefined
        : {
            url: loggerRuntimeConfig.otlpEndpoint,
          };

    return new LoggerProvider({
      processors: [
        new BatchLogRecordProcessor(new OTLPLogExporter(exporterOptions)),
      ],
      resource: resourceFromAttributes(loggerRuntimeConfig.resourceAttributes),
    });
  } catch (error) {
    writeBootstrapError("Failed to initialize OTEL log exporter.", {
      ...serializeError(error),
      eventName: "webapp_otel_logger_bootstrap_failed",
    });
  }

  return new LoggerProvider({
    resource: resourceFromAttributes(loggerRuntimeConfig.resourceAttributes),
  });
};

const getOtelLoggerState = (): OtelLoggerState => {
  const existingState = globalLoggerState[OTEL_LOGGER_GLOBAL_KEY];

  if (existingState != null) {
    return existingState;
  }

  const provider = createLoggerProvider();
  logs.setGlobalLoggerProvider(provider);

  const state: OtelLoggerState = {
    provider,
    logger: logs.getLogger(
      loggerRuntimeConfig.serviceName,
      loggerRuntimeConfig.serviceVersion,
    ),
  };

  globalLoggerState[OTEL_LOGGER_GLOBAL_KEY] = state;

  return state;
};

const otelLoggerState = getOtelLoggerState();

const resolveLogArguments = (
  arguments_: LoggerMethodArguments,
): {
  fields: LogFields;
  message: string;
} => {
  const [firstArgument, secondArgument] = arguments_;

  if (typeof firstArgument === "string") {
    return {
      fields:
        secondArgument != null && typeof secondArgument !== "string"
          ? secondArgument
          : {},
      message: firstArgument,
    };
  }

  return {
    fields: firstArgument,
    message: typeof secondArgument === "string" ? secondArgument : "",
  };
};

const mergeFields = (
  bindings: LoggerBindings,
  fields: LogFields,
): Record<string, boolean | number | string> => {
  const mergedFields = {
    ...bindings,
    ...fields,
  };
  const sanitizedFields: Record<string, boolean | number | string> = {};

  for (const [key, value] of Object.entries(mergedFields)) {
    if (value === undefined) {
      continue;
    }

    sanitizedFields[key] = value === null ? "null" : value;
  }

  return sanitizedFields;
};

const isLevelEnabled = (configuredLevel: DotNetLogLevel, level: DotNetLogLevel): boolean => {
  if (configuredLevel === "None" || level === "None") {
    return false;
  }

  return LOG_LEVEL_PRIORITY[level] >= LOG_LEVEL_PRIORITY[configuredLevel];
};

const createScopedLogger = (bindings: LoggerBindings): WebAppLogger => {
  const emitLog = (
    level: Exclude<DotNetLogLevel, "None">,
    message: string,
    fields: LogFields = {},
  ): void => {
    if (!isLevelEnabled(loggerRuntimeConfig.resolvedLogLevel, level)) {
      return;
    }

    otelLoggerState.logger.emit({
      attributes: mergeFields(bindings, fields),
      body: message,
      severityNumber: OTEL_SEVERITY_NUMBER[level],
      severityText: level,
    });
  };

  const createLevelMethod =
    (level: Exclude<DotNetLogLevel, "None">) =>
    (...arguments_: LoggerMethodArguments): void => {
      const resolvedArguments = resolveLogArguments(arguments_);

      emitLog(level, resolvedArguments.message, resolvedArguments.fields);
    };

  return {
    critical: createLevelMethod("Critical"),
    debug: createLevelMethod("Debug"),
    error: createLevelMethod("Error"),
    information: createLevelMethod("Information"),
    isLevelEnabled: (level: DotNetLogLevel): boolean => {
      return isLevelEnabled(loggerRuntimeConfig.resolvedLogLevel, level);
    },
    log: (level: DotNetLogLevel, message: string, fields: LogFields = {}): void => {
      if (level === "None") {
        return;
      }

      emitLog(level, message, fields);
    },
    trace: createLevelMethod("Trace"),
    warning: createLevelMethod("Warning"),
  };
};

export const logger = createScopedLogger({});
export { loggerRuntimeConfig };

export const createLogger = (bindings: LoggerBindings): WebAppLogger => {
  return createScopedLogger(bindings);
};

const awaitWithTimeout = async (promise: Promise<void>, timeoutMs: number): Promise<void> => {
  await Promise.race([
    promise.catch(() => {
      return undefined;
    }),
    new Promise<void>((resolve) => {
      setTimeout(resolve, timeoutMs);
    }),
  ]);
};

export const flushLogger = async (): Promise<void> => {
  await awaitWithTimeout(otelLoggerState.provider.forceFlush(), LOGGER_FLUSH_TIMEOUT_MS);
};

export const shutdownLoggerTransport = async (): Promise<void> => {
  await awaitWithTimeout(otelLoggerState.provider.shutdown(), LOGGER_FLUSH_TIMEOUT_MS);
};

if (loggerRuntimeConfig.invalidLogLevelWarning !== null) {
  logger.warning(
    {
      configuredLogLevel: loggerRuntimeConfig.invalidLogLevelWarning.configuredLogLevel,
      eventName: "webapp_invalid_log_level",
      resolvedLogLevel: loggerRuntimeConfig.invalidLogLevelWarning.resolvedLogLevel,
    },
    "Invalid LOG_LEVEL configured. Falling back to Information.",
  );
}
