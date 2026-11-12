#!/usr/bin/env bash
set -euo pipefail

MAX_LINES="${1:-250}"
VIOLATIONS=0

while IFS= read -r -d '' file; do
  lines=$(wc -l < "$file" | tr -d ' ')
  if [[ "$lines" -gt "$MAX_LINES" ]]; then
    echo "[SPAGHETTI GUARD VIOLATION] $file has $lines lines (Max allowed: $MAX_LINES)"
    VIOLATIONS=$((VIOLATIONS + 1))
  fi
done < <(find apps database tests -type f \( -name "*.cs" -o -name "*.ts" -o -name "*.tsx" -o -name "*.sql" -o -name "*.gs" \) -print0 2>/dev/null)

if [[ "$VIOLATIONS" -gt 0 ]]; then
  echo "FAILED: Found $VIOLATIONS file(s) exceeding $MAX_LINES lines. Decompose using the 5-Level Taxonomy."
  exit 1
fi

echo "PASSED: All source, SQL, and test files comply with the <= $MAX_LINES line taxonomy cap."
