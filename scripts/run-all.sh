#!/usr/bin/env bash
# 依序跑完全部情境:00-setup 先建好共用資源,01~07 再各自測自己負責的功能。
# 05-inbound 一定要在 06-outbound/07-stocks 之前跑,因為要先進貨才有庫存可以出貨/查詢。
#
# 用法: ./scripts/run-all.sh
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

for script in 00-setup.sh 01-auth.sh 02-warehouse.sh 03-users.sh 04-products.sh 05-inbound.sh 06-outbound.sh 07-stocks.sh; do
  echo
  echo "############################################################"
  echo "# 執行 $script"
  echo "############################################################"
  # 用 for 迴圈跑子腳本,單純呼叫不會讓外層腳本的 exit code 反映子腳本失敗 ——
  # 沒有 && 串接,子腳本 exit 1 只會讓迴圈跳到下一輪,run-all.sh 本身還是回 0。
  if ! ./"$script"; then
    echo
    echo "!! $script 失敗,整個 run-all.sh 中止。" >&2
    exit 1
  fi
done

echo
echo "全部情境腳本都跑完了。"
