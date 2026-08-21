#!/usr/bin/env bash

set -e

DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="SalesHub"
DB_USER="anhdo"
BACKUP_FILE="SalesHub-schema.dump"

export PGPASSWORD="12345678"

pg_dump \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    --schema-only \
    -F c \
    -f "$BACKUP_FILE"

unset PGPASSWORD

echo "Backup completed: $BACKUP_FILE"
