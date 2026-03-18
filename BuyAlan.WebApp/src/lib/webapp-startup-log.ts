import type { TelemetryRuntimeConfig } from "@/lib/telemetry-config";

export type WebAppStartupLogFields = {
  deploymentEnvironment: string | undefined;
  eventName: string;
  logLevelConfigured: string | undefined;
  logLevelResolved: string;
  nextRuntime: string | undefined;
  nodeEnv: string | undefined;
  otlpEndpoint: string | undefined;
  otlpProtocol: string;
  serviceName: string;
  serviceVersion: string;
};

const WEBAPP_STARTED_EVENT_NAME = "webapp_started";

export const createWebAppStartupLogFields = (
  telemetryRuntimeConfig: TelemetryRuntimeConfig,
  nextRuntime: string | undefined,
  nodeEnv: string | undefined,
): WebAppStartupLogFields => {
  return {
    deploymentEnvironment: telemetryRuntimeConfig.deploymentEnvironment,
    eventName: WEBAPP_STARTED_EVENT_NAME,
    logLevelConfigured: telemetryRuntimeConfig.configuredLogLevel,
    logLevelResolved: telemetryRuntimeConfig.resolvedLogLevel,
    nextRuntime,
    nodeEnv,
    otlpEndpoint: telemetryRuntimeConfig.otlpEndpoint,
    otlpProtocol: telemetryRuntimeConfig.otlpProtocol,
    serviceName: telemetryRuntimeConfig.serviceName,
    serviceVersion: telemetryRuntimeConfig.serviceVersion,
  };
};
