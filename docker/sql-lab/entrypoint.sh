#!/bin/bash
set -euo pipefail

secret_file=/run/secrets/sql_lab_sa_password
if [[ ! -r "$secret_file" ]]; then
    echo "SqlLab startup refused: the administrator secret file is missing." >&2
    exit 78
fi

password="$(tr -d '\r\n' < "$secret_file")"
if (( ${#password} < 12 || ${#password} > 128 )); then
    echo "SqlLab startup refused: the administrator secret file is invalid." >&2
    exit 78
fi

export MSSQL_SA_PASSWORD="$password"
unset password
exec /opt/mssql/bin/sqlservr
