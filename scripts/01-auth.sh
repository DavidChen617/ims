#!/usr/bin/env bash
# 情境:登入/登出/換發 token,以及跟認證有關的錯誤情境。
# 依賴: ./00-setup.sh 先跑過。
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"
load_state

WA_PASSWORD="Passw0rd!"

step "重新用 WarehouseAdmin 登入,拿一組新的 refreshToken 來測換發 (Organization)"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WA_USERNAME" --arg p "$WA_PASSWORD" '{username:$u, password:$p}')"
expect_status 200
LOGIN_USER_ID=$(echo "$CALL_BODY" | jq -r '.userId')
REFRESH_TOKEN=$(echo "$CALL_BODY" | jq -r '.refreshToken')

step "用剛拿到的 refreshToken 換發新的 access token (Organization, 免登入)"
call POST "$ORG_URL/api/v1/auth/refresh/token" \
  "$(jq -n --arg uid "$LOGIN_USER_ID" --arg rt "$REFRESH_TOKEN" '{userId:$uid, refreshToken:$rt}')"
expect_status 200
NEW_REFRESH_TOKEN=$(echo "$CALL_BODY" | jq -r '.refreshToken')

step "★負向測試★ 舊的 refreshToken 用過一次就作廢,再拿去換一次應該要失敗 (401)"
call POST "$ORG_URL/api/v1/auth/refresh/token" \
  "$(jq -n --arg uid "$LOGIN_USER_ID" --arg rt "$REFRESH_TOKEN" '{userId:$uid, refreshToken:$rt}')"
expect_status 401

step "登出 (Organization, 需要登入但不限角色)"
call POST "$ORG_URL/api/v1/auth/logout" \
  "$(jq -n --arg rt "$NEW_REFRESH_TOKEN" '{refreshToken:$rt}')" \
  "$WA_TOKEN"
expect_status 204

step "★負向測試★ 帳號密碼錯誤登入應該回 401"
call POST "$ORG_URL/api/v1/auth/login" \
  "$(jq -n --arg u "$WA_USERNAME" --arg p "wrong-password" '{username:$u, password:$p}')"
expect_status 401

step "★負向測試★ 沒帶 token 打受保護的 API 應該回 401"
call GET "$ORG_URL/api/v1/warehouse"
expect_status 401

step "★已知 bug★ WarehouseAdmin 自助建立 WarehouseUser 這支 API 掛在 RequireAuthorization(\"WarehouseAdminOnly\"),但 Organization.Program.cs 從沒註冊過這個 policy 名字,理論上任何呼叫都會失敗(通常是 500 而不是乾淨的 403)"
call POST "$ORG_URL/api/v1/auth/warehouseAdmin/register/warehouseUser" \
  "$(jq -n '{name:"測試", username:"should-never-be-created", password:"Passw0rd!"}')" \
  "$WA_TOKEN"
echo ">> 實際拿到 HTTP $CALL_STATUS(如果哪天變成 200/201,代表這個已知 bug 被修好了,記得回來更新這支腳本)。"

echo
echo "══════════════════════════════════════════════════════════════════"
echo "01-auth 情境跑完了。"
echo "══════════════════════════════════════════════════════════════════"
