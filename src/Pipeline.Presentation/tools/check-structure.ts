import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

function main() {
  console.log('[check-structure] Verifying folder grammar, manifests, and per-folder READMEs...');
  const currentDir = typeof __dirname !== 'undefined' ? __dirname : path.dirname(fileURLToPath(import.meta.url));
  const srcDir = path.resolve(currentDir, '..', 'src');

  const requiredReadmes = [
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

  for (const folder of requiredReadmes) {
    const readmePath = path.join(srcDir, folder, 'README.md');
    if (!fs.existsSync(readmePath)) {
      console.error(`[STRUCTURE] Missing required README.md at ${readmePath}`);
      process.exit(1);
    }
  }

  // Check features have manifests
  const featuresDir = path.join(srcDir, 'features');
  const entries = fs.readdirSync(featuresDir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.isDirectory()) {
      const manifestPath = path.join(featuresDir, entry.name, 'feature.manifest.ts');
      if (!fs.existsSync(manifestPath)) {
        console.error(`[STRUCTURE] Feature '${entry.name}' is missing feature.manifest.ts`);
        process.exit(1);
      }
    }
  }

  console.log('[check-structure] SUCCESS: Codebase folder structure and grammar verified.');
}

main();
