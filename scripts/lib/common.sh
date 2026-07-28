#!/usr/bin/env bash
# 所有情境腳本共用的 helper —— 在每支腳本開頭 `source "$(dirname "$0")/lib/common.sh"`。
#
# 提供:
#   call METHOD URL [JSON_BODY] [BEARER_TOKEN]   打一支 API,印出 request/response
#   expect_status CODE                            斷言剛剛 call() 的狀態碼,不符就中止
#   expect_status_in CODE...                       斷言狀態碼落在允許清單裡
#   id_from_location                               從 Location header 解析出最後一段路徑當 id
#   wait_for_outbound_status ID TOKEN [FINAL...]   輪詢出貨單狀態直到離開 Processing
#   wait_for_stock_item PRODUCT_ID TOKEN            輪詢 /stocks/warehouse 直到查得到該商品
#   load_state / save_state                        讀寫 00-setup.sh 產生的共用狀態
#
# 全域變數(call() 執行後可用): CALL_STATUS / CALL_BODY / CALL_LOCATION

set -uo pipefail

ORG_URL="${ORG_URL:-http://localhost:5032}"
ORDERING_URL="${ORDERING_URL:-http://localhost:5116}"
INVENTORY_URL="${INVENTORY_URL:-http://localhost:5205}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STATE_FILE="$SCRIPT_DIR/.state.env"

TMP_DIR=$(mktemp -d)
trap 'rm -rf "$TMP_DIR"' EXIT

STEP=0

step() {
  STEP=$((STEP + 1))
  echo
  echo "══════════════════════════════════════════════════════════════════"
  echo "[$STEP] $1"
  echo "══════════════════════════════════════════════════════════════════"
}

# 用法: call METHOD URL [JSON_BODY] [BEARER_TOKEN]
call() {
  local method="$1" url="$2" body="${3:-}" token="${4:-}"
  local -a curl_args=(-sS -D "$TMP_DIR/headers" -o "$TMP_DIR/body" -w '%{http_code}' -X "$method" "$url" -H "Content-Type: application/json")

  echo "--- 請求 ---"
  echo "$method $url"
  if [[ -n "$token" ]]; then
    curl_args+=(-H "Authorization: Bearer $token")
    echo "Authorization: Bearer ${token:0:24}...(略)"
  fi
  if [[ -n "$body" ]]; then
    curl_args+=(-d "$body")
    echo "$body" | jq . 2>/dev/null || echo "$body"
  fi

  CALL_STATUS=$(curl "${curl_args[@]}")
  CALL_BODY=$(cat "$TMP_DIR/body")
  CALL_LOCATION=$(grep -i '^location:' "$TMP_DIR/headers" | tr -d '\r' | awk '{print $2}')

  echo
  echo "--- 回應 ---"
  echo "HTTP $CALL_STATUS"
  [[ -n "$CALL_LOCATION" ]] && echo "Location: $CALL_LOCATION"
  if [[ -n "$CALL_BODY" ]]; then
    echo "$CALL_BODY" | jq . 2>/dev/null || echo "$CALL_BODY"
  fi
  echo
}

expect_status() {
  local expected="$1"
  if [[ "$CALL_STATUS" != "$expected" ]]; then
    echo "!! 預期 HTTP $expected,實際拿到 HTTP $CALL_STATUS,腳本中止。" >&2
    exit 1
  fi
  echo ">> OK,狀態碼符合預期 ($expected)。"
}

# 用法: expect_status_in 200 201  ── 只要是其中一個就算過
expect_status_in() {
  local s
  for s in "$@"; do
    [[ "$CALL_STATUS" == "$s" ]] && { echo ">> OK,狀態碼落在預期範圍內 ($*)。"; return 0; }
  done
  echo "!! 預期狀態碼落在 ($*) 之一,實際拿到 HTTP $CALL_STATUS,腳本中止。" >&2
  exit 1
}

id_from_location() {
  echo "$CALL_LOCATION" | awk -F'/' '{print $NF}'
}

# query string 裡有中文字/特殊字元時要用這個包一下,否則 curl 會把原始 UTF-8 bytes
# 直接塞進 URL,Kestrel 收到未跳脫的 query string 會直接回 400(甚至比認證檢查更早發生)。
urlencode() {
  jq -rn --arg v "$1" '$v | @uri'
}

# 出貨單建立後靠 Kafka 非同步預留庫存,狀態會先卡在 Processing,
# 要輪詢到變成別的狀態(Pending=預留成功、Rejected=庫存不足)才能往下操作。
wait_for_outbound_status() {
  local order_id="$1" token="$2" tries=20
  local status
  for ((i = 1; i <= tries; i++)); do
    call GET "$ORDERING_URL/api/v1/orders/outbound/$order_id" "" "$token"
    expect_status 200
    status=$(echo "$CALL_BODY" | jq -r '.status')
    if [[ "$status" != "Processing" ]]; then
      echo ">> Kafka 非同步預留已處理完,目前狀態: $status"
      return 0
    fi
    echo ">> 狀態還是 Processing,等 Kafka 消費 outbound 事件,重試中 ($i/$tries)..."
    sleep 1
  done
  echo "!! 等太久,outbound 訂單狀態一直卡在 Processing,腳本中止。" >&2
  exit 1
}

# 進貨的庫存增加一樣是靠 Kafka 非同步事件觸發,查詢時機太早的話 stocks 表可能還沒
# 被 Inventory 那邊的 consumer 寫進去,所以改成輪詢直到查得到那個 productId 為止。
wait_for_stock_item() {
  local product_id="$1" token="$2" tries=20
  for ((i = 1; i <= tries; i++)); do
    call GET "$INVENTORY_URL/api/v1/stocks/warehouse?productId=$product_id" "" "$token"
    expect_status 200
    if [[ "$(echo "$CALL_BODY" | jq '.items | length')" -gt 0 ]]; then
      return 0
    fi
    echo ">> 還查不到這個 productId 的庫存,等 Kafka 消費事件,重試中 ($i/$tries)..."
    sleep 1
  done
  echo "!! 等太久,Inventory 一直沒消費到事件,腳本中止。" >&2
  exit 1
}

save_state() {
  {
    echo "# 由 00-setup.sh 產生,其他情境腳本 source 這份檔案取得共用的帳號/資源。"
    echo "export RUN_ID='$RUN_ID'"
    echo "export ADMIN_TOKEN='$ADMIN_TOKEN'"
    echo "export WAREHOUSE_ID='$WAREHOUSE_ID'"
    echo "export WAREHOUSE_NAME='$WAREHOUSE_NAME'"
    echo "export WA_USERNAME='$WA_USERNAME'"
    echo "export WA_USER_ID='$WA_USER_ID'"
    echo "export WA_TOKEN='$WA_TOKEN'"
    echo "export WU_USERNAME='$WU_USERNAME'"
    echo "export WU_USER_ID='$WU_USER_ID'"
    echo "export WU_TOKEN='$WU_TOKEN'"
    echo "export UNIT_NAME='$UNIT_NAME'"
    echo "export PRODUCT_NO='$PRODUCT_NO'"
    echo "export PRODUCT_ID='$PRODUCT_ID'"
  } > "$STATE_FILE"
  echo ">> 狀態已寫入 $STATE_FILE"
}

load_state() {
  if [[ ! -f "$STATE_FILE" ]]; then
    echo "!! 找不到 $STATE_FILE,請先執行 ./00-setup.sh。" >&2
    exit 1
  fi
  # shellcheck source=/dev/null
  source "$STATE_FILE"
}
