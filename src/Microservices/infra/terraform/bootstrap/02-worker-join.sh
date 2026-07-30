#!/usr/bin/env bash
set -euo pipefail

# 在 2 台 worker + edge node 上跑,且已經跑過 00-common.sh。
# 用法: sudo ./02-worker-join.sh 'kubeadm join 10.0.1.x:6443 --token ... --discovery-token-ca-cert-hash sha256:...'
# (參數是 01-control-plane-init.sh 印出來的整串 join 指令,記得加引號)

JOIN_CMD="${1:?請把 control-plane 印出來的完整 kubeadm join 指令當參數}"

eval "sudo ${JOIN_CMD}"