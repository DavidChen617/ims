#!/usr/bin/env bash
set -euo pipefail

# 只在 control-plane 上跑,且已經跑過 00-common.sh。
# 用法: ./01-control-plane-init.sh <control-plane-public-ip-or-eip>
# 第一個參數會加進 apiserver 憑證的 extra SAN,之後才能從外部(你自己的電腦)用 kubectl 打這台。

PUBLIC_IP="${1:?請帶入 control-plane 的 public/Elastic IP 當第一個參數}"

TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
PRIVATE_IP=$(curl -s -H "X-aws-ec2-metadata-token: $TOKEN" http://169.254.169.254/latest/meta-data/local-ipv4)

sudo kubeadm init \
  --apiserver-advertise-address="${PRIVATE_IP}" \
  --control-plane-endpoint="${PRIVATE_IP}:6443" \
  --apiserver-cert-extra-sans="${PUBLIC_IP}" \
  --pod-network-cidr=192.168.0.0/16

mkdir -p "$HOME/.kube"
sudo cp -i /etc/kubernetes/admin.conf "$HOME/.kube/config"
sudo chown "$(id -u):$(id -g)" "$HOME/.kube/config"

# vanilla kubeadm 沒有內建 CNI,裝 Calico。
kubectl create -f https://raw.githubusercontent.com/projectcalico/calico/v3.29.3/manifests/tigera-operator.yaml

# custom-resources.yaml 用到的 Installation CRD 是上一步 tigera-operator.yaml 才建的,
# API server 要幾秒鐘才會註冊完成,不等的話 kubectl create 會直接噴 "no matches for kind"。
kubectl wait --for=condition=established --timeout=90s crd/installations.operator.tigera.io

curl -O https://raw.githubusercontent.com/projectcalico/calico/v3.29.3/manifests/custom-resources.yaml
kubectl create -f custom-resources.yaml

echo
echo "=== 等 Calico 起來後用這個確認 ==="
echo "kubectl get pods -n calico-system"
echo
echo "=== join 指令(拿去給 2 台 worker + edge node 用,整串複製) ==="
kubeadm token create --print-join-command
