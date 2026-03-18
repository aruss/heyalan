import { NodeSDK } from "@opentelemetry/sdk-node";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-grpc";
import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-grpc";
import { PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics";
import { resourceFromAttributes } from "@opentelemetry/resources";
import {
  getLoggerTransportReadyTimeoutMs,
  logger,
  loggerRuntimeConfig,
  waitForLoggerTransportReady,
} from "@/lib/logger";
import { registerServerErrorHooks } from "@/lib/server-error-hooks";
import { createWebAppStartupLogFields } from "@/lib/webapp-startup-log";

const WEBAPP_STARTED_MESSAGE = "WebApp is started with environment configuration";

const sdk = new NodeSDK({
  metricReader: new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter(),
  }),
  resource: resourceFromAttributes(loggerRuntimeConfig.resourceAttributes),
  traceExporter: new OTLPTraceExporter(),
});

const emitStartupLog = async (): Promise<void> => {
  const transportReadyState = await waitForLoggerTransportReady();

  if (transportReadyState === "error") {
    logger.warn(
      {
        eventName: "webapp_logger_transport_ready_failed",
      },
      "Logger transport failed before startup debug log was emitted.",
    );
  }

  if (transportReadyState === "timeout") {
    logger.warn(
      {
        eventName: "webapp_logger_transport_ready_timeout",
        timeoutMs: getLoggerTransportReadyTimeoutMs(),
      },
      "Logger transport was not ready before the startup debug log timeout elapsed.",
    );
  }

  if (!logger.isLevelEnabled("debug")) {
    return;
  }

  logger.debug(
    createWebAppStartupLogFields(
      loggerRuntimeConfig,
      process.env.NEXT_RUNTIME,
      process.env.NODE_ENV,
    ),
    WEBAPP_STARTED_MESSAGE,
  );
};

registerServerErrorHooks();
sdk.start();
void emitStartupLog();
