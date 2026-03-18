import "server-only";

import fs from "node:fs";
import {
  logger,
  serializeError,
} from "@/lib/logger";

const SERVER_ERROR_HOOKS_SENTINEL_KEY =
  "__buyalanWebAppServerErrorHooksRegistered__";

type GlobalSentinelState = typeof globalThis & {
  [SERVER_ERROR_HOOKS_SENTINEL_KEY]?: boolean;
};

const globalSentinelState = globalThis as GlobalSentinelState;

const writeEmergencyErrorLog = (error: unknown): void => {
  const serializedError = serializeError(error);
  const stackSuffix =
    serializedError.stack == null ? "" : `\n${serializedError.stack}`;

  fs.writeSync(
    process.stderr.fd,
    `[webapp] Fatal server error. ${serializedError.errorMessage}${stackSuffix}\n`,
  );
};

const handleUncaughtExceptionMonitor = (
  error: Error,
  origin: NodeJS.UncaughtExceptionOrigin,
): void => {
  writeEmergencyErrorLog({
    message: `Unhandled server exception (${origin})`,
    name: "UncaughtExceptionMonitor",
    stack: error.stack,
  });
  writeEmergencyErrorLog(error);
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
    "uncaughtExceptionMonitor",
    (error: Error, origin: NodeJS.UncaughtExceptionOrigin) => {
      handleUncaughtExceptionMonitor(error, origin);
    },
  );
};
