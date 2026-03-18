import assert from "node:assert/strict";
import test from "node:test";
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

test("resolveLogLevel accepts only .NET log levels case-insensitively", () => {
  assert.equal(resolveLogLevel("Trace").resolvedLogLevel, "Trace");
  assert.equal(resolveLogLevel("DEBUG").resolvedLogLevel, "Debug");
  assert.equal(resolveLogLevel("Information").resolvedLogLevel, "Information");
  assert.equal(resolveLogLevel("warning").resolvedLogLevel, "Warning");
  assert.equal(resolveLogLevel("Error").resolvedLogLevel, "Error");
  assert.equal(resolveLogLevel("Critical").resolvedLogLevel, "Critical");
  assert.equal(resolveLogLevel("None").resolvedLogLevel, "None");
});

test("resolveLogLevel defaults and falls back safely for blank or invalid values", () => {
  assert.equal(resolveLogLevel(undefined).resolvedLogLevel, "Information");
  assert.equal(resolveLogLevel("   ").resolvedLogLevel, "Information");

  const invalidLevelResult = resolveLogLevel("warn");

  assert.equal(invalidLevelResult.resolvedLogLevel, "Information");
  assert.equal(invalidLevelResult.isFallback, true);
});

test("getTelemetryRuntimeConfig aligns resource attributes and normalizes OTLP endpoint", async () => {
  await withEnvironment(
    {
      APP_VERSION: "2.3.4",
      LOG_LEVEL: "Critical",
      NODE_ENV: "Development",
      OTEL_EXPORTER_OTLP_ENDPOINT: "http://user:secret@example.com:4317/v1/logs",
      OTEL_EXPORTER_OTLP_PROTOCOL: "grpc",
      OTEL_SERVICE_NAME: "custom-webapp",
    },
    () => {
      const runtimeConfig = getTelemetryRuntimeConfig();

      assert.equal(runtimeConfig.serviceName, "custom-webapp");
      assert.equal(runtimeConfig.serviceVersion, "2.3.4");
      assert.equal(runtimeConfig.deploymentEnvironment, "Development");
      assert.equal(runtimeConfig.resolvedLogLevel, "Critical");
      assert.equal(runtimeConfig.otlpEndpoint, "http://example.com:4317/");
      assert.equal(runtimeConfig.otlpProtocol, "grpc");
      assert.equal(runtimeConfig.otlpLogsExportEnabled, true);
      assert.equal(runtimeConfig.otlpLogsExportWarning, null);
      assert.deepEqual(runtimeConfig.resourceAttributes, {
        "deployment.environment": "Development",
        "service.name": "custom-webapp",
        "service.version": "2.3.4",
      });
    },
  );
});

test("logs-specific OTLP env vars override global OTLP env vars", async () => {
  await withEnvironment(
    {
      OTEL_EXPORTER_OTLP_ENDPOINT: "http://global-collector:4317",
      OTEL_EXPORTER_OTLP_LOGS_ENDPOINT: "http://logs-collector:4317",
      OTEL_EXPORTER_OTLP_LOGS_PROTOCOL: "grpc",
      OTEL_EXPORTER_OTLP_PROTOCOL: "http/protobuf",
    },
    () => {
      const runtimeConfig = getTelemetryRuntimeConfig();

      assert.equal(runtimeConfig.otlpEndpoint, "http://logs-collector:4317/");
      assert.equal(runtimeConfig.otlpProtocol, "grpc");
      assert.equal(runtimeConfig.otlpLogsExportEnabled, true);
    },
  );
});

test("non-grpc OTLP logs protocols disable log export with a warning", async () => {
  await withEnvironment(
    {
      OTEL_EXPORTER_OTLP_LOGS_ENDPOINT: "http://collector:4318/v1/logs",
      OTEL_EXPORTER_OTLP_LOGS_PROTOCOL: "http/protobuf",
    },
    () => {
      const runtimeConfig = getTelemetryRuntimeConfig();

      assert.equal(runtimeConfig.otlpEndpoint, "http://collector:4318/");
      assert.equal(runtimeConfig.otlpProtocol, "http/protobuf");
      assert.equal(runtimeConfig.otlpLogsExportEnabled, false);
      assert.deepEqual(runtimeConfig.otlpLogsExportWarning, {
        configuredProtocol: "http/protobuf",
        reason: "unsupported-protocol",
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
        logLevelResolved: "Debug",
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
