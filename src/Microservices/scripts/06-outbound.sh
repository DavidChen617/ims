#!/usr/bin/env bash
# 情境:出貨單的建立/查詢/確認/駁回,含庫存不足自動駁回、狀態機、權限相關的錯誤情境。
# 依賴: ./00-setup.sh 跟 ./05-inbound.sh 先跑過(要有庫存才能出貨)。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

TS=$(date +%s%N)

step "建立出貨單 A,出貨 20 個「${PRODUCT_NO}」,準備走確認流程 (Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound" \
  "$(jq -n --arg no "OUT-A-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:20}]}')" \
  "$WU_TOKEN"
expect_status 201
OUTBOUND_A_ID=$(id_from_location)
echo ">> outboundOrderId (A) = $OUTBOUND_A_ID"

step "查詢出貨單 A 明細 (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_A_ID" "" "$WU_TOKEN"
expect_status 200

step "★負向測試★ 出貨單剛建立時狀態還是 Processing,這時候就確認應該回 400"
call POST "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_A_ID/confirm" "" "$WA_TOKEN"
expect_status 400

step "等 Kafka 完成庫存預留,狀態應該變成 Pending (Ordering)"
wait_for_outbound_status "$OUTBOUND_A_ID" "$WA_TOKEN"
[[ "$(echo "$CALL_BODY" | jq -r '.status')" == "Pending" ]] \
  && echo ">> OK,庫存足夠,預留成功變成 Pending" \
  || { echo "!! 預期變成 Pending,腳本中止。" >&2; exit 1; }

step "列出待處理出貨單,確認 A 在清單裡 (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/pending" "" "$WA_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$OUTBOUND_A_ID" '.items[] | select(.id == $id)' >/dev/null \
  && echo ">> OK,待處理清單裡找到出貨單 A" \
  || { echo "!! 待處理清單裡沒找到出貨單 A,腳本中止。" >&2; exit 1; }

step "確認出貨單 A (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_A_ID/confirm" "" "$WA_TOKEN"
expect_status 200

step "建立出貨單 B,故意要求超過庫存的數量,驗證系統會自動駁回 (Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound" \
  "$(jq -n --arg no "OUT-B-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:999999999}]}')" \
  "$WU_TOKEN"
expect_status 201
OUTBOUND_B_ID=$(id_from_location)
echo ">> outboundOrderId (B) = $OUTBOUND_B_ID"

step "等 Kafka 處理完,庫存不足應該自動變成 Rejected (Ordering)"
wait_for_outbound_status "$OUTBOUND_B_ID" "$WA_TOKEN"
[[ "$(echo "$CALL_BODY" | jq -r '.status')" == "Rejected" ]] \
  && echo ">> OK,庫存不足,系統自動駁回變成 Rejected" \
  || { echo "!! 預期變成 Rejected,腳本中止。" >&2; exit 1; }

step "建立出貨單 C,準備走『人工駁回』流程(而不是庫存不足自動駁回) (Ordering, WarehouseUserOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound" \
  "$(jq -n --arg no "OUT-C-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:1}]}')" \
  "$WU_TOKEN"
expect_status 201
OUTBOUND_C_ID=$(id_from_location)
echo ">> outboundOrderId (C) = $OUTBOUND_C_ID"

step "等出貨單 C 預留成功變成 Pending,才能人工駁回 (Ordering)"
wait_for_outbound_status "$OUTBOUND_C_ID" "$WA_TOKEN"

step "人工駁回出貨單 C (Ordering, WarehouseAdminOnly)"
call POST "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_C_ID/reject" \
  "$(jq -n '{reason:"客戶取消訂單"}')" \
  "$WA_TOKEN"
expect_status 200
[[ "$(echo "$CALL_BODY" | jq -r '.status')" == "Rejected" ]] \
  && echo ">> OK,出貨單 C 狀態變成 Rejected" \
  || { echo "!! 出貨單 C 沒有變成 Rejected,腳本中止。" >&2; exit 1; }

step "查看出貨歷程(Admin 全域視角),應該看得到 A/B/C (Ordering, AdminOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/history" "" "$ADMIN_TOKEN"
expect_status 200

step "查看出貨歷程(倉庫員工自助視角) (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/history/warehouse?status=Rejected" "" "$WA_TOKEN"
expect_status 200

step "查詢待出貨(已預留但未確認)數量統計 —— Admin 全域視角 (Ordering, AdminOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/pending-quantities?productId=$PRODUCT_ID" "" "$ADMIN_TOKEN"
expect_status 200

step "查詢待出貨數量統計 —— 倉庫員工自助視角 (Ordering, WarehouseStaffOnly)"
call GET "$ORDERING_URL/api/v1/orders/outbound/pending-quantities/warehouse?productId=$PRODUCT_ID" "" "$WU_TOKEN"
expect_status 200

step "★負向測試★ 用 WarehouseAdmin(而不是 WarehouseUser)建立出貨單應該回 403"
call POST "$ORDERING_URL/api/v1/orders/outbound" \
  "$(jq -n --arg no "OUT-should-not-exist-$TS" --arg pid "$PRODUCT_ID" --arg pno "$PRODUCT_NO" \
      '{orderNo:$no, items:[{productId:$pid, productNo:$pno, quantity:1}]}')" \
  "$WA_TOKEN"
expect_status 403

step "★負向測試★ 出貨單 C 已經是 Rejected,再駁回一次應該回 400"
call POST "$ORDERING_URL/api/v1/orders/outbound/$OUTBOUND_C_ID/reject" \
  "$(jq -n '{reason:"再試一次"}')" \
  "$WA_TOKEN"
expect_status 400

step "★負向測試★ 查詢不存在的出貨單應該回 404"
call GET "$ORDERING_URL/api/v1/orders/outbound/00000000-0000-0000-0000-000000000000" "" "$WU_TOKEN"
expect_status 404

step "★負向測試★ 沒帶 token 建立出貨單應該回 401"
call POST "$ORDERING_URL/api/v1/orders/outbound" "$(jq -n '{orderNo:"anon", items:[]}')"
expect_status 401

echo
echo "══════════════════════════════════════════════════════════════════"
echo "06-outbound 情境跑完了。"
echo "  outboundId(A, Confirmed)          = $OUTBOUND_A_ID"
echo "  outboundId(B, 庫存不足自動Rejected) = $OUTBOUND_B_ID"
echo "  outboundId(C, 人工Rejected)         = $OUTBOUND_C_ID"
echo "══════════════════════════════════════════════════════════════════"
