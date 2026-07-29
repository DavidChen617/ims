#!/usr/bin/env bash
# 情境:查詢庫存(Admin 全域視角 vs 倉庫員工自助視角),含篩選條件跟權限相關的錯誤情境。
# 依賴: ./00-setup.sh 跟 ./05-inbound.sh 先跑過(要有庫存資料才能查)。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

step "Admin 查詢全部倉庫的庫存,用 productId 篩選 (Inventory, AdminOnly)"
call GET "$INVENTORY_URL/api/v1/stocks?productId=$PRODUCT_ID" "" "$ADMIN_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$PRODUCT_ID" '.items[] | select(.productId == $id)' >/dev/null \
  && echo ">> OK,查得到剛剛進出貨的商品庫存。" \
  || { echo "!! 查不到預期的庫存項目,腳本中止。" >&2; exit 1; }

step "Admin 查詢全部倉庫的庫存,用 productNo 模糊篩選 (Inventory, AdminOnly)"
call GET "$INVENTORY_URL/api/v1/stocks?productNo=$PRODUCT_NO" "" "$ADMIN_TOKEN"
expect_status 200

step "WarehouseUser 自助查詢自己倉庫的庫存 (Inventory, WarehouseStaffOnly)"
call GET "$INVENTORY_URL/api/v1/stocks/warehouse?productId=$PRODUCT_ID" "" "$WU_TOKEN"
expect_status 200

step "WarehouseAdmin 也可以用同一支自助查詢 API (Inventory, WarehouseStaffOnly)"
call GET "$INVENTORY_URL/api/v1/stocks/warehouse?productName=$(urlencode "測試商品")" "" "$WA_TOKEN"
expect_status 200

step "★負向測試★ WarehouseUser 身份呼叫『全倉庫』這支管理端 API 應該回 403"
call GET "$INVENTORY_URL/api/v1/stocks?productId=$PRODUCT_ID" "" "$WU_TOKEN"
expect_status 403

step "★負向測試★ 沒帶 token 應該回 401"
call GET "$INVENTORY_URL/api/v1/stocks/warehouse"
expect_status 401

echo
echo "══════════════════════════════════════════════════════════════════"
echo "07-stocks 情境跑完了。"
echo "══════════════════════════════════════════════════════════════════"
