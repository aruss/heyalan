import type { CreateClientConfig } from './api/client.gen'; // Import FROM generated files

export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseUrl: process.env.WEBAPI_ENDPOINT || '/api',
});