# Cluster Bootstrap

`infra/terraform` apply 完之後,手動照順序把 4 台 EC2 組成 kubeadm cluster、接上 CI/CD。
之後要重跑代表節點掛了要重建,不是日常操作,所以沒有另外包裝自動化。

## 0. AWS 前置作業(第一次設定才需要)

- **用 IAM user,不要用 root 金鑰**:IAM console 建一個 user,掛 `AmazonEC2FullAccess`
  這個 managed policy 就夠(VPC/SG/EIP 這些資源在 IAM 裡都算在 `ec2:*` 底下)。
- **Region 要避開 opt-in region**:預設用 `ap-northeast-1`(東京,標準 region,不用額外開通)。
  如果要換成別的 region,先查一下是不是 opt-in region(2019 年之後開的新 region 大多是),
  是的話要先在 AWS Console 的 Account 頁面手動 Enable,不然金鑰明明是對的也會一直報
  `InvalidClientTokenId`,很難聯想到是這個原因。
- **建 EC2 key pair**,把本機既有的 SSH 公鑰匯入即可,不用另外生一把:
  ```bash
  aws ec2 import-key-pair --key-name "your-key" \
    --public-key-material fileb://~/.ssh/id_ed25519.pub \
    --region ap-northeast-1
  ```
- **填 `infra/terraform/terraform.tfvars`**(複製 `terraform.tfvars.example`):
  - `key_name`:上面建的 key pair 名稱
  - `admin_cidrs`:你自己的固定 IP(`curl -s https://checkip.amazonaws.com`),給 SSH(22)/
    kube-apiserver(6443) 用。**這個 IP 如果之後換網路會變,SSH 連不上時先檢查這裡是不是要更新
    再重新 `terraform apply`。**

## 1. terraform apply 後記錄輸出

```bash
cd infra/terraform
terraform output
```

記下 `control_plane_public_ip`、`control_plane_private_ip`、`edge_public_ip`、
`edge_public_dns`、`worker_private_ips`。

> containerd/kubeadm/kubelet/kubectl 的安裝已經包在 `templates/00-common.sh.tpl` 的
> `user_data` 裡,四台開機時會自動跑完,不用再手動 SSH 進去跑 `00-common.sh`。

## 2. control-plane 跑 `01-control-plane-init.sh`

```bash
scp 01-control-plane-init.sh ubuntu@<control-plane-public-ip>:~
ssh ubuntu@<control-plane-public-ip>
./01-control-plane-init.sh <control-plane-public-ip>
```

跑完會印出 `kubeadm join ...` 整串指令,複製起來,下一步要用。

## 3. worker + edge 跑 `02-worker-join.sh`

```bash
scp 02-worker-join.sh ubuntu@<worker-or-edge-public-ip>:~
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
scp 03-edge-setup.sh ubuntu@<control-plane-public-ip>:~
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

## 6. CI/CD 部署前置作業

- **GitHub repo secret**:Settings → Secrets and variables → Actions,加
  `ORG_RSA_PRIVATE_KEY`(`org-private.pem` 的完整內容),build organization image 時要用。
- **control-plane 上裝 `kustomize` CLI**(deploy job 的 `kustomize edit set image` 要用,
  `kubectl kustomize` 沒有 `edit` 子指令):
  ```bash
  curl -s "https://raw.githubusercontent.com/kubernetes-sigs/kustomize/master/hack/install_kustomize.sh" | bash
  sudo mv kustomize /usr/local/bin/
  ```
- **GHCR package 設成 public**:第一次 CI 跑完 push 出 4 個 image 後,去 GitHub 帳號 →
  Packages → 每個 `ims-*` package → Package settings → 改成 Public,不然 cluster 拉不到
  (或是另外設定 `imagePullSecrets`,但公開這個 demo 專案的 image 更簡單)。

## 7. 部署 app

接著照 `infra/k8s/README.md` 的步驟(`.env`、`kubectl apply -k`、HTTPS 憑證核發)。
