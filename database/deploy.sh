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

for script in 01_schema.sql 02_functions_views.sql 03_procedures.sql 04_seed_reference.sql 05_seed_sample.sql; do
  echo "Running $script"
  "$SQLCMD" -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -b -I -d "$DATABASE" -i "$DIR/$script"
done

if [[ -n "${API_DB_PASSWORD:-}" ]]; then
  echo "Running 06_api_login.sql"
  "$SQLCMD" -S "$SERVER" -U "$USER" -P "$PASSWORD" -C -b -I -d "$DATABASE" -v API_DB_PASSWORD="$API_DB_PASSWORD" -i "$DIR/06_api_login.sql"
fi

echo "Database $DATABASE deployed to $SERVER"
