import assert from "node:assert/strict";
import test from "node:test";
import { getTelemetryRuntimeConfig } from "../../lib/telemetry-config.ts";
import {
  createHealthLogProbeFields,
  shouldEmitHealthLogProbe,
} from "./health-log-probe.ts";

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

test("shouldEmitHealthLogProbe only enables the probe for logProbe=1", () => {
  assert.equal(shouldEmitHealthLogProbe("http://localhost:3300/health"), false);
  assert.equal(
    shouldEmitHealthLogProbe("http://localhost:3300/health?logProbe=0"),
    false,
  );
  assert.equal(
    shouldEmitHealthLogProbe("http://localhost:3300/health?logProbe=1"),
    true,
  );
  assert.equal(
    shouldEmitHealthLogProbe("http://localhost:3300/health?foo=bar&logProbe=1"),
    true,
  );
});

test("createHealthLogProbeFields emits safe OTEL verification metadata", async () => {
  await withEnvironment(
    {
      APP_VERSION: "1.2.3",
      LOG_LEVEL: "Information",
      NODE_ENV: "Development",
      OTEL_EXPORTER_OTLP_LOGS_ENDPOINT: "http://collector:4317",
      OTEL_EXPORTER_OTLP_LOGS_PROTOCOL: "grpc",
      OTEL_SERVICE_NAME: "buyalan-webapp",
    },
    () => {
      const probeFields = createHealthLogProbeFields(
        getTelemetryRuntimeConfig(),
        "GET",
      );

      assert.deepEqual(probeFields, {
        deploymentEnvironment: "Development",
        eventName: "webapp_health_log_probe",
        logLevelConfigured: "Information",
        logLevelResolved: "Information",
        method: "GET",
        otlpEndpoint: "http://collector:4317/",
        otlpProtocol: "grpc",
        route: "/health",
        serviceName: "buyalan-webapp",
        serviceVersion: "1.2.3",
      });
    },
  );
});
