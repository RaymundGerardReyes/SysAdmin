export const queryKeys = {
  session: ['session'] as const,
  capabilities: ['capabilities'] as const,
  jobs: {
    all: ['jobs'] as const,
    detail: (id: string) => ['jobs', id] as const,
  },
  sheets: {
    range: (sheetId: string, range: string) => ['sheets', sheetId, range] as const,
  },
};
