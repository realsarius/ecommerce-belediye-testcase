#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./common.sh
source "$SCRIPT_DIR/common.sh"

SMOKE_SUMMARY="${SMOKE_SUMMARY:-/tmp/api-smoke-summary.txt}"
API_LOG="${API_LOG:-/tmp/api.log}"
SMOKE_EXPECT_SWAGGER="${SMOKE_EXPECT_SWAGGER:-true}"

cleanup() {
  stop_api
}

trap cleanup EXIT

rm -f "$SMOKE_SUMMARY"
start_api "$API_LOG"

wait_for_url "$API_BASE_URL/health/ready" "API readiness" 60 2 "$API_LOG"

curl -fsS "$API_BASE_URL/health/live" >/dev/null

if [[ "$SMOKE_EXPECT_SWAGGER" == "true" ]]; then
  wait_for_url "$API_BASE_URL/swagger/index.html" "Swagger" 60 2 "$API_LOG"
  curl -fsS "$API_BASE_URL/swagger/index.html" >/dev/null
fi

search_response="$(mktemp)"
request_json "GET" "$API_BASE_URL/api/v1/search/products?q=test&page=1&pageSize=5" "$search_response"
assert_json_equals "$search_response" "success" "true" "ürün arama"

suggest_response="$(mktemp)"
request_json "GET" "$API_BASE_URL/api/v1/search/suggestions?q=ad&limit=5" "$suggest_response"
assert_json_equals "$suggest_response" "success" "true" "ürün öneri"

customer_token="$(login_and_get_token "customer@test.com" "Test123!")"
support_token="$(login_and_get_token "support@test.com" "Test123!")"

if [[ -z "$customer_token" || -z "$support_token" ]]; then
  log "Login yanıtı token üretmedi"
  exit 1
fi

subject="CI Smoke $(date +%s)-$RANDOM"
initial_message="CI smoke support conversation"
create_response="$(mktemp)"
request_json \
  "POST" \
  "$API_BASE_URL/api/v1/support/conversations" \
  "$create_response" \
  -H "Authorization: Bearer $customer_token" \
  -H "Content-Type: application/json" \
  -d "{\"subject\":\"${subject}\",\"initialMessage\":\"${initial_message}\"}"
assert_json_equals "$create_response" "success" "true" "destek konuşması oluşturma"
assert_json_equals "$create_response" "data.subject" "$subject" "destek konuşması konusu"
conversation_id="$(json_read "$create_response" "data.id")"

queue_response="$(mktemp)"
request_json \
  "GET" \
  "$API_BASE_URL/api/v1/support/conversations/queue?page=1&pageSize=20" \
  "$queue_response" \
  -H "Authorization: Bearer $support_token"
assert_json_equals "$queue_response" "success" "true" "destek kuyruğu"
assert_json_contains "$queue_response" "any(item['id'] == int('${conversation_id}') for item in payload['data']['items'])" "kuyrukta konuşma görünürlüğü"

support_message="CI smoke support reply"
send_response="$(mktemp)"
request_json \
  "POST" \
  "$API_BASE_URL/api/v1/support/conversations/${conversation_id}/messages" \
  "$send_response" \
  -H "Authorization: Bearer $support_token" \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"${support_message}\"}"
assert_json_equals "$send_response" "success" "true" "destek mesajı gönderme"

messages_response="$(mktemp)"
request_json \
  "GET" \
  "$API_BASE_URL/api/v1/support/conversations/${conversation_id}/messages?page=1&pageSize=20" \
  "$messages_response" \
  -H "Authorization: Bearer $customer_token"
assert_json_equals "$messages_response" "success" "true" "destek mesajlarını okuma"
assert_json_contains "$messages_response" "any(item['message'] == '${initial_message}' for item in payload['data']['items'])" "ilk müşteri mesajı"
assert_json_contains "$messages_response" "any(item['message'] == '${support_message}' for item in payload['data']['items'])" "destek yanıt mesajı"

close_response="$(mktemp)"
request_json \
  "POST" \
  "$API_BASE_URL/api/v1/support/conversations/${conversation_id}/close" \
  "$close_response" \
  -H "Authorization: Bearer $support_token"
assert_json_equals "$close_response" "success" "true" "destek konuşması kapatma"
assert_json_equals "$close_response" "data.status" "Closed" "destek konuşması durumu"

{
  echo "health=ok"
  if [[ "$SMOKE_EXPECT_SWAGGER" == "true" ]]; then
    echo "swagger=ok"
  else
    echo "swagger=skipped"
  fi
  echo "search=ok"
  echo "suggestions=ok"
  echo "auth=ok"
  echo "support_flow=ok"
  echo "conversation_id=${conversation_id}"
} | tee "$SMOKE_SUMMARY"

log "API smoke testi başarıyla tamamlandı"
