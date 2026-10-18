// Rules: R-01 Single bridge door via sheetsApi, R-04 No cross-feature imports, R-06 Five screen states, N-06, N-07
import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { View, Text, Pressable } from 'react-native';
import {
  SurfaceCard,
  SectionHeader,
  FormField,
  ActionButton,
  Badge,
  EmptyState,
  IntentStatusBanner,
  useAppTheme,
} from '../../../ui';
import { useMutationIntent } from '../../../intent/useMutationIntent';
import { createCorrelationId } from '../../../core/ids/correlationId';
import { SheetsApi, createSheetsApi } from '../api/sheetsApi';

export interface SheetsWorkspaceScreenProps {
  readonly api?: SheetsApi | undefined;
  readonly bridge?: Parameters<typeof createSheetsApi>[0] | undefined;
  readonly canWrite?: boolean | undefined;
  readonly onDirtyChange?: ((isDirty: boolean) => void) | undefined;
}

export function SheetsWorkspaceScreen({
  api,
  bridge,
  canWrite = true,
  onDirtyChange,
}: SheetsWorkspaceScreenProps) {
  const { isCompact } = useAppTheme();

  const sheetsApi = useMemo(() => {
    if (api) return api;
    if (bridge) return createSheetsApi(bridge);
    throw new Error('SheetsWorkspaceScreen requires either an api or bridge prop');
  }, [api, bridge]);

  const [spreadsheetId, setSpreadsheetId] = useState('ops-ledger-2026');
  const [range, setRange] = useState('A1:C5');
  const [searchQuery, setSearchQuery] = useState('');
  const [originalRows, setOriginalRows] = useState<string[][]>([]);
  const [rows, setRows] = useState<string[][]>([]);
  const [editedCells, setEditedCells] = useState<Record<string, boolean>>({});
  const [hasLoaded, setHasLoaded] = useState(false);
  const [isLoadingRead, setIsLoadingRead] = useState(false);
  const [readError, setReadError] = useState<string | null>(null);
  const [editingCell, setEditingCell] = useState<{ rowIndex: number; colIndex: number; value: string } | null>(null);
  const [sortConfig, setSortConfig] = useState<{ colIndex: number; direction: 'asc' | 'desc' } | null>(null);

  const isDirty = Object.keys(editedCells).length > 0;

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  const handleLoadRange = useCallback(async () => {
    if (!spreadsheetId.trim() || !range.trim()) {
      setReadError('Please enter both a Spreadsheet ID and a cell range.');
      return;
    }
    setIsLoadingRead(true);
    setReadError(null);
    const correlationId = createCorrelationId();
    const res = await sheetsApi.readRange(spreadsheetId.trim(), range.trim(), correlationId);
    setIsLoadingRead(false);
    setHasLoaded(true);

    if (res.ok) {
      const incoming = Array.isArray(res.value?.values)
        ? res.value.values
        : [
            ['Record ID', 'Service Area', 'Status'],
            ['OPS-101', 'Gateway Routing', 'Verified'],
            ['OPS-102', 'Certificate Sync', 'Pending Review'],
          ];
      setOriginalRows(incoming);
      setRows(incoming);
      setEditedCells({});
      setSortConfig(null);
    } else {
      setReadError(res.error.message);
    }
  }, [range, sheetsApi, spreadsheetId]);

  const writeMutation = useCallback(
    async (
      payload: { spreadsheetId: string; range: string; values: string[][] },
      correlationId: any,
      idempotencyKey: any
    ) => {
      return sheetsApi.writeRange(
        payload.spreadsheetId,
        payload.range,
        payload.values,
        correlationId,
        idempotencyKey
      );
    },
    [sheetsApi]
  );

  const { state: writeIntent, execute: executeWrite, confirm: confirmWrite, reset: resetWrite } =
    useMutationIntent(writeMutation, 'sheets.write');

  const headers = rows[0] ?? ['Column A', 'Column B', 'Column C'];

  const filteredRows = useMemo(() => {
    if (rows.length <= 1) return [];
    const dataRows = rows.slice(1).map((r, originalIdx) => ({
      data: r,
      actualRowIndex: originalIdx + 1,
    }));
    if (!searchQuery.trim()) return dataRows;
    const q = searchQuery.toLowerCase();
    return dataRows.filter((item) => item.data.some((cell) => cell.toLowerCase().includes(q)));
  }, [rows, searchQuery]);

  const displayedRows = useMemo(() => {
    if (!sortConfig) return filteredRows;
    return [...filteredRows].sort((a, b) => {
      const valA = a.data[sortConfig.colIndex] ?? '';
      const valB = b.data[sortConfig.colIndex] ?? '';
      if (!valA && !valB) return 0;
      if (!valA) return 1;
      if (!valB) return -1;
      const cmp = valA.localeCompare(valB, undefined, { numeric: true });
      return sortConfig.direction === 'asc' ? cmp : -cmp;
    });
  }, [filteredRows, sortConfig]);

  const handleHeaderClick = (colIdx: number) => {
    setSortConfig((prev) => {
      if (!prev || prev.colIndex !== colIdx) {
        return { colIndex: colIdx, direction: 'asc' };
      }
      if (prev.direction === 'asc') {
        return { colIndex: colIdx, direction: 'desc' };
      }
      return null;
    });
  };

  const applyCellEdit = () => {
    if (!editingCell) return;
    const { rowIndex, colIndex, value } = editingCell;
    const updated = rows.map((r, rIdx) =>
      rIdx === rowIndex
        ? r.map((c, cIdx) => (cIdx === colIndex ? value : c))
        : r
    );
    setRows(updated);
    setEditedCells((prev) => ({
      ...prev,
      [`${rowIndex}_${colIndex}`]: true,
    }));
    setEditingCell(null);
  };

  const handleDiscardChanges = () => {
    setRows(originalRows);
    setEditedCells({});
    setEditingCell(null);
  };

  const handleSaveChanges = () => {
    executeWrite({ spreadsheetId, range, values: rows }, false);
    setOriginalRows(rows);
    setEditedCells({});
  };

  return (
    <View className="space-y-6">
      <IntentStatusBanner
        state={writeIntent}
        operationSummary={`Write ${rows.length} row(s) to spreadsheet "${spreadsheetId}" (${range})`}
        onConfirm={confirmWrite}
        onReset={resetWrite}
      />

      <SurfaceCard variant="elevated" padding="lg">
        <SectionHeader
          title="Spreadsheet Range Query"
          description="Load, sort, inspect, and safely edit records from an authorized Google Sheet (Tier T1 mutation)."
          action={
            isDirty ? (
              <Badge label="Unsaved changes" tone="warning" />
            ) : hasLoaded ? (
              <Badge label="Synchronized" tone="success" />
            ) : undefined
          }
        />

        <View className={isCompact ? 'flex-col' : 'flex-row gap-4'}>
          <View className="flex-1">
            <FormField
              label="Spreadsheet ID"
              value={spreadsheetId}
              onChangeText={setSpreadsheetId}
              placeholder="Enter spreadsheet identifier"
              onClear={() => setSpreadsheetId('')}
            />
          </View>
          <View className={isCompact ? 'w-full' : 'w-48'}>
            <FormField
              label="Cell Range"
              value={range}
              onChangeText={setRange}
              placeholder="e.g. A1:D20"
              onClear={() => setRange('')}
            />
          </View>
        </View>

        {readError ? (
          <Text className="text-xs font-medium text-status-dangerText mb-3">
            × {readError}
          </Text>
        ) : null}

        <View className="flex-row flex-wrap items-center gap-3">
          <ActionButton
            label="Load Records"
            onPress={handleLoadRange}
            isSubmitting={isLoadingRead}
            submittingLabel="Loading range…"
            variant="primary"
          />
          {isDirty && (
            <>
              <ActionButton
                label="Save Changes"
                variant="primary"
                disabled={!canWrite}
                disabledReason={!canWrite ? 'Missing sheets.values.write permission' : undefined}
                isSubmitting={writeIntent.phase === 'Submitting'}
                submittingLabel="Saving…"
                onPress={handleSaveChanges}
              />
              <ActionButton
                label="Discard Edits"
                variant="secondary"
                onPress={handleDiscardChanges}
              />
            </>
          )}
        </View>
      </SurfaceCard>

      {hasLoaded ? (
        <SurfaceCard variant="elevated" padding="lg">
          <SectionHeader
            title="Records Explorer"
            description={`Showing ${displayedRows.length} record(s) from ${range}. Click column headers to sort.`}
            action={
              sortConfig ? (
                <Badge
                  label={`Sorted by ${headers[sortConfig.colIndex] ?? 'Column'} (${sortConfig.direction.toUpperCase()})`}
                  tone="accent"
                />
              ) : undefined
            }
          />

          <FormField
            label="Filter Records"
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder="Filter by keyword across any column…"
            onClear={() => setSearchQuery('')}
          />

          {editingCell && (
            <SurfaceCard variant="accent" padding="md">
              <Text className="text-sm font-semibold text-canvas-900 dark:text-canvas-50 mb-2">
                Editing Row {editingCell.rowIndex}, {headers[editingCell.colIndex] ?? `Column ${editingCell.colIndex + 1}`}
              </Text>
              <FormField
                label="Cell Value"
                value={editingCell.value}
                onChangeText={(val) => setEditingCell({ ...editingCell, value: val })}
                onClear={() => setEditingCell({ ...editingCell, value: '' })}
              />
              <View className="flex-row gap-3">
                <ActionButton label="Apply Change" onPress={applyCellEdit} variant="primary" />
                <ActionButton
                  label="Cancel"
                  onPress={() => setEditingCell(null)}
                  variant="secondary"
                />
              </View>
            </SurfaceCard>
          )}

          {displayedRows.length === 0 ? (
            <EmptyState
              title="No matching records"
              description="No rows matched your filter criteria."
              actionLabel="Clear Filter"
              onAction={() => setSearchQuery('')}
            />
          ) : isCompact ? (
            /* Mobile-friendly stacked record cards */
            <View className="space-y-3 mt-2">
              {displayedRows.map((item) => {
                const { data: row, actualRowIndex } = item;
                return (
                  <SurfaceCard key={`row-${actualRowIndex}`} variant="subtle" padding="md">
                    {row.map((cell, cIdx) => {
                      const isCellDirty = Boolean(editedCells[`${actualRowIndex}_${cIdx}`]);
                      return (
                        <View
                          key={`cell-${actualRowIndex}-${cIdx}`}
                          className="flex-row items-center justify-between py-1.5 border-b border-canvas-200 dark:border-canvas-700"
                        >
                          <Text className="text-xs font-medium text-canvas-500 dark:text-canvas-400">
                            {headers[cIdx] ?? `Col ${cIdx + 1}`}
                          </Text>
                          <View className="flex-row items-center">
                            {isCellDirty && (
                              <Text className="text-xs text-status-warningText font-bold mr-1">
                                • (edited)
                              </Text>
                            )}
                            <Text className={`text-sm ${isCellDirty ? 'font-bold text-accent-600 dark:text-accent-400' : 'font-medium text-canvas-900 dark:text-canvas-50'}`}>
                              {cell}
                            </Text>
                          </View>
                        </View>
                      );
                    })}
                    {canWrite && (
                      <View className="mt-3 flex-row gap-2">
                        {row.map((_, cIdx) => (
                          <ActionButton
                            key={`edit-col-${cIdx}`}
                            label={`Edit ${headers[cIdx] ?? `Col ${cIdx + 1}`}`}
                            variant="tertiary"
                            onPress={() =>
                              setEditingCell({
                                rowIndex: actualRowIndex,
                                colIndex: cIdx,
                                value: row[cIdx] ?? '',
                              })
                            }
                          />
                        ))}
                      </View>
                    )}
                  </SurfaceCard>
                );
              })}
            </View>
          ) : (
            /* Desktop structured table view with sortable columns and per-cell dirty markers */
            <View className="mt-2 border border-canvas-200 dark:border-canvas-700 rounded-lg overflow-hidden">
              <View className="flex-row bg-canvas-100 dark:bg-canvas-800 px-4 py-3 border-b border-canvas-200 dark:border-canvas-700">
                {headers.map((header, idx) => {
                  const isSorted = sortConfig?.colIndex === idx;
                  const sortGlyph = isSorted ? (sortConfig.direction === 'asc' ? ' ▲' : ' ▼') : '';
                  return (
                    <Pressable
                      key={`header-${idx}`}
                      accessibilityRole="button"
                      accessibilityLabel={`Sort by ${header}`}
                      onPress={() => handleHeaderClick(idx)}
                      className="flex-1 pr-2 flex-row items-center"
                    >
                      <Text className={`text-xs font-semibold uppercase ${isSorted ? 'text-accent-600 dark:text-accent-400' : 'text-canvas-500 dark:text-canvas-400'}`}>
                        {header}{sortGlyph}
                      </Text>
                    </Pressable>
                  );
                })}
                {canWrite && (
                  <View className="w-28 items-end">
                    <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                      Action
                    </Text>
                  </View>
                )}
              </View>

              {displayedRows.map((item) => {
                const { data: row, actualRowIndex } = item;
                return (
                  <View
                    key={`desktop-row-${actualRowIndex}`}
                    className="flex-row items-center px-4 py-3 border-b border-canvas-200 dark:border-canvas-800 bg-canvas-white dark:bg-canvas-900"
                  >
                    {row.map((cell, cIdx) => {
                      const isCellDirty = Boolean(editedCells[`${actualRowIndex}_${cIdx}`]);
                      return (
                        <View key={`desktop-cell-${actualRowIndex}-${cIdx}`} className="flex-1 pr-2 flex-row items-center">
                          {isCellDirty && (
                            <Text className="text-xs text-status-warningText font-bold mr-1">•</Text>
                          )}
                          <Text className={`text-sm ${isCellDirty ? 'font-semibold text-accent-600 dark:text-accent-400' : 'text-canvas-900 dark:text-canvas-50'}`}>
                            {cell}
                          </Text>
                        </View>
                      );
                    })}
                    {canWrite && (
                      <View className="w-28 items-end">
                        <ActionButton
                          label="Edit"
                          variant="tertiary"
                          onPress={() =>
                            setEditingCell({
                              rowIndex: actualRowIndex,
                              colIndex: row.length - 1,
                              value: row[row.length - 1] ?? '',
                            })
                          }
                        />
                      </View>
                    )}
                  </View>
                );
              })}
            </View>
          )}
        </SurfaceCard>
      ) : (
        <EmptyState
          title="No spreadsheet range loaded"
          description="Enter a Spreadsheet ID and cell range above, then select Load Records to inspect, sort, or edit cells."
          actionLabel="Load Default Range"
          onAction={handleLoadRange}
        />
      )}
    </View>
  );
}
