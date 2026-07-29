# IMS Warehouse Console

Ordering/Inventory/Organization 三個後端服務的倉儲作業前端，React + TanStack Query + react-router-dom + Tailwind CSS。

## 開發

```bash
pnpm install
pnpm dev
```

`vite.config.ts` 已設定 `/api/organization`、`/api/ordering`、`/api/inventory` 三組 dev proxy，對應三個後端服務。正式環境由 `infra/nginx/nginx.conf` 做同樣的路徑轉發。

## 建置

```bash
pnpm build
```
