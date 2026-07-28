#!/usr/bin/env bash
# 走一次「正流程」的端到端 smoke test:
# Organization(建倉庫/建帳號) -> Ordering(建商品/進貨/出貨) -> Inventory(驗證庫存)
#
# 每一步都會印出實際打出去的 request(method/url/body)跟收到的 response
# (status/body/Location header),方便人工核對,取代在 Insomnia 裡一個一個點。
#
# 用法: ./scripts/happy-path-smoke-test.sh
# 可用環境變數覆蓋預設的服務位址:
#   ORG_URL=http://localhost:5032 ORDERING_URL=http://localhost:5116 INVENTORY_URL=http://localhost:5205
set -uo pipefail

ORG_URL="${ORG_URL:-http://localhost:5032}"
ORDERING_URL="${ORDERING_URL:-http://localhost:5116}"
INVENTORY_URL="${INVENTORY_URL:-http://localhost:5205}"

# 用 timestamp 當 suffix,warehouse name / username / product no / unit name
# 都有唯一性限制,這樣每次重跑腳本不會撞到上一次留下的資料。
RUN_ID=$(date +%s)

ADMIN_PASSWORD="1qazXSW@"
WAREHOUSE_ADMIN_PASSWORD="Passw0rd!"
WAREHOUSE_USER_PASSWORD="Passw0rd!"

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
# 執行後可用的全域變數: CALL_STATUS / CALL_BODY / CALL_LOCATION
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
}

id_from_location() {
  echo "$CALL_LOCATION" | awk -F'/' '{print $NF}'
}

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
    echo ">> 還查不到這個 productId 的庫存,等 Kafka 消費 inbound 事件,重試中 ($i/$tries)..."
    sleep 1
  done
  echo "!! 等太久,Inventory 一直沒消費到 inbound 事件,腳本中止。" >&2
  exit 1
}

# ── 1. 用預先種好的 admin 登入 (Organization) ─────────────────────────────
step "用 admin 登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u admin --arg p "$ADMIN_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
ADMIN_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

# ── 2. 建一個倉庫 (Organization, admin) ───────────────────────────────────
WAREHOUSE_NAME="warehouse-$RUN_ID"
step "建立倉庫「${WAREHOUSE_NAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/warehouse" \
  "$(jq -n --arg n "$WAREHOUSE_NAME" '{name:$n}')" \
  "$ADMIN_TOKEN"
expect_status 200
WAREHOUSE_ID=$(echo "$CALL_BODY" | jq -r '.id')

# ── 3. 建立這個倉庫的 WarehouseAdmin + WarehouseUser (Organization, admin) ─
WA_USERNAME="wa-$RUN_ID"
step "建立 WarehouseAdmin 帳號「${WA_USERNAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/auth/admin/register/user" \
  "$(jq -n --arg wid "$WAREHOUSE_ID" --arg u "$WA_USERNAME" --arg p "$WAREHOUSE_ADMIN_PASSWORD" \
      '{warehouseId:$wid, name:"倉管一號", username:$u, password:$p, role:1}')" \
  "$ADMIN_TOKEN"
expect_status 200

WU_USERNAME="wu-$RUN_ID"
step "建立 WarehouseUser 帳號「${WU_USERNAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/auth/admin/register/user" \
  "$(jq -n --arg wid "$WAREHOUSE_ID" --arg u "$WU_USERNAME" --arg p "$WAREHOUSE_USER_PASSWORD" \
      '{warehouseId:$wid, name:"倉管一號的助手", username:$u, password:$p, role:2}')" \
  "$ADMIN_TOKEN"
expect_status 200

# ── 4. 分別用兩個新帳號登入,拿到帶 warehouseId claim 的 token ─────────────
step "用 WarehouseAdmin「${WA_USERNAME}」登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WA_USERNAME" --arg p "$WAREHOUSE_ADMIN_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
WA_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

step "用 WarehouseUser「${WU_USERNAME}」登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WU_USERNAME" --arg p "$WAREHOUSE_USER_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
WU_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

# ── 5. 建立商品單位 + 商品 (Ordering, WarehouseAdmin) ──────────────────────
UNIT_NAME="unit-$RUN_ID"
step "建立商品單位「${UNIT_NAME}」(Ordering, AdminOrWarehouseAdmin)"
call POST "$ORDERING_URL/api/v1/products/units" \
  "$(jq -n --arg n "$UNIT_NAME" '{name:$n}')" \
  "$WA_TOKEN"
expect_status 201

PRODUCT_NO="P-$RUN_ID"
step "建立商品「${PRODUCT_NO}」(Ordering, AdminOrWarehouseAdmin)"
call POST "$ORDERING_URL/api/v1/products" \
  "$(jq -n --arg no "$PRODUCT_NO" --arg unit "$UNIT_NAME" \
      '{productNo:$no, name:"測試商品", unit:$unit, price:9.9}')" \
  "$WA_TOKEN"
expect_status 201
# 這支 API 回空 body,新商品的 id 只會出現在 Location header 上。
PRODUCT_ID=$(id_from_location)
echo ">> 從 Location header 解析出 productId = $PRODUCT_ID"

# ── 6. 建立進貨單並確認 (Ordering) ────────────────────────────────────────
INBOUND_QTY=100
step "建立進貨單,進貨 $INBOUND_QTY 個「${PRODUCT_NO}」(Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound" \
  "$(jq -n --arg no "IN-$RUN_ID" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" --argjson qty "$INBOUND_QTY" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:$qty, unitPrice:null}]}')" \
  "$WU_TOKEN"
expect_status 201
INBOUND_ID=$(id_from_location)
echo ">> 從 Location header 解析出 inboundOrderId = $INBOUND_ID"

step "確認進貨單 (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound/$INBOUND_ID/confirm" "" "$WA_TOKEN"
expect_status 200

step "查詢倉庫庫存,確認進貨後數量增加 (Inventory, WarehouseStaffOnly)"
wait_for_stock_item "$PRODUCT_ID" "$WU_TOKEN"

# ── 7. 建立出貨單,等 Kafka 非同步預留完成後確認 (Ordering) ─────────────────
OUTBOUND_QTY=30
step "建立出貨單,出貨 $OUTBOUND_QTY 個「${PRODUCT_NO}」(Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound" \
  "$(jq -n --arg no "OUT-$RUN_ID" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" --argjson qty "$OUTBOUND_QTY" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:$qty}]}')" \
  "$WU_TOKEN"
expect_status 201
OUTBOUND_ID=$(id_from_location)
echo ">> 從 Location header 解析出 outboundOrderId = $OUTBOUND_ID"

step "等待 Inventory 透過 Kafka 完成庫存預留 (輪詢出貨單狀態)"
wait_for_outbound_status "$OUTBOUND_ID" "$WA_TOKEN"

step "確認出貨單 (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_ID/confirm" "" "$WA_TOKEN"
expect_status 200

step "查詢倉庫庫存,確認出貨後數量減少、累計出貨量增加 (Inventory, WarehouseStaffOnly)"
call GET "$INVENTORY_URL/api/v1/stocks/warehouse?productId=$PRODUCT_ID" "" "$WU_TOKEN"
expect_status 200

echo
echo "══════════════════════════════════════════════════════════════════"
echo "正流程全部跑完了。"
echo "  warehouseId = $WAREHOUSE_ID"
echo "  productId   = $PRODUCT_ID"
echo "  inboundId   = $INBOUND_ID"
echo "  outboundId  = $OUTBOUND_ID"
echo "══════════════════════════════════════════════════════════════════"
