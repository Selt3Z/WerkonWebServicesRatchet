#!/usr/bin/env bash
set -euo pipefail

STATUS_FILE="/backups/status.json"
BACKUP_DIR="/backups/daily"
WEEKLY_DIR="/backups/weekly"
TIMESTAMP="$(date -u +"%Y%m%dT%H%M%SZ")"
DUMP_FILE="${BACKUP_DIR}/ratchet-${TIMESTAMP}.sql.gz"

mkdir -p "${BACKUP_DIR}" "${WEEKLY_DIR}"

write_status() {
  local status="$1"
  local message="$2"
  local size="${3:-0}"
  local restic_enabled="false"

  if [[ -n "${RESTIC_REPOSITORY:-}" ]]; then
    restic_enabled="true"
  fi

  cat > "${STATUS_FILE}" <<EOF
{"lastRunUtc":"$(date -u +"%Y-%m-%dT%H:%M:%SZ")","lastStatus":"${status}","lastMessage":"${message}","lastBackupSizeBytes":${size},"resticEnabled":${restic_enabled}}
EOF
}

rotate_daily_retention() {
  local retention_days="${DAILY_RETENTION_DAYS:-7}"

  if [[ "${retention_days}" -le 0 ]]; then
    return 0
  fi

  find "${BACKUP_DIR}" -maxdepth 1 -type f -name 'ratchet-*.sql.gz' -mtime +"${retention_days}" -delete
  echo "Daily retention applied: keep last ${retention_days} day(s)."
}

promote_weekly_if_sunday() {
  if [[ "$(date -u +%u)" -ne 7 ]]; then
    return 0
  fi

  local weekly_file="${WEEKLY_DIR}/ratchet-week-$(date -u +%Y-W%V).sql.gz"
  cp -f "${DUMP_FILE}" "${weekly_file}"
  echo "Weekly copy created: ${weekly_file}"
}

rotate_weekly_retention() {
  local retention_weeks="${WEEKLY_RETENTION_WEEKS:-4}"

  if [[ "${retention_weeks}" -le 0 ]]; then
    return 0
  fi

  local retention_days=$((retention_weeks * 7))
  find "${WEEKLY_DIR}" -maxdepth 1 -type f -name 'ratchet-*.sql.gz' -mtime +"${retention_days}" -delete
  echo "Weekly retention applied: keep last ${retention_weeks} week(s)."
}

run_backup() {
  echo "Starting PostgreSQL backup..."
  PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump \
    -h "${POSTGRES_HOST}" \
    -U "${POSTGRES_USER}" \
    -d "${POSTGRES_DB}" \
    | gzip > "${DUMP_FILE}"

  local size
  size="$(stat -c%s "${DUMP_FILE}")"

  promote_weekly_if_sunday
  rotate_daily_retention
  rotate_weekly_retention

  if [[ -n "${RESTIC_REPOSITORY:-}" && -n "${RESTIC_PASSWORD:-}" ]]; then
    echo "Uploading backup snapshot to restic repository..."
    restic snapshots >/dev/null 2>&1 || restic init
    restic backup "${DUMP_FILE}" --tag ratchet-db
    restic forget --keep-daily "${RESTIC_KEEP_DAILY:-7}" --keep-weekly "${RESTIC_KEEP_WEEKLY:-4}" --prune
  fi

  write_status "success" "Backup completed." "${size}"
  echo "Backup completed: ${DUMP_FILE}"
}

while true; do
  if run_backup; then
    :
  else
    write_status "error" "Backup script failed."
  fi

  sleep "${BACKUP_INTERVAL_SECONDS:-86400}"
done
