#!/usr/bin/env bash
# 建立所有情境腳本共用的資源:一個倉庫、一個 WarehouseAdmin、一個 WarehouseUser、
# 一個商品單位、一個商品。跑完把 id/token 存到 .state.env,讓 01~07 腳本直接 source 用,
# 不用每支腳本都重新建一次帳號。
#
# 用法: ./scripts/00-setup.sh
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"

ADMIN_PASSWORD="1qazXSW@"
WA_PASSWORD="Passw0rd!"
WU_PASSWORD="Passw0rd!"

RUN_ID=$(date +%s)

step "用 admin 登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u admin --arg p "$ADMIN_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
ADMIN_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

WAREHOUSE_NAME="warehouse-$RUN_ID"
step "建立倉庫「${WAREHOUSE_NAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/warehouse" \
  "$(jq -n --arg n "$WAREHOUSE_NAME" '{name:$n}')" \
  "$ADMIN_TOKEN"
expect_status 200
WAREHOUSE_ID=$(echo "$CALL_BODY" | jq -r '.id')

WA_USERNAME="wa-$RUN_ID"
step "建立 WarehouseAdmin 帳號「${WA_USERNAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/auth/admin/register/user" \
  "$(jq -n --arg wid "$WAREHOUSE_ID" --arg u "$WA_USERNAME" --arg p "$WA_PASSWORD" \
      '{warehouseId:$wid, name:"倉管一號", username:$u, password:$p, role:1}')" \
  "$ADMIN_TOKEN"
expect_status 200
WA_USER_ID=$(echo "$CALL_BODY" | jq -r '.userId')

WU_USERNAME="wu-$RUN_ID"
step "建立 WarehouseUser 帳號「${WU_USERNAME}」(Organization, AdminOnly)"
call POST "$ORG_URL/api/v1/auth/admin/register/user" \
  "$(jq -n --arg wid "$WAREHOUSE_ID" --arg u "$WU_USERNAME" --arg p "$WU_PASSWORD" \
      '{warehouseId:$wid, name:"倉管一號的助手", username:$u, password:$p, role:2}')" \
  "$ADMIN_TOKEN"
expect_status 200
WU_USER_ID=$(echo "$CALL_BODY" | jq -r '.userId')

step "用 WarehouseAdmin「${WA_USERNAME}」登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WA_USERNAME" --arg p "$WA_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
WA_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

step "用 WarehouseUser「${WU_USERNAME}」登入 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WU_USERNAME" --arg p "$WU_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
WU_TOKEN=$(echo "$CALL_BODY" | jq -r '.accessToken')

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
PRODUCT_ID=$(id_from_location)
echo ">> 從 Location header 解析出 productId = $PRODUCT_ID"

save_state

echo
echo "══════════════════════════════════════════════════════════════════"
echo "setup 完成,可以開始跑 01~07 的情境腳本了。"
echo "  warehouseId = $WAREHOUSE_ID"
echo "  productId   = $PRODUCT_ID"
echo "══════════════════════════════════════════════════════════════════"
