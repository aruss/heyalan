import { NextResponse } from "next/server";
import { createLogger, loggerRuntimeConfig } from "@/lib/logger";
import {
  probeWebApiHealth,
  type WebApiHealthProbeResult,
} from "./webapi-health";
import {
  createHealthLogProbeFields,
  shouldEmitHealthLogProbe,
} from "./health-log-probe";

const NO_STORE_CACHE_CONTROL = "no-store";
const UNHEALTHY_STATUS_CODE = 503;
const healthLogger = createLogger({
  module: "health-route",
  route: "/health",
});

const toNoStoreHeaders = (): HeadersInit => {
  return {
    "Cache-Control": NO_STORE_CACHE_CONTROL,
  };
};

const logProbeFailure = (result: WebApiHealthProbeResult): void => {
  if (result.isHealthy) {
    return;
  }

  healthLogger.warning(
    {
      eventName: "webapp_health_probe_failed",
      reason: result.reason,
      upstreamStatus: result.upstreamStatus,
    },
    "Web API health probe failed",
  );
};

export async function GET(request: Request): Promise<NextResponse> {
  if (shouldEmitHealthLogProbe(request.url)) {
    healthLogger.information(
      createHealthLogProbeFields(loggerRuntimeConfig, request.method),
      "WebApp OTEL log probe",
    );
  }

  const healthResult = await probeWebApiHealth(process.env.WEBAPI_ENDPOINT, fetch);

  if (healthResult.isHealthy) {
    return NextResponse.json(
      {
        status: "healthy",
      },
      {
        status: 200,
        headers: toNoStoreHeaders(),
      },
    );
  }

  logProbeFailure(healthResult);

  return NextResponse.json(
    {
      status: "unhealthy",
      reason: healthResult.reason,
    },
    {
      status: UNHEALTHY_STATUS_CODE,
      headers: toNoStoreHeaders(),
    },
  );
}
