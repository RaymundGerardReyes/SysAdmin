/**
 * Build & Semantic Versioning Information for Pipeline.Presentation
 * Rules: L-01, L-02
 */

export interface BuildInfo {
  readonly component: 'frontend';
  readonly version: string;
  readonly tagPrefix: string;
  readonly targetApiVersion: string;
  readonly buildDate: string;
  readonly platform: 'react-native-windows-ios';
}

export const BUILD_INFO: BuildInfo = {
  component: 'frontend',
  version: '1.0.0',
  tagPrefix: 'frontend-v',
  targetApiVersion: '1.0.0',
  buildDate: '2026-10-17',
  platform: 'react-native-windows-ios',
} as const;

