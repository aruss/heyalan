// instrumentation.ts

export async function register() {
    if (process.env.NEXT_RUNTIME === 'nodejs') {
        await import('./instrumentation.otel');
    }

    if (process.env.NEXT_RUNTIME === 'edge') {
        throw new Error("Edge runtime not supported yet");
    }
}