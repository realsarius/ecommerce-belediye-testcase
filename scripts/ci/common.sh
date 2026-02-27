#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
API_LOG="${API_LOG:-/tmp/api.log}"
API_PID=""

log() {
  printf '[ci] %s\n' "$*"
}

start_api() {
  local log_file="${1:-$API_LOG}"

  if [[ -z "${ASPNETCORE_URLS:-}" ]]; then
    export ASPNETCORE_URLS="$API_BASE_URL"
  fi

  log "API başlatılıyor (${ASPNETCORE_URLS})"
  dotnet run --project "$ROOT_DIR/EcommerceAPI.API" --no-build --configuration Release >"$log_file" 2>&1 &
  API_PID=$!
  export API_PID
}

stop_api() {
  if [[ -z "${API_PID:-}" ]]; then
    return
  fi

  if kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi

  unset API_PID
}

wait_for_url() {
  local url="$1"
  local label="$2"
  local attempts="${3:-60}"
  local sleep_seconds="${4:-2}"
  local log_file="${5:-$API_LOG}"

  for attempt in $(seq 1 "$attempts"); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      return 0
    fi

    if [[ "$attempt" -eq "$attempts" ]]; then
      log "${label} zamanında hazır olmadı: ${url}"
      tail -n 200 "$log_file" 2>/dev/null || true
      return 1
    fi

    sleep "$sleep_seconds"
  done
}

request_json() {
  local method="$1"
  local url="$2"
  local output_file="$3"
  shift 3

  local status_code
  status_code="$(curl -sS -o "$output_file" -w "%{http_code}" -X "$method" "$url" "$@")"

  if [[ ! "$status_code" =~ ^2 ]]; then
    log "HTTP isteği başarısız (${method} ${url}) status=${status_code}"
    cat "$output_file" 2>/dev/null || true
    return 1
  fi
}

request_json_with_retry() {
  local method="$1"
  local url="$2"
  local output_file="$3"
  local attempts="${4:-3}"
  local sleep_seconds="${5:-2}"
  shift 5

  local attempt
  for attempt in $(seq 1 "$attempts"); do
    if request_json "$method" "$url" "$output_file" "$@"; then
      return 0
    fi

    if [[ "$attempt" -eq "$attempts" ]]; then
      return 1
    fi

    sleep "$sleep_seconds"
  done
}

json_read() {
  local file_path="$1"
  local path="$2"

  python3 - "$file_path" "$path" <<'PY'
import json
import sys

file_path = sys.argv[1]
path = sys.argv[2]

with open(file_path, "r", encoding="utf-8") as handle:
    current = json.load(handle)

for part in path.split("."):
    if isinstance(current, list):
        current = current[int(part)]
    else:
        current = current[part]

if isinstance(current, bool):
    print(str(current).lower())
elif current is None:
    print("")
else:
    print(current)
PY
}

assert_json_equals() {
  local file_path="$1"
  local path="$2"
  local expected="$3"
  local label="$4"

  local actual
  actual="$(json_read "$file_path" "$path")"

  if [[ "$actual" != "$expected" ]]; then
    log "${label} doğrulaması başarısız. Beklenen='${expected}' Gerçek='${actual}'"
    cat "$file_path"
    return 1
  fi
}

assert_json_contains() {
  local file_path="$1"
  local expression="$2"
  local label="$3"

  python3 - "$file_path" "$expression" "$label" <<'PY'
import json
import sys

file_path, expression, label = sys.argv[1:4]

with open(file_path, "r", encoding="utf-8") as handle:
    payload = json.load(handle)

if not eval(expression, {"payload": payload}):
    print(f"[ci] {label} doğrulaması başarısız")
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    raise SystemExit(1)
PY
}

login_and_get_token() {
  local email="$1"
  local password="$2"
  local response_file
  response_file="$(mktemp)"

  request_json \
    "POST" \
    "$API_BASE_URL/api/v1/auth/login" \
    "$response_file" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${email}\",\"password\":\"${password}\"}"

  assert_json_equals "$response_file" "success" "true" "login başarı"
  json_read "$response_file" "data.token"
}
