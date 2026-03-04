#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./common.sh
source "$SCRIPT_DIR/common.sh"

SMOKE_SUMMARY="${SMOKE_SUMMARY:-/tmp/api-smoke-summary.txt}"
API_LOG="${API_LOG:-/tmp/api.log}"
SMOKE_EXPECT_SWAGGER="${SMOKE_EXPECT_SWAGGER:-true}"
SMOKE_REUSE_RUNNING_API="${SMOKE_REUSE_RUNNING_API:-true}"
SMOKE_INCLUDE_PAYMENT_FLOW="${SMOKE_INCLUDE_PAYMENT_FLOW:-true}"

cleanup() {
  stop_api
}

trap cleanup EXIT

rm -f "$SMOKE_SUMMARY"
if [[ "$SMOKE_REUSE_RUNNING_API" == "true" ]] && curl -fsS "$API_BASE_URL/health/ready" >/dev/null 2>&1; then
  log "Hazir calisan API kullaniliyor: $API_BASE_URL"
else
  start_api "$API_LOG"
fi

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

products_response="$(mktemp)"
request_json "GET" "$API_BASE_URL/api/v1/products?page=1&pageSize=20" "$products_response"

smoke_product_id="$(python3 - "$products_response" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as handle:
    payload = json.load(handle)

items = payload.get("data", {}).get("items", [])
for item in items:
    price = item.get("price") or 0
    if price < 5000:
        print(item["id"])
        raise SystemExit(0)

raise SystemExit("Uygun smoke checkout ürünü bulunamadı")
PY
)"

clear_cart_response="$(mktemp)"
request_json \
  "DELETE" \
  "$API_BASE_URL/api/v1/cart" \
  "$clear_cart_response" \
  -H "Authorization: Bearer $customer_token"

add_to_cart_response="$(mktemp)"
request_json \
  "POST" \
  "$API_BASE_URL/api/v1/cart/items" \
  "$add_to_cart_response" \
  -H "Authorization: Bearer $customer_token" \
  -H "Content-Type: application/json" \
  -d "{\"productId\":${smoke_product_id},\"quantity\":1}"
assert_json_equals "$add_to_cart_response" "success" "true" "checkout smoke sepete ekleme"

checkout_response="$(mktemp)"
request_json \
  "POST" \
  "$API_BASE_URL/api/v1/orders" \
  "$checkout_response" \
  -H "Authorization: Bearer $customer_token" \
  -H "Content-Type: application/json" \
  -d '{"shippingAddress":"Test Customer, Bağdat Cd. No:1, Kadıköy/İstanbul P.K: 34700 - Tel: 5551234567","paymentMethod":"CreditCard","notes":"","preliminaryInfoAccepted":true,"distanceSalesContractAccepted":true,"invoiceInfo":{"type":"Individual","fullName":"Test Customer","invoiceAddress":"Test Customer, Bağdat Cd. No:1, Kadıköy/İstanbul P.K: 34700 - Tel: 5551234567"}}'
assert_json_equals "$checkout_response" "success" "true" "checkout smoke sipariş oluşturma"
order_id="$(json_read "$checkout_response" "data.id")"

order_detail_response="$(mktemp)"
request_json \
  "GET" \
  "$API_BASE_URL/api/v1/orders/${order_id}" \
  "$order_detail_response" \
  -H "Authorization: Bearer $customer_token"
assert_json_equals "$order_detail_response" "success" "true" "checkout smoke sipariş detayı"

checkout_payment_flow_status="skipped"
if [[ "$SMOKE_INCLUDE_PAYMENT_FLOW" == "true" ]]; then
  payment_response="$(mktemp)"
  request_json \
    "POST" \
    "$API_BASE_URL/api/v1/payments" \
    "$payment_response" \
    -H "Authorization: Bearer $customer_token" \
    -H "Content-Type: application/json" \
    -d "{\"orderId\":${order_id},\"paymentProvider\":\"Iyzico\",\"cardNumber\":\"5406670000000009\",\"cardHolderName\":\"TEST CUSTOMER\",\"expiryDate\":\"12/27\",\"cvv\":\"123\",\"require3DS\":false}"
  assert_json_equals "$payment_response" "success" "true" "checkout smoke ödeme isteği"
  assert_json_equals "$payment_response" "data.status" "Success" "checkout smoke ödeme sonucu"

  request_json \
    "GET" \
    "$API_BASE_URL/api/v1/orders/${order_id}" \
    "$order_detail_response" \
    -H "Authorization: Bearer $customer_token"
  assert_json_equals "$order_detail_response" "success" "true" "checkout smoke sipariş detayı"
  assert_json_equals "$order_detail_response" "data.status" "Paid" "checkout smoke sipariş durumu"
  assert_json_equals "$order_detail_response" "data.payment.status" "Success" "checkout smoke payment durumu"
  checkout_payment_flow_status="ok"
else
  assert_json_equals "$order_detail_response" "data.status" "PendingPayment" "checkout smoke sipariş durumu"
  assert_json_equals "$order_detail_response" "data.payment.status" "Pending" "checkout smoke payment durumu"
fi

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
  echo "checkout_order_flow=ok"
  echo "checkout_payment_flow=${checkout_payment_flow_status}"
  echo "checkout_order_id=${order_id}"
  echo "conversation_id=${conversation_id}"
} | tee "$SMOKE_SUMMARY"

log "API smoke testi başarıyla tamamlandı"
