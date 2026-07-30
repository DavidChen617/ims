# Cluster Bootstrap

`infra/terraform` apply 完之後,手動照順序跑這幾支腳本把 4 台 EC2 組成 kubeadm cluster。
之後要重跑代表節點掛了要重建,不是日常操作,所以沒有另外包裝自動化。

## 0. 準備

```bash
cd infra/terraform
terraform output
```

記下 `control_plane_public_ip`、`control_plane_private_ip`、`edge_public_ip`、
`edge_public_dns`、`worker_private_ips`。

## 1. 四台都跑 `00-common.sh`

```bash
scp 00-common.sh ubuntu@<node-public-ip>:~
ssh ubuntu@<node-public-ip> 'bash 00-common.sh <hostname>'
```

`<hostname>` 依序帶 `ims-control-plane`、`ims-edge`、`ims-worker-1`、`ims-worker-2`——
要跟 terraform 的 Name tag 一致,`infra/k8s/storage.yaml` 的 local PV 是用 hostname 指定死是哪台。

## 2. control-plane 跑 `01-control-plane-init.sh`

```bash
ssh ubuntu@<control-plane-public-ip>
./01-control-plane-init.sh <control-plane-public-ip>
```

跑完會印出 `kubeadm join ...` 整串指令,複製起來,下一步要用。

## 3. worker + edge 跑 `02-worker-join.sh`

```bash
ssh ubuntu@<worker-or-edge-public-ip>
sudo ./02-worker-join.sh 'kubeadm join 10.0.1.x:6443 --token ... --discovery-token-ca-cert-hash sha256:...'
```

2 台 worker + edge node 都要跑,貼上一模一樣那串 join 指令。

## 4. control-plane 跑 `03-edge-setup.sh`

先確認 edge node 的名稱:

```bash
kubectl get nodes
```

```bash
./03-edge-setup.sh <edge-node-name>
```

幫 edge node 打上 `ims-role=ingress` label 跟 `dedicated=ingress:NoSchedule` taint——
label 給 `infra/k8s` 裡 nginx/certbot 的 `nodeSelector` 用,taint 擋掉一般 workload 排到這台。

## 5. 在 worker 上建 local PV 要用的目錄

`infra/k8s/storage.yaml` 用 local PV 靜態綁定,不裝 provisioner,但目錄要自己先建好:

```bash
ssh ubuntu@<ims-worker-1-public-ip> 'sudo mkdir -p /mnt/data/postgres && sudo chmod 777 /mnt/data/postgres'
ssh ubuntu@<ims-worker-2-public-ip> 'sudo mkdir -p /mnt/data/kafka && sudo chmod 777 /mnt/data/kafka'
```

## 6. 部署 app

接著照 `infra/k8s/README.md` 的步驟(`.env`、`kubectl apply -k`、HTTPS 憑證核發)。