#!/bin/bash
# Create the windmill database if it does not already exist.
# This script runs as part of PostgreSQL's docker-entrypoint-initdb.d.

set -e

WINDMILL_DB="${WINDMILL_DB:-windmill}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    SELECT 'CREATE DATABASE $WINDMILL_DB'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$WINDMILL_DB')\gexec
EOSQL

echo "Windmill database '$WINDMILL_DB' is ready."
