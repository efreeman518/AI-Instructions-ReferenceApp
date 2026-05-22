# TaskFlow.React

React + TypeScript reference UI for TaskFlow.

## Stack

- Vite SPA
- React Router
- TanStack Query
- MUI
- Typed API client for `/api/v1`

## Local

Run through Aspire when the full stack is needed. AppHost passes `VITE_API_BASE_URL` and Vite proxies `/api` to the gateway.

```powershell
rtk dotnet run --project .\Host\Aspire\AppHost\AppHost.csproj
```

Standalone UI build:

```powershell
rtk npm install
rtk npm run build
```
