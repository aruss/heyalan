// eslint-disable-next-line @typescript-eslint/no-require-imports
const { PHASE_PRODUCTION_BUILD } = require('next/constants');

/** @type {import('next').NextConfig} */
const nextConfig = (phase) => {
    const isBuild = phase === PHASE_PRODUCTION_BUILD;

    // Check runtime configuration 
    if (!isBuild) {
        console.log("Checking runtime configuration");

        if (!process.env.WEBAPI_ENDPOINT) {
            throw new Error('Configuration failed: WEBAPI_ENDPOINT environment variable is missing.');
        }
    }


    return {
        trailingSlash: false,
        output: "standalone",
        webpack: (config, { dev }) => {
            if (dev) {
                // FORCE this. Default 'eval-source-map' fails in VS.
                config.devtool = 'source-map';
            }
            return config;
        },
    };
};

module.exports = nextConfig;