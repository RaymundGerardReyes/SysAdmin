import * as fs from 'fs';
import * as path from 'path';

describe('Frontend Architectural Boundaries', () => {
  const srcRoot = path.resolve(__dirname, '../../src');

  function getAllFiles(dir: string, extFilter: string[]): string[] {
    const results: string[] = [];
    if (!fs.existsSync(dir)) return results;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        results.push(...getAllFiles(fullPath, extFilter));
      } else if (extFilter.some(ext => entry.name.endsWith(ext))) {
        results.push(fullPath);
      }
    }
    return results;
  }

  function getImports(filePath: string): string[] {
    const content = fs.readFileSync(filePath, 'utf8');
    const importRegex = /(?:import|export)\s+(?:[\s\S]*?from\s+)?['"]([^'"]+)['"]/g;
    const imports: string[] = [];
    let match;
    while ((match = importRegex.exec(content)) !== null) {
      if (match[1]) {
        imports.push(match[1]);
      }
    }
    return imports;
  }

  function importsLayer(imp: string, layer: string): boolean {
    const regex = new RegExp(`(^|\\/)${layer}(\\/|$)`);
    return regex.test(imp);
  }

  test('Contracts layer (L-02) does not import any higher concentric layer', () => {
    const contractsDir = path.join(srcRoot, 'contracts');
    const files = getAllFiles(contractsDir, ['.ts', '.tsx']);
    const forbiddenLayers = ['core', 'bridge', 'ui', 'state', 'security', 'observability', 'intent', 'platform', 'features', 'app'];

    for (const file of files) {
      const imports = getImports(file);
      for (const imp of imports) {
        for (const forbidden of forbiddenLayers) {
          expect(importsLayer(imp, forbidden)).toBe(false);
        }
      }
    }
  });

  test('Core layer (L-03) does not import bridge, state, ui, features, or app', () => {
    const coreDir = path.join(srcRoot, 'core');
    const files = getAllFiles(coreDir, ['.ts', '.tsx']);
    const forbiddenLayers = ['bridge', 'ui', 'state', 'platform', 'features', 'app'];

    for (const file of files) {
      const imports = getImports(file);
      for (const imp of imports) {
        for (const forbidden of forbiddenLayers) {
          expect(importsLayer(imp, forbidden)).toBe(false);
        }
      }
    }
  });

  test('Bridge layer (L-04) does not import ui, state, intent, platform, features, or app', () => {
    const bridgeDir = path.join(srcRoot, 'bridge');
    const files = getAllFiles(bridgeDir, ['.ts', '.tsx']);
    const forbiddenLayers = ['ui', 'state', 'platform', 'features', 'app'];

    for (const file of files) {
      const imports = getImports(file);
      for (const imp of imports) {
        for (const forbidden of forbiddenLayers) {
          expect(importsLayer(imp, forbidden)).toBe(false);
        }
      }
    }
  });

  test('No cross-feature imports (R-04: features/A must not import features/B)', () => {
    const featuresDir = path.join(srcRoot, 'features');
    if (!fs.existsSync(featuresDir)) return;

    const featureDirs = fs.readdirSync(featuresDir, { withFileTypes: true })
      .filter(d => d.isDirectory())
      .map(d => d.name);

    for (const currentFeature of featureDirs) {
      const currentFeaturePath = path.join(featuresDir, currentFeature);
      const files = getAllFiles(currentFeaturePath, ['.ts', '.tsx']);

      for (const otherFeature of featureDirs) {
        if (otherFeature === currentFeature) continue;

        for (const file of files) {
          const imports = getImports(file);
          for (const imp of imports) {
            const forbiddenImport = `features/${otherFeature}`;
            expect(imp.includes(forbiddenImport)).toBe(false);
          }
        }
      }
    }
  });

  test('One-bridge-door (R-01, B-01): Only features/*/api and app may import bridge', () => {
    const files = getAllFiles(srcRoot, ['.ts', '.tsx']);

    for (const file of files) {
      const normalizedPath = file.replace(/\\/g, '/');
      const isBridgeModule = normalizedPath.includes('/src/bridge/');
      const isFeatureApi = /\/src\/features\/[^/]+\/api\//.test(normalizedPath);
      const isAppModule = normalizedPath.includes('/src/app/');
      const isTestOrTool = normalizedPath.includes('__tests__') || normalizedPath.includes('tools');

      const allowedToImportBridge = isBridgeModule || isFeatureApi || isAppModule || isTestOrTool;

      const imports = getImports(file);
      const importsBridge = imports.some(imp => importsLayer(imp, 'bridge'));

      if (importsBridge && !allowedToImportBridge) {
        throw new Error(`File ${normalizedPath} violates One-Bridge-Door by importing bridge`);
      }
      expect(importsBridge ? allowedToImportBridge : true).toBe(true);
    }
  });

  test('Every feature has a feature.manifest.ts', () => {
    const featuresDir = path.join(srcRoot, 'features');
    const entries = fs.readdirSync(featuresDir, { withFileTypes: true });

    for (const entry of entries) {
      if (entry.isDirectory()) {
        const manifest = path.join(featuresDir, entry.name, 'feature.manifest.ts');
        expect(fs.existsSync(manifest)).toBe(true);
      }
    }
  });

  test('All required concentric layer folders have a README.md', () => {
    const requiredLayers = [
      'contracts',
      'core',
      'bridge',
      'state',
      'security',
      'observability',
      'intent',
      'ui',
      'platform',
      'features',
      'app'
    ];

    for (const layer of requiredLayers) {
      const readmePath = path.join(srcRoot, layer, 'README.md');
      expect(fs.existsSync(readmePath)).toBe(true);
    }
  });
});
