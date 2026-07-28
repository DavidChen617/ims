#!/usr/bin/env bash
# 情境:查詢人員清單(admin 全域視角 vs 倉庫員工自助查自己倉庫的視角)。
# 依賴: ./00-setup.sh 先跑過。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

step "Admin 查看全部人員 (Organization, AdminOrWarehouseAdminOnly)"
call GET "$ORG_URL/api/v1/users" "" "$ADMIN_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$WU_USER_ID" '.items[] | select(.id == $id)' >/dev/null \
  && echo ">> OK,清單裡找到 WarehouseUser userId=$WU_USER_ID" \
  || { echo "!! 清單裡沒找到剛建的 WarehouseUser,腳本中止。" >&2; exit 1; }

step "WarehouseAdmin 查看『自己倉庫』的人員清單 (Organization, WarehouseStaffOnly)"
call GET "$ORG_URL/api/v1/users/warehouse" "" "$WA_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg id "$WU_USER_ID" '.items[] | select(.id == $id)' >/dev/null \
  && echo ">> OK,自助查詢也看得到同倉庫的 WarehouseUser。" \
  || { echo "!! 自助查詢清單裡沒看到同倉庫的 WarehouseUser,腳本中止。" >&2; exit 1; }

step "WarehouseUser 也可以用同一支自助查詢 API 看自己倉庫的同事 (Organization, WarehouseStaffOnly)"
call GET "$ORG_URL/api/v1/users/warehouse" "" "$WU_TOKEN"
expect_status 200

step "★負向測試★ WarehouseUser 身份呼叫『全部人員』這支管理端 API 應該回 403"
call GET "$ORG_URL/api/v1/users" "" "$WU_TOKEN"
expect_status 403

step "★負向測試★ 沒帶 token 應該回 401"
call GET "$ORG_URL/api/v1/users/warehouse"
expect_status 401

echo
echo "══════════════════════════════════════════════════════════════════"
echo "03-users 情境跑完了。"
echo "══════════════════════════════════════════════════════════════════"
