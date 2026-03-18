import type { TelemetryRuntimeConfig } from "@/lib/telemetry-config";

const HEALTH_LOG_PROBE_QUERY_PARAM = "logProbe";
const HEALTH_LOG_PROBE_ENABLED_VALUE = "1";
const HEALTH_LOG_PROBE_EVENT_NAME = "webapp_health_log_probe";

export type HealthLogProbeFields = {
  deploymentEnvironment: string | undefined;
  eventName: string;
  logLevelConfigured: string | undefined;
  logLevelResolved: string;
  method: string;
  otlpEndpoint: string | undefined;
  otlpProtocol: string;
  route: string;
  serviceName: string;
  serviceVersion: string;
};

export const shouldEmitHealthLogProbe = (requestUrl: string): boolean => {
  const url = new URL(requestUrl);
  const queryValue = url.searchParams.get(HEALTH_LOG_PROBE_QUERY_PARAM);

  return queryValue === HEALTH_LOG_PROBE_ENABLED_VALUE;
};

export const createHealthLogProbeFields = (
  telemetryRuntimeConfig: TelemetryRuntimeConfig,
  method: string,
): HealthLogProbeFields => {
  return {
    deploymentEnvironment: telemetryRuntimeConfig.deploymentEnvironment,
    eventName: HEALTH_LOG_PROBE_EVENT_NAME,
    logLevelConfigured: telemetryRuntimeConfig.configuredLogLevel,
    logLevelResolved: telemetryRuntimeConfig.resolvedLogLevel,
    method,
    otlpEndpoint: telemetryRuntimeConfig.otlpEndpoint,
    otlpProtocol: telemetryRuntimeConfig.otlpProtocol,
    route: "/health",
    serviceName: telemetryRuntimeConfig.serviceName,
    serviceVersion: telemetryRuntimeConfig.serviceVersion,
  };
};
