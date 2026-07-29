#!/usr/bin/env bash
# 情境:進貨單的建立/查詢/確認/駁回,以及狀態機、權限相關的錯誤情境。
# 依賴: ./00-setup.sh 先跑過。這支腳本會讓 setup 建的商品庫存增加,06-outbound.sh 要靠這裡
# 累積出來的庫存才能出貨,所以要在 06 之前跑。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

TS=$(date +%s%N)
INBOUND_QTY=200

step "建立進貨單 A,進貨 $INBOUND_QTY 個「${PRODUCT_NO}」,準備走確認流程 (Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound" \
  "$(jq -n --arg no "IN-A-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" --argjson qty "$INBOUND_QTY" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:$qty, unitPrice:null}]}')" \
  "$WU_TOKEN"
expect_status 201
INBOUND_A_ID=$(id_from_location)
echo ">> inboundOrderId (A) = $INBOUND_A_ID"

step "查詢進貨單 A 明細 (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/inbound/$INBOUND_A_ID" "" "$WU_TOKEN"
expect_status 200

step "列出待處理進貨單,確認 A 在清單裡 (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/inbound/pending" "" "$WA_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$INBOUND_A_ID" '.items[] | select(.id == $id)' >/dev/null \
  && echo ">> OK,待處理清單裡找到進貨單 A" \
  || { echo "!! 待處理清單裡沒找到進貨單 A,腳本中止。" >&2; exit 1; }

step "確認進貨單 A (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound/$INBOUND_A_ID/confirm" "" "$WA_TOKEN"
expect_status 200

step "等 Kafka 消費完進貨事件,確認庫存有反映出來 (Inventory, WarehouseStaffOnly)"
wait_for_stock_item "$PRODUCT_ID" "$WU_TOKEN"

step "建立進貨單 B,準備走駁回流程 (Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound" \
  "$(jq -n --arg no "IN-B-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:10, unitPrice:null}]}')" \
  "$WU_TOKEN"
expect_status 201
INBOUND_B_ID=$(id_from_location)
echo ">> inboundOrderId (B) = $INBOUND_B_ID"

step "駁回進貨單 B (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/inbound/$INBOUND_B_ID/reject" \
  "$(jq -n '{reason:"單據資訊有誤"}')" \
  "$WA_TOKEN"
expect_status 200
[[ "$(echo "$CALL_BODY" | jq -r '.status')" == "Rejected" ]] \
  && echo ">> OK,進貨單 B 狀態變成 Rejected" \
  || { echo "!! 進貨單 B 沒有變成 Rejected,腳本中止。" >&2; exit 1; }

step "查看進貨歷程(已處理清單),應該同時看到 A(Confirmed)跟 B(Rejected) (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/inbound/done" "" "$WA_TOKEN"
expect_status 200

step "★負向測試★ 用 WarehouseAdmin(而不是 WarehouseUser)建立進貨單應該回 403"
call POST "$ORDERING_URL/api/v1/orders/inbound" \
  "$(jq -n --arg no "IN-should-not-exist-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:1, unitPrice:null}]}')" \
  "$WA_TOKEN"
expect_status 403

step "★負向測試★ 進貨單 A 已經是 Confirmed,再駁回一次應該回 400"
call POST "$ORDERING_URL/api/v1/orders/inbound/$INBOUND_A_ID/reject" \
  "$(jq -n '{reason:"再試一次"}')" \
  "$WA_TOKEN"
expect_status 400

step "★負向測試★ 查詢不存在的進貨單應該回 404"
call GET "$ORDERING_URL/api/v1/orders/inbound/00000000-0000-0000-0000-000000000000" "" "$WU_TOKEN"
expect_status 404

step "★負向測試★ 沒帶 token 建立進貨單應該回 401"
call POST "$ORDERING_URL/api/v1/orders/inbound" "$(jq -n '{orderNo:"anon", items:[]}')"
expect_status 401

echo
echo "══════════════════════════════════════════════════════════════════"
echo "05-inbound 情境跑完了。"
echo "  inboundId(A, Confirmed) = $INBOUND_A_ID"
echo "  inboundId(B, Rejected)  = $INBOUND_B_ID"
echo "══════════════════════════════════════════════════════════════════"
