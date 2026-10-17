import { breadcrumbs, Breadcrumb } from './breadcrumbs';

export interface SupportBundle {
  readonly appVersion: string;
  readonly platform: string;
  readonly generatedAtUtc: string;
  readonly breadcrumbs: readonly Breadcrumb[];
}

export function generateSupportBundle(): SupportBundle {
  return {
    appVersion: '1.0.0',
    platform: 'react-native-windows',
    generatedAtUtc: new Date().toISOString(),
    breadcrumbs: breadcrumbs.getAll(),
  };
}
