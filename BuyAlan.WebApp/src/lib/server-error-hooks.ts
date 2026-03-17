import "server-only";

import {
  flushLogger,
  logger,
  serializeError,
  shutdownLoggerTransport,
} from "@/lib/logger";

const UNCAUGHT_EXCEPTION_EXIT_CODE = 1;
const SERVER_ERROR_HOOKS_SENTINEL_KEY =
  "__buyalanWebAppServerErrorHooksRegistered__";
const FATAL_EXIT_SENTINEL_KEY = "__buyalanWebAppFatalExitScheduled__";

type GlobalSentinelState = typeof globalThis & {
  [FATAL_EXIT_SENTINEL_KEY]?: boolean;
  [SERVER_ERROR_HOOKS_SENTINEL_KEY]?: boolean;
};

const globalSentinelState = globalThis as GlobalSentinelState;

const writeEmergencyErrorLog = (error: unknown): void => {
  const serializedError = serializeError(error);
  const stackSuffix =
    serializedError.stack == null ? "" : `\n${serializedError.stack}`;

  console.error(
    `[webapp] Failed to persist fatal server error log. ${serializedError.errorMessage}${stackSuffix}`,
  );
};

const scheduleFatalExit = (): void => {
  if (globalSentinelState[FATAL_EXIT_SENTINEL_KEY] === true) {
    return;
  }

  globalSentinelState[FATAL_EXIT_SENTINEL_KEY] = true;

  setImmediate(() => {
    process.exit(UNCAUGHT_EXCEPTION_EXIT_CODE);
  });
};

const handleUncaughtException = async (
  error: Error,
  origin: NodeJS.UncaughtExceptionOrigin,
): Promise<void> => {
  scheduleFatalExit();

  try {
    logger.fatal(
      {
        eventName: "webapp_uncaught_exception",
        origin,
        ...serializeError(error),
      },
      "Unhandled server exception",
    );

    await flushLogger();
    await shutdownLoggerTransport();
  } catch (loggingError) {
    writeEmergencyErrorLog(loggingError);
    writeEmergencyErrorLog(error);
  }
};

export const registerServerErrorHooks = (): void => {
  if (globalSentinelState[SERVER_ERROR_HOOKS_SENTINEL_KEY] === true) {
    return;
  }

  globalSentinelState[SERVER_ERROR_HOOKS_SENTINEL_KEY] = true;

  process.on("unhandledRejection", (reason: unknown) => {
    logger.error(
      {
        eventName: "webapp_unhandled_rejection",
        ...serializeError(reason),
      },
      "Unhandled promise rejection",
    );
  });

  process.on(
    "uncaughtException",
    (error: Error, origin: NodeJS.UncaughtExceptionOrigin) => {
      void handleUncaughtException(error, origin);
    },
  );
};
