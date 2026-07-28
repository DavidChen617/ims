#!/usr/bin/env bash
# 情境:商品/商品單位的建立、查詢、刪除,以及重複建立、找不到、刪除中使用的單位等錯誤情境。
# 依賴: ./00-setup.sh 先跑過。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

TS=$(date +%s%N)

step "列出所有商品單位,確認 setup 建的單位在裡面 (Ordering, AnyWarehouseRole)"
call GET "$ORDERING_URL/api/v1/products/units" "" "$WU_TOKEN"
expect_status 200
echo "$CALL_BODY" | jq -e --arg n "$UNIT_NAME" '.items[] | select(.name == $n)' >/dev/null \
  && echo ">> OK,清單裡找到單位「${UNIT_NAME}」" \
  || { echo "!! 清單裡沒找到剛建的單位,腳本中止。" >&2; exit 1; }

step "列出所有商品,分頁查詢 (Ordering, AnyWarehouseRole)"
call GET "$ORDERING_URL/api/v1/products?page=1&size=20" "" "$WU_TOKEN"
expect_status 200

step "查詢單一商品明細 (Ordering, AnyWarehouseRole)"
call GET "$ORDERING_URL/api/v1/products/$PRODUCT_ID" "" "$WU_TOKEN"
expect_status 200

step "★負向測試★ 重複建立同名商品單位應該回 409"
call POST "$ORDERING_URL/api/v1/products/units" \
  "$(jq -n --arg n "$UNIT_NAME" '{name:$n}')" \
  "$WA_TOKEN"
expect_status 409

step "★負向測試★ 重複建立同 productNo 的商品應該回 409"
call POST "$ORDERING_URL/api/v1/products" \
  "$(jq -n --arg no "$PRODUCT_NO" --arg unit "$UNIT_NAME" '{productNo:$no, name:"重複商品", unit:$unit, price:1}')" \
  "$WA_TOKEN"
expect_status 409

step "★負向測試★ 建立商品時填一個不存在的單位名稱,應該回 404(這個 404 沒寫在 API 文件的 .Produces() 上,是隱藏行為)"
call POST "$ORDERING_URL/api/v1/products" \
  "$(jq -n --arg no "P-should-not-exist-$TS" --arg unit "no-such-unit-$TS" '{productNo:$no, name:"測試", unit:$unit, price:1}')" \
  "$WA_TOKEN"
expect_status 404

step "★負向測試★ 查詢不存在的商品應該回 404"
call GET "$ORDERING_URL/api/v1/products/00000000-0000-0000-0000-000000000000" "" "$WU_TOKEN"
expect_status 404

step "★負向測試★ 沒帶 token 建立商品單位應該回 401"
call POST "$ORDERING_URL/api/v1/products/units" "$(jq -n '{name:"anonymous-unit"}')"
expect_status 401

step "★負向測試★ WarehouseUser(非 Admin/WarehouseAdmin)建立商品單位應該回 403"
call POST "$ORDERING_URL/api/v1/products/units" \
  "$(jq -n --arg n "extra-unit-$TS" '{name:$n}')" \
  "$WU_TOKEN"
expect_status 403

EXTRA_UNIT="extra-unit-$TS"
step "建立一個沒被任何商品用到的單位「${EXTRA_UNIT}」,準備測刪除 (Ordering, AdminOrWarehouseAdmin)"
call POST "$ORDERING_URL/api/v1/products/units" \
  "$(jq -n --arg n "$EXTRA_UNIT" '{name:$n}')" \
  "$WA_TOKEN"
expect_status 201

step "刪除沒被使用的單位應該成功 (Ordering, AdminOrWarehouseAdmin)"
call DELETE "$ORDERING_URL/api/v1/products/units/$EXTRA_UNIT" "" "$WA_TOKEN"
expect_status 204

step "★負向測試★ 刪除還在被商品「${PRODUCT_NO}」使用中的單位「${UNIT_NAME}」應該回 409"
call DELETE "$ORDERING_URL/api/v1/products/units/$UNIT_NAME" "" "$WA_TOKEN"
expect_status 409

echo
echo "══════════════════════════════════════════════════════════════════"
echo "04-products 情境跑完了。"
echo "══════════════════════════════════════════════════════════════════"
