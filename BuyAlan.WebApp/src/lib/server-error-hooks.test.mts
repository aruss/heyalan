import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import { registerServerErrorHooks } from "./server-error-hooks.ts";

const SERVER_ERROR_HOOKS_SENTINEL_KEY =
  "__buyalanWebAppServerErrorHooksRegistered__";

type ProcessEventHandler = (...arguments_: unknown[]) => void;

const resetServerErrorHooksSentinel = (): void => {
  delete (globalThis as typeof globalThis & {
    [SERVER_ERROR_HOOKS_SENTINEL_KEY]?: boolean;
  })[SERVER_ERROR_HOOKS_SENTINEL_KEY];
};

test("registerServerErrorHooks registers monitor-based fatal handling once", () => {
  resetServerErrorHooksSentinel();

  const registeredEvents: string[] = [];
  const originalProcessOn = process.on;

  process.on = ((event: string, listener: ProcessEventHandler) => {
    registeredEvents.push(event);
    return process;
  }) as typeof process.on;

  try {
    registerServerErrorHooks();
    registerServerErrorHooks();
  } finally {
    process.on = originalProcessOn;
    resetServerErrorHooksSentinel();
  }

  assert.deepEqual(registeredEvents, [
    "unhandledRejection",
    "uncaughtExceptionMonitor",
  ]);
});

test("uncaughtExceptionMonitor writes fatal crash details synchronously to stderr", () => {
  resetServerErrorHooksSentinel();

  let monitorHandler: ProcessEventHandler | undefined;
  const originalProcessOn = process.on;
  const originalWriteSync = fs.writeSync;
  const writes: Array<{ fd: number; message: string }> = [];

  process.on = ((event: string, listener: ProcessEventHandler) => {
    if (event === "uncaughtExceptionMonitor") {
      monitorHandler = listener;
    }

    return process;
  }) as typeof process.on;

  fs.writeSync = ((fd: number, message: string | Uint8Array) => {
    writes.push({
      fd,
      message:
        typeof message === "string"
          ? message
          : Buffer.from(message).toString("utf8"),
    });

    return typeof message === "string" ? message.length : message.byteLength;
  }) as typeof fs.writeSync;

  try {
    registerServerErrorHooks();
    assert.notEqual(monitorHandler, undefined);

    const error = new Error("boom");

    monitorHandler?.(error, "uncaughtException");
  } finally {
    process.on = originalProcessOn;
    fs.writeSync = originalWriteSync;
    resetServerErrorHooksSentinel();
  }

  assert.equal(writes.length, 2);
  assert.equal(writes[0]?.fd, process.stderr.fd);
  assert.match(
    writes[0]?.message ?? "",
    /Fatal server error\. Unhandled server exception \(uncaughtException\)/,
  );
  assert.equal(writes[1]?.fd, process.stderr.fd);
  assert.match(writes[1]?.message ?? "", /Fatal server error\. boom/);
});
