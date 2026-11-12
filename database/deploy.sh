#!/usr/bin/env bash
set -euo pipefail

SERVER="${DB_SERVER:-localhost,1433}"
DATABASE="${DB_NAME:-AeShipManagement}"
USER="${DB_USER:-sa}"
PASSWORD="${DB_PASSWORD:?DB_PASSWORD is required}"
SQLCMD="${SQLCMD:-sqlcmd}"
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"$SQLCMD" -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -b -Q \
  "IF DB_ID('$DATABASE') IS NOT NULL BEGIN ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DATABASE]; END; CREATE DATABASE [$DATABASE];"

while IFS= read -r script || [[ -n "$script" ]]; do
  script="${script%$'\r'}"
  [[ -z "$script" ]] && continue
  echo "Running $script"
  "$SQLCMD" -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -b -I -d "$DATABASE" -i "$DIR/$script"
done < "$DIR/deploy-order.txt"

if [[ -n "${API_DB_PASSWORD:-}" ]]; then
  echo "Running security/api_login.sql"
  "$SQLCMD" -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -b -I -d "$DATABASE" -v API_DB_PASSWORD="$API_DB_PASSWORD" -i "$DIR/security/api_login.sql"
fi

echo "Database $DATABASE deployed to $SERVER"
