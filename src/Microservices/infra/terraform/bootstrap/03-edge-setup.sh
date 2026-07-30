#!/usr/bin/env bash
set -euo pipefail

# 在 control-plane 上跑(要有 kubectl 存取權),且 edge node 已經 join 進 cluster。
# 用法: ./03-edge-setup.sh <edge-node-name>
# node 名稱用 `kubectl get nodes` 查,預設會是 edge VM 的 hostname。

EDGE_NODE="${1:?請帶入 edge node 的名稱(用 kubectl get nodes 查)}"

# label 給 infra/k8s 裡的 nodeSelector 用;taint 擋掉一般 workload 排程到 edge node。
kubectl label node "${EDGE_NODE}" ims-role=ingress --overwrite
kubectl taint node "${EDGE_NODE}" dedicated=ingress:NoSchedule --overwrite

kubectl get nodes -o wide