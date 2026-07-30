#!/usr/bin/env bash
set -euo pipefail

# 由 Terraform user_data 在開機時自動執行,對應 infra/bootstrap/00-common.sh 的內容。
# ${hostname} 是 Terraform 帶進來的變數,其餘 $${...} 都是要逃逸給 bash 用的,不是 Terraform 變數。

sudo hostnamectl set-hostname "${hostname}"

TOKEN=$(curl -s -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600")
PRIVATE_IP=$(curl -s -H "X-aws-ec2-metadata-token: $TOKEN" http://169.254.169.254/latest/meta-data/local-ipv4)

sudo swapoff -a
sudo sed -i '/ swap /s/^/#/' /etc/fstab

cat <<'EOF' | sudo tee /etc/modules-load.d/k8s.conf
overlay
br_netfilter
EOF
sudo modprobe overlay
sudo modprobe br_netfilter

cat <<'EOF' | sudo tee /etc/sysctl.d/k8s.conf
net.bridge.bridge-nf-call-iptables  = 1
net.bridge.bridge-nf-call-ip6tables = 1
net.ipv4.ip_forward                 = 1
EOF
sudo sysctl --system

sudo apt-get update
sudo apt-get install -y containerd apt-transport-https ca-certificates curl gpg

sudo mkdir -p /etc/containerd
sudo containerd config default | sudo tee /etc/containerd/config.toml >/dev/null
sudo sed -i 's/SystemdCgroup = false/SystemdCgroup = true/' /etc/containerd/config.toml
sudo systemctl restart containerd
sudo systemctl enable containerd

sudo mkdir -p -m 755 /etc/apt/keyrings
curl -fsSL https://pkgs.k8s.io/core:/stable:/v1.36/deb/Release.key \
  | sudo gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
echo 'deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.36/deb/ /' \
  | sudo tee /etc/apt/sources.list.d/kubernetes.list

sudo apt-get update
sudo apt-get install -y kubelet kubeadm kubectl
sudo apt-mark hold kubelet kubeadm kubectl

PAUSE_IMAGE=$(kubeadm config images list --kubernetes-version stable-1.36 2>/dev/null | grep pause)
sudo sed -i "s#sandbox_image = \".*\"#sandbox_image = \"$${PAUSE_IMAGE}\"#" /etc/containerd/config.toml
sudo systemctl restart containerd

echo "KUBELET_EXTRA_ARGS=--node-ip=$${PRIVATE_IP}" | sudo tee /etc/default/kubelet
sudo systemctl daemon-reload
sudo systemctl enable --now kubelet

echo "=== done. hostname=${hostname}, private ip = $${PRIVATE_IP}, pause image = $${PAUSE_IMAGE} ===" > /var/log/00-common-done.log