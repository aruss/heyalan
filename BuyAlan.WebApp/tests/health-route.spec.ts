import { expect, test } from "@playwright/test";
import { GET } from "../src/app/health/route";

const originalFetch = globalThis.fetch;
const originalWebApiEndpoint = process.env.WEBAPI_ENDPOINT;
const createHealthRequest = (): Request => {
  return new Request("http://localhost/health", { method: "GET" });
};

test.afterEach(() => {
  globalThis.fetch = originalFetch;
  if (originalWebApiEndpoint === undefined) {
    delete process.env.WEBAPI_ENDPOINT;
    return;
  }

  process.env.WEBAPI_ENDPOINT = originalWebApiEndpoint;
});

test("returns 200 when WEBAPI /health responds with 200", async () => {
  process.env.WEBAPI_ENDPOINT = "http://webapi:5000";

  const fetchMock: typeof fetch = async (
    input: RequestInfo | URL,
    init?: RequestInit,
  ) => {
    const inputValue = input.toString();
    expect(inputValue).toBe("http://webapi:5000/health");
    expect(init?.method).toBe("GET");
    expect(init?.cache).toBe("no-store");
    return new Response(null, { status: 200 });
  };

  globalThis.fetch = fetchMock;

  const response = await GET(createHealthRequest());
  const payload = await response.json();

  expect(response.status).toBe(200);
  expect(response.headers.get("Cache-Control")).toBe("no-store");
  expect(payload).toEqual({ status: "healthy" });
});

test("returns unhealthy when WEBAPI /health returns non-200", async () => {
  process.env.WEBAPI_ENDPOINT = "http://webapi:5000";

  const fetchMock: typeof fetch = async () => {
    return new Response(null, { status: 502 });
  };

  globalThis.fetch = fetchMock;

  const response = await GET(createHealthRequest());
  const payload = await response.json();

  expect(response.status).toBe(503);
  expect(response.headers.get("Cache-Control")).toBe("no-store");
  expect(payload).toEqual({
    status: "unhealthy",
    reason: "upstream_non_200",
  });
});

test("returns unhealthy when WEBAPI /health probe times out", async () => {
  process.env.WEBAPI_ENDPOINT = "http://webapi:5000";

  const fetchMock: typeof fetch = async () => {
    const timeoutError = new Error("request timed out");
    Object.defineProperty(timeoutError, "name", { value: "AbortError" });
    return Promise.reject(timeoutError);
  };

  globalThis.fetch = fetchMock;

  const response = await GET(createHealthRequest());
  const payload = await response.json();

  expect(response.status).toBe(503);
  expect(payload).toEqual({
    status: "unhealthy",
    reason: "upstream_timeout",
  });
});

test("returns unhealthy when WEBAPI /health is unreachable", async () => {
  process.env.WEBAPI_ENDPOINT = "http://webapi:5000";

  const fetchMock: typeof fetch = async () => {
    return Promise.reject(new Error("network failure"));
  };

  globalThis.fetch = fetchMock;

  const response = await GET(createHealthRequest());
  const payload = await response.json();

  expect(response.status).toBe(503);
  expect(payload).toEqual({
    status: "unhealthy",
    reason: "upstream_unreachable",
  });
});

test("returns unhealthy when WEBAPI_ENDPOINT is missing", async () => {
  delete process.env.WEBAPI_ENDPOINT;
  let fetchCallCount = 0;

  const fetchMock: typeof fetch = async () => {
    fetchCallCount += 1;
    return new Response(null, { status: 200 });
  };

  globalThis.fetch = fetchMock;

  const response = await GET(createHealthRequest());
  const payload = await response.json();

  expect(fetchCallCount).toBe(0);
  expect(response.status).toBe(503);
  expect(payload).toEqual({
    status: "unhealthy",
    reason: "missing_config",
  });
});
