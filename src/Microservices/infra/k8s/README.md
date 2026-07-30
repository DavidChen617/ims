# IMS Kubernetes Manifests

這些 YAML 是從根目錄 `docker-compose.yml` 轉換而來，放在 `ims` namespace。

## Images

Compose 內的 `organization`、`ordering`、`inventory`、`nginx` 使用 `build:`，Kubernetes 需要先有可拉取或已載入叢集的 image。預設 manifest 使用：

- `ims/organization:latest`
- `ims/ordering:latest`
- `ims/inventory:latest`
- `ims/web:latest`

可以用 kustomize 覆寫 image：

```bash
kubectl kustomize infra/k8s | sed 's#ims/organization:latest#your-registry/ims-organization:tag#g' | kubectl apply -f -
```

## Deploy

把部署環境要用的 `.env` 放到 `infra/k8s/.env`：

```bash
cp .env infra/k8s/.env
```

或在雲端主機上：

```bash
scp .env user@host:/path/to/Microservices/infra/k8s/.env
```

Kustomize 會用 `infra/k8s/.env` 產生 `ims-env` Secret，所有原本 Compose 從 `.env` 注入的值都會從這個 Secret 讀取。

把 public 入口 VM 對應的 Kubernetes node 標記為 ingress node：

```bash
kubectl label node <entry-node-name> ims-role=ingress
```

Migration 用的 ConfigMap 不是 Kustomize 管的（kustomize 的 `configMapGenerator` 不支援整個目錄／glob，硬要手動列每個檔名等於沒解決問題），改成從實際的 `Migrations` 目錄動態產生，`kubectl apply -k` 之前要先跑：

```bash
kubectl create namespace ims --dry-run=client -o yaml | kubectl apply -f -
kubectl create configmap organization-migrations -n ims --from-file=../../src/Organization/Migrations --dry-run=client -o yaml | kubectl apply -f -
kubectl create configmap ordering-migrations -n ims --from-file=../../src/Ordering/Migrations --dry-run=client -o yaml | kubectl apply -f -
kubectl create configmap inventory-migrations -n ims --from-file=../../src/Inventory/Migrations --dry-run=client -o yaml | kubectl apply -f -
```

（CI 的 `deploy` job 已經自動做這件事；這裡是給手動部署／debug 用，執行位置要在 repo 裡跑，讓上面的相對路徑對得上實際的 Migrations 目錄。）

```bash
kubectl apply -k infra/k8s
kubectl -n ims get pods
```

第一次部署時，PostgreSQL 會透過 init script 建立三個 database，三個 migration Job 會套用 SQL migration。API Pod 會等到對應 schema 存在後才啟動。

## HTTPS（Let's Encrypt）

`nginx` Pod 用 `hostPort: 80/443`，透過 `nodeSelector` 固定排到標記為 `ims-role=ingress` 的入口 node，443 走 TLS，80 只做 acme-challenge 跟導向 443。

`.env` 除了原本的變數，還要補：

```
EDGE_PUBLIC_DNS=  # 入口 node 的 AWS 公有 DNS 名稱，例如 ec2-x-x-x-x.<region>.compute.amazonaws.com
LETSENCRYPT_EMAIL=
```

第一次核發憑證前，nginx 的 443 server block 需要一份佔位憑證才能啟動，先在入口 VM 上：

```bash
sudo mkdir -p /etc/letsencrypt/live/ims-edge
sudo openssl req -x509 -nodes -newkey rsa:2048 -days 1 \
  -keyout /etc/letsencrypt/live/ims-edge/privkey.pem \
  -out /etc/letsencrypt/live/ims-edge/fullchain.pem \
  -subj "/CN=localhost"
```

`kubectl apply -k infra/k8s` 起完後，跑一次性的核發 Job（見 `certbot-init-job.yaml` 檔頭註解，跑之前先確認 Docker Hub 上 `certbot/certbot` 目前的 `arm64v8-*` tag）：

```bash
kubectl apply -f infra/k8s/certbot-init-job.yaml
kubectl -n ims logs -f job/certbot-init
kubectl -n ims rollout restart deployment/nginx
kubectl delete job/certbot-init -n ims
```

之後續期由 `certbot-renew` CronJob（每週一 03:00）自動處理，續期完會重啟 `nginx` Deployment 讓它讀到新憑證——因為是無條件重啟、單一 replica，重啟當下會有幾秒中斷，這是先求簡單的取捨，在意的話可以再改成比對憑證檔案再決定要不要重啟。

```bash
https://<EDGE_PUBLIC_DNS>
```

Jaeger UI 可用 port-forward：

```bash
kubectl -n ims port-forward svc/jaeger 16686:16686
```

## Notes

- `infra/k8s/.env` 被 `.gitignore` 排除，不會跟著 manifests 提交。
- Kustomize 產生的 Secret 只是 Kubernetes Secret，不等於雲端 KMS 加密；正式環境可再替換成外部 secret 管理。
- K8s 內部服務會用 `.env` 的 `KAFKA_BOOTSTRAP_SERVERS=kafka:9092` 連 Kafka；若你改 `.env`，請保留叢集內可解析的 `kafka:9092` listener。
- PostgreSQL/Kafka 的 PVC 用 `storage.yaml` 裡靜態綁定的 local PV（`storageClassName: local-storage`），資料固定放在 `ims-worker-1`/`ims-worker-2` 的本地磁碟，套用前記得照 `infra/terraform/bootstrap/README.md` 先在對應 node 建好目錄。
