export function baseUrl() {
  console.log(import.meta.env.MODE)

  return import.meta.env.MODE === 'development'
    ? 'http://localhost:5280'
    : 'tmademoapi-e9brgbcacubfgqa3.westeurope-01.azurewebsites.net'
}
