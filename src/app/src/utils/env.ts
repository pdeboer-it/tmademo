export function baseUrl() {
  return import.meta.env.MODE === 'development'
    ? 'http://localhost:5280'
    : 'https://tmademoapi-e9brgbcacubfgqa3.westeurope-01.azurewebsites.net'
}
