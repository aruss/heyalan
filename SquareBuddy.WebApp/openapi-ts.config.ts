import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:5000/openapi/v1.json',
  output: './src/lib/api',
  plugins: [
    {
      name: '@hey-api/client-next',
      runtimeConfigPath: '../hey-api-config'
    }, 
    '@hey-api/sdk',
    '@tanstack/react-query'
  ],
});
