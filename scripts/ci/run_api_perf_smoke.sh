#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./common.sh
source "$SCRIPT_DIR/common.sh"

API_LOG="${API_LOG:-/tmp/api-perf.log}"
PERF_SUMMARY="${PERF_SUMMARY:-/tmp/perf-summary.txt}"
PERF_SEARCH_PRODUCTS_MAX_P95="${PERF_SEARCH_PRODUCTS_MAX_P95:-1.20}"
PERF_SEARCH_SUGGESTIONS_MAX_P95="${PERF_SEARCH_SUGGESTIONS_MAX_P95:-1.00}"

cleanup() {
  stop_api
}

trap cleanup EXIT

run_perf_case() {
  local case_name="$1"
  local target_path="$2"
  local total_requests="$3"
  local concurrency="$4"
  local rounds="$5"
  local max_p95="$6"

  local total_non_200=0
  local worst_p95=0
  local target_url="${API_BASE_URL}${target_path}"

  for round in $(seq 1 "$rounds"); do
    local results_file="/tmp/${case_name}-perf-results-round-${round}.txt"
    local times_file="/tmp/${case_name}-perf-times-round-${round}.txt"

    seq 1 "$total_requests" | xargs -I{} -P "$concurrency" sh -c \
      'curl -s -o /dev/null -w "%{http_code} %{time_total}\n" "$1"' _ "$target_url" \
      >"$results_file"

    local non_200_round
    non_200_round="$(awk '$1 != 200 {count++} END {print count+0}' "$results_file")"
    total_non_200=$((total_non_200 + non_200_round))

    awk '{print $2}' "$results_file" | sort -n >"$times_file"
    local sample_count
    sample_count="$(wc -l < "$times_file")"
    local p95_index=$(( (sample_count * 95 + 99) / 100 ))
    local p95
    p95="$(awk -v idx="$p95_index" 'NR==idx {print $1}' "$times_file")"
    if [[ -z "$p95" ]]; then
      p95=0
    fi

    local avg
    avg="$(awk '{sum+=$1} END { if (NR==0) print 0; else printf "%.4f", sum/NR }' "$times_file")"
    worst_p95="$(awk -v current="$worst_p95" -v candidate="$p95" 'BEGIN { if (candidate > current) printf "%.4f", candidate; else printf "%.4f", current }')"

    echo "case=${case_name} round=${round} requests=${total_requests} concurrency=${concurrency} non_200=${non_200_round} avg=${avg}s p95=${p95}s" | tee -a "$PERF_SUMMARY"
  done

  if awk -v p95="$worst_p95" -v threshold="$max_p95" 'BEGIN { exit (p95 > threshold ? 0 : 1) }'; then
    log "${case_name} perf smoke başarısız: worst p95 ${worst_p95}s, eşik ${max_p95}s"
    tail -n 200 "$API_LOG" 2>/dev/null || true
    exit 1
  fi

  if [[ "$total_non_200" -ne 0 ]]; then
    log "${case_name} perf smoke başarısız: ${total_non_200} adet non-200 response"
    tail -n 200 "$API_LOG" 2>/dev/null || true
    exit 1
  fi
}

rm -f "$PERF_SUMMARY"
start_api "$API_LOG"

wait_for_url "$API_BASE_URL/health/ready" "API readiness" 60 2 "$API_LOG"

run_perf_case "search-products" "/api/v1/search/products?q=test&page=1&pageSize=5" 60 10 2 "$PERF_SEARCH_PRODUCTS_MAX_P95"
run_perf_case "search-suggestions" "/api/v1/search/suggestions?q=ad&limit=8" 40 10 2 "$PERF_SEARCH_SUGGESTIONS_MAX_P95"

log "API performance smoke testi başarıyla tamamlandı"
