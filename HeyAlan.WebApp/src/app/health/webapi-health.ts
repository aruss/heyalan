export const HEALTH_CHECK_TIMEOUT_MS = 3_000;
const WEBAPI_HEALTH_PATH = "/health";

export type HealthFailureReason =
  | "missing_config"
  | "upstream_non_200"
  | "upstream_unreachable"
  | "upstream_timeout";

export type WebApiHealthProbeResult =
  | { isHealthy: true; upstreamStatus: 200 }
  | { isHealthy: false; reason: HealthFailureReason; upstreamStatus?: number };

type FetchFunction = (input: string, init?: RequestInit) => Promise<Response>;

export const buildWebApiHealthUrl = (webApiEndpoint: string): string => {
  const normalizedEndpoint = webApiEndpoint.replace(/\/+$/, "");
  return `${normalizedEndpoint}${WEBAPI_HEALTH_PATH}`;
};

const isAbortError = (error: unknown): boolean => {
  if (!(error instanceof Error)) {
    return false;
  }

  return error.name === "AbortError";
};

export const probeWebApiHealth = async (
  webApiEndpoint: string | undefined,
  fetchFunction: FetchFunction,
  timeoutMs: number = HEALTH_CHECK_TIMEOUT_MS,
): Promise<WebApiHealthProbeResult> => {
  if (!webApiEndpoint) {
    return { isHealthy: false, reason: "missing_config" };
  }

  const webApiHealthUrl = buildWebApiHealthUrl(webApiEndpoint);
  const abortController = new AbortController();
  const timeoutHandle = setTimeout(() => {
    abortController.abort();
  }, timeoutMs);

  try {
    const upstreamResponse = await fetchFunction(webApiHealthUrl, {
      method: "GET",
      cache: "no-store",
      signal: abortController.signal,
    });

    if (upstreamResponse.status === 200) {
      return { isHealthy: true, upstreamStatus: 200 };
    }

    return {
      isHealthy: false,
      reason: "upstream_non_200",
      upstreamStatus: upstreamResponse.status,
    };
  } catch (error: unknown) {
    if (isAbortError(error)) {
      return { isHealthy: false, reason: "upstream_timeout" };
    }

    return { isHealthy: false, reason: "upstream_unreachable" };
  } finally {
    clearTimeout(timeoutHandle);
  }
};
