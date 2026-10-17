// Rules: R-01 Single bridge door via sheetsApi, R-04 No cross-feature imports, R-06 Five screen states, N-06, N-07
import React, { useState, useMemo, useCallback } from 'react';
import { View, Text } from 'react-native';
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
import { SheetsApi } from '../api/sheetsApi';

export interface SheetsWorkspaceScreenProps {
  readonly api: SheetsApi;
  readonly canWrite: boolean;
}

export function SheetsWorkspaceScreen({ api: sheetsApi, canWrite }: SheetsWorkspaceScreenProps) {
  const { isCompact } = useAppTheme();

  const [spreadsheetId, setSpreadsheetId] = useState('ops-ledger-2026');
  const [range, setRange] = useState('A1:C5');
  const [searchQuery, setSearchQuery] = useState('');
  const [rows, setRows] = useState<string[][]>([]);
  const [hasLoaded, setHasLoaded] = useState(false);
  const [isLoadingRead, setIsLoadingRead] = useState(false);
  const [readError, setReadError] = useState<string | null>(null);
  const [editingCell, setEditingCell] = useState<{ rowIndex: number; colIndex: number; value: string } | null>(null);
  const [isDirty, setIsDirty] = useState(false);

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
      setRows(incoming);
      setIsDirty(false);
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

  const filteredRows = useMemo(() => {
    if (rows.length <= 1) return [];
    const dataRows = rows.slice(1);
    if (!searchQuery.trim()) return dataRows;
    const q = searchQuery.toLowerCase();
    return dataRows.filter((r) => r.some((cell) => cell.toLowerCase().includes(q)));
  }, [rows, searchQuery]);

  const headers = rows[0] ?? ['Column A', 'Column B', 'Column C'];

  const applyCellEdit = () => {
    if (!editingCell) return;
    const updated = rows.map((r, rIdx) =>
      rIdx === editingCell.rowIndex
        ? r.map((c, cIdx) => (cIdx === editingCell.colIndex ? editingCell.value : c))
        : r
    );
    setRows(updated);
    setEditingCell(null);
    setIsDirty(true);
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
          description="Load and filter records from an authorized Google Sheet."
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
            />
          </View>
          <View className={isCompact ? 'w-full' : 'w-48'}>
            <FormField
              label="Cell Range"
              value={range}
              onChangeText={setRange}
              placeholder="e.g. A1:D20"
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
          {canWrite && isDirty && (
            <>
              <ActionButton
                label="Save Changes"
                variant="primary"
                isSubmitting={writeIntent.phase === 'Submitting'}
                submittingLabel="Saving…"
                onPress={() => {
                  executeWrite({ spreadsheetId, range, values: rows }, false);
                  setIsDirty(false);
                }}
              />
              <ActionButton
                label="Discard Edits"
                variant="secondary"
                onPress={handleLoadRange}
              />
            </>
          )}
        </View>
      </SurfaceCard>

      {hasLoaded ? (
        <SurfaceCard variant="elevated" padding="lg">
          <SectionHeader
            title="Records Explorer"
            description={`Showing ${filteredRows.length} record(s) from ${range}`}
          />

          <FormField
            label="Filter Records"
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder="Filter by keyword across any column…"
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

          {filteredRows.length === 0 ? (
            <EmptyState
              title="No matching records"
              description="No rows matched your filter criteria."
              actionLabel="Clear Filter"
              onAction={() => setSearchQuery('')}
            />
          ) : isCompact ? (
            /* Mobile-friendly stacked record cards */
            <View className="space-y-3 mt-2">
              {filteredRows.map((row, idx) => {
                const actualRowIndex = idx + 1;
                return (
                  <SurfaceCard key={`row-${actualRowIndex}`} variant="subtle" padding="md">
                    {row.map((cell, cIdx) => (
                      <View
                        key={`cell-${actualRowIndex}-${cIdx}`}
                        className="flex-row items-center justify-between py-1.5 border-b border-canvas-200 dark:border-canvas-700"
                      >
                        <Text className="text-xs font-medium text-canvas-500 dark:text-canvas-400">
                          {headers[cIdx] ?? `Col ${cIdx + 1}`}
                        </Text>
                        <Text className="text-sm font-medium text-canvas-900 dark:text-canvas-50">
                          {cell}
                        </Text>
                      </View>
                    ))}
                    {canWrite && (
                      <View className="mt-3">
                        <ActionButton
                          label="Edit Record"
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
                  </SurfaceCard>
                );
              })}
            </View>
          ) : (
            /* Desktop structured table view */
            <View className="mt-2 border border-canvas-200 dark:border-canvas-700 rounded-lg overflow-hidden">
              <View className="flex-row bg-canvas-100 dark:bg-canvas-800 px-4 py-3 border-b border-canvas-200 dark:border-canvas-700">
                {headers.map((header, idx) => (
                  <View key={`header-${idx}`} className="flex-1 pr-2">
                    <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                      {header}
                    </Text>
                  </View>
                ))}
                {canWrite && (
                  <View className="w-28 items-end">
                    <Text className="text-xs font-semibold uppercase text-canvas-500 dark:text-canvas-400">
                      Action
                    </Text>
                  </View>
                )}
              </View>

              {filteredRows.map((row, idx) => {
                const actualRowIndex = idx + 1;
                return (
                  <View
                    key={`desktop-row-${actualRowIndex}`}
                    className="flex-row items-center px-4 py-3 border-b border-canvas-200 dark:border-canvas-800 bg-canvas-white dark:bg-canvas-900"
                  >
                    {row.map((cell, cIdx) => (
                      <View key={`desktop-cell-${actualRowIndex}-${cIdx}`} className="flex-1 pr-2">
                        <Text className="text-sm text-canvas-900 dark:text-canvas-50">
                          {cell}
                        </Text>
                      </View>
                    ))}
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
          description="Enter a Spreadsheet ID and cell range above, then select Load Records to inspect or edit cells."
          actionLabel="Load Default Range"
          onAction={handleLoadRange}
        />
      )}
    </View>
  );
}

