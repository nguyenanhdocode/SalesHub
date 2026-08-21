#!/usr/bin/env bash

set -e

DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="SalesHub_Test"
DB_USER="anhdo"
BACKUP_FILE="SalesHub-schema.dump"

export PGPASSWORD="12345678"

# Xóa database cũ
dropdb \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    --if-exists \
    "$DB_NAME"

# Tạo database mới
createdb \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    "$DB_NAME"

# Restore schema
pg_restore \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    "$BACKUP_FILE"

unset PGPASSWORD

echo "Restore completed: $DB_NAME"