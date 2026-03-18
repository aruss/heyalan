import test from "node:test";
import assert from "node:assert/strict";
import pino from "pino";
import {
  getTelemetryRuntimeConfig,
  resolveLogLevel,
} from "./telemetry-config.ts";
import { createWebAppStartupLogFields } from "./webapp-startup-log.ts";

const withEnvironment = async (
  entries: Record<string, string | undefined>,
  action: () => void | Promise<void>,
): Promise<void> => {
  const previousValues = new Map<string, string | undefined>();

  for (const [key, value] of Object.entries(entries)) {
    previousValues.set(key, process.env[key]);

    if (value === undefined) {
      delete process.env[key];
      continue;
    }

    process.env[key] = value;
  }

  try {
    await action();
  } finally {
    for (const [key, value] of previousValues.entries()) {
      if (value === undefined) {
        delete process.env[key];
        continue;
      }

      process.env[key] = value;
    }
  }
};

test("resolveLogLevel maps .NET log levels to pino levels case-insensitively", () => {
  assert.equal(resolveLogLevel("Trace").resolvedLogLevel, "trace");
  assert.equal(resolveLogLevel("DEBUG").resolvedLogLevel, "debug");
  assert.equal(resolveLogLevel("Information").resolvedLogLevel, "info");
  assert.equal(resolveLogLevel("warning").resolvedLogLevel, "warn");
  assert.equal(resolveLogLevel("Error").resolvedLogLevel, "error");
  assert.equal(resolveLogLevel("Critical").resolvedLogLevel, "fatal");
  assert.equal(resolveLogLevel("None").resolvedLogLevel, "silent");
});

test("resolveLogLevel preserves existing pino aliases and falls back safely", () => {
  assert.equal(resolveLogLevel("warn").resolvedLogLevel, "warn");
  assert.equal(resolveLogLevel("fatal").resolvedLogLevel, "fatal");
  assert.equal(resolveLogLevel(undefined).resolvedLogLevel, "info");
  assert.equal(resolveLogLevel("   ").resolvedLogLevel, "info");

  const invalidLevelResult = resolveLogLevel("verbose");

  assert.equal(invalidLevelResult.resolvedLogLevel, "info");
  assert.equal(invalidLevelResult.isFallback, true);
});

test("resolved pino level controls whether debug startup logs are enabled", () => {
  const debugLogger = pino({ level: resolveLogLevel("Debug").resolvedLogLevel });
  const informationLogger = pino({
    level: resolveLogLevel("Information").resolvedLogLevel,
  });

  assert.equal(debugLogger.isLevelEnabled("debug"), true);
  assert.equal(informationLogger.isLevelEnabled("debug"), false);
});

test("getTelemetryRuntimeConfig aligns resource attributes and sanitizes OTLP endpoint", async () => {
  await withEnvironment(
    {
      APP_VERSION: "2.3.4",
      LOG_LEVEL: "Critical",
      NODE_ENV: "Development",
      OTEL_EXPORTER_OTLP_ENDPOINT: "http://user:secret@example.com:4317/logs",
      OTEL_EXPORTER_OTLP_PROTOCOL: "grpc",
      OTEL_SERVICE_NAME: "custom-webapp",
    },
    () => {
      const runtimeConfig = getTelemetryRuntimeConfig();

      assert.equal(runtimeConfig.serviceName, "custom-webapp");
      assert.equal(runtimeConfig.serviceVersion, "2.3.4");
      assert.equal(runtimeConfig.deploymentEnvironment, "Development");
      assert.equal(runtimeConfig.resolvedLogLevel, "fatal");
      assert.equal(runtimeConfig.otlpEndpoint, "http://example.com:4317/logs");
      assert.equal(runtimeConfig.otlpProtocol, "grpc");
      assert.deepEqual(runtimeConfig.resourceAttributes, {
        "deployment.environment": "Development",
        "service.name": "custom-webapp",
        "service.version": "2.3.4",
      });
    },
  );
});

test("createWebAppStartupLogFields exposes only the expected startup metadata", async () => {
  await withEnvironment(
    {
      APP_VERSION: "9.9.9",
      LOG_LEVEL: "Debug",
      NODE_ENV: "Development",
      OTEL_EXPORTER_OTLP_LOGS_ENDPOINT: "http://collector:4317",
      OTEL_EXPORTER_OTLP_LOGS_PROTOCOL: "grpc",
      OTEL_SERVICE_NAME: "buyalan-webapp",
    },
    () => {
      const startupFields = createWebAppStartupLogFields(
        getTelemetryRuntimeConfig(),
        "nodejs",
        process.env.NODE_ENV,
      );

      assert.deepEqual(Object.keys(startupFields).sort(), [
        "deploymentEnvironment",
        "eventName",
        "logLevelConfigured",
        "logLevelResolved",
        "nextRuntime",
        "nodeEnv",
        "otlpEndpoint",
        "otlpProtocol",
        "serviceName",
        "serviceVersion",
      ]);
      assert.deepEqual(startupFields, {
        deploymentEnvironment: "Development",
        eventName: "webapp_started",
        logLevelConfigured: "Debug",
        logLevelResolved: "debug",
        nextRuntime: "nodejs",
        nodeEnv: "Development",
        otlpEndpoint: "http://collector:4317/",
        otlpProtocol: "grpc",
        serviceName: "buyalan-webapp",
        serviceVersion: "9.9.9",
      });
    },
  );
});
