// instrumentation.otel.ts

import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';
import { logger } from '@/lib/logger';
import { registerServerErrorHooks } from '@/lib/server-error-hooks';

const sdk = new NodeSDK({
    resource: resourceFromAttributes({
        [ATTR_SERVICE_NAME]: process.env.OTEL_SERVICE_NAME || 'buyalan-webapp',
    }),
    traceExporter: new OTLPTraceExporter(),
    metricReader: new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter(),
    }),
});

registerServerErrorHooks();
sdk.start();
logger.debug(
    {
        eventName: "webapp_started",
        nextRuntime: process.env.NEXT_RUNTIME,
        serviceName: process.env.OTEL_SERVICE_NAME || 'buyalan-webapp',
    },
    "WebApp started",
);
