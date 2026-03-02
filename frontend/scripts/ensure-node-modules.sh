#!/bin/sh

set -eu

LOCKFILE_PATH="/app/package-lock.json"
STAMP_PATH="/app/node_modules/.package-lock.sha256"

if [ ! -f "$LOCKFILE_PATH" ]; then
  echo "[frontend] package-lock.json bulunamadi, normal dev sunucusu baslatiliyor."
  exec npm run dev -- --host 0.0.0.0
fi

CURRENT_HASH="$(sha256sum "$LOCKFILE_PATH" | awk '{print $1}')"
STORED_HASH=""

if [ -f "$STAMP_PATH" ]; then
  STORED_HASH="$(cat "$STAMP_PATH")"
fi

if [ ! -d /app/node_modules ] || [ "$CURRENT_HASH" != "$STORED_HASH" ]; then
  echo "[frontend] Bagimliliklar senkronize ediliyor..."
  npm install
  mkdir -p /app/node_modules
  echo "$CURRENT_HASH" > "$STAMP_PATH"
else
  echo "[frontend] node_modules guncel, npm install atlandi."
fi

exec npm run dev -- --host 0.0.0.0
