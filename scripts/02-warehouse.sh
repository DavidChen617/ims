#!/usr/bin/env bash
# 情境:倉庫的建立/查詢,以及跟權限、重複名稱有關的錯誤情境。
# 依賴: ./00-setup.sh 先跑過。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

step "列出所有倉庫,確認剛建的倉庫在清單裡 (Organization, AdminOnly)"
call GET "$ORG_URL/api/v1/warehouse" "" "$ADMIN_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$WAREHOUSE_ID" '.items[] | select(.id == $id)' >/dev/null \
  && echo ">> OK,清單裡找到 warehouseId=$WAREHOUSE_ID" \
  || { echo "!! 清單裡沒找到剛建的倉庫,腳本中止。" >&2; exit 1; }

step "查詢單一倉庫明細,確認 WarehouseAdmin/WarehouseUser 都在裡面 (Organization, AdminOnly)"
call GET "$ORG_URL/api/v1/warehouse/$WAREHOUSE_ID" "" "$ADMIN_TOKEN"
expect_status 200

step "★負向測試★ 建立同名倉庫應該回 400"
call POST "$ORG_URL/api/v1/warehouse" \
  "$(jq -n --arg n "$WAREHOUSE_NAME" '{name:$n}')" \
  "$ADMIN_TOKEN"
expect_status 400

step "★負向測試★ 用 WarehouseAdmin(非 Admin)身份建立倉庫應該回 403"
call POST "$ORG_URL/api/v1/warehouse" \
  "$(jq -n --arg n "warehouse-should-not-be-created-$RUN_ID" '{name:$n}')" \
  "$WA_TOKEN"
expect_status 403

step "★負向測試★ 查詢不存在的倉庫應該回 404"
call GET "$ORG_URL/api/v1/warehouse/00000000-0000-0000-0000-000000000000" "" "$ADMIN_TOKEN"
expect_status 404

echo
echo "══════════════════════════════════════════════════════════════════"
echo "02-warehouse 情境跑完了。"
echo "══════════════════════════════════════════════════════════════════"
