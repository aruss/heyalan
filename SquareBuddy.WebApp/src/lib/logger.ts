/*import pino from 'pino';
import pkg from '../package.json';

const transport = pino.transport({
  target: 'pino-opentelemetry-transport',
  options: {
    // Matches your .NET exporter configuration
    resourceAttributes: {
      'service.name': pkg.name,
    },
    logRecordProcessorOptions: {
      recordProcessorType: 'batch',
      exporterOptions: {
        protocol: 'grpc', // or 'http/protobuf'
        // Endpoint is typically inherited from OTEL_EXPORTER_OTLP_ENDPOINT
        // or set explicitly here if needed.
      },
    },
  },
});

export const logger = pino(
  {
    level: process.env.LOG_LEVEL || 'info',
    // Key renaming to match OTel Semantic Conventions
    timestamp: pino.stdTimeFunctions.isoTime,
    base: undefined, // Remove default pid/hostname (OTel Resource handles this)
  },
  transport
);
*/