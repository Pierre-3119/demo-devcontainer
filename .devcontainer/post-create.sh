#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore

# SQL Server peut accepter les connexions avant d'être réellement prêt : on réessaie la migration
# jusqu'à ce qu'elle passe plutôt que de faire échouer la création du conteneur.
max_attempts=30
for attempt in $(seq 1 "$max_attempts"); do
  if dotnet ef database update --project src/Api; then
    exit 0
  fi
  echo "SQL Server pas encore prêt (tentative $attempt/$max_attempts), nouvel essai dans 5 s..."
  sleep 5
done

echo "Échec de la migration après $max_attempts tentatives." >&2
exit 1
