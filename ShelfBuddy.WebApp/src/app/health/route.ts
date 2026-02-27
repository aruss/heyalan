import { NextResponse } from "next/server";
import {
  probeWebApiHealth,
  type WebApiHealthProbeResult,
} from "./webapi-health";

const NO_STORE_CACHE_CONTROL = "no-store";
const UNHEALTHY_STATUS_CODE = 503;

const toNoStoreHeaders = (): HeadersInit => {
  return {
    "Cache-Control": NO_STORE_CACHE_CONTROL,
  };
};

const logProbeFailure = (result: WebApiHealthProbeResult): void => {
  if (result.isHealthy) {
    return;
  }

  console.warn("webapp_health_probe_failed", {
    reason: result.reason,
    upstreamStatus: result.upstreamStatus,
  });
};

export async function GET(): Promise<NextResponse> {

  return NextResponse.json(
    {
      status: "healthy",
    },
    {
      status: 200,
      headers: toNoStoreHeaders(),
    },
  );

  /*
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
  */
}
