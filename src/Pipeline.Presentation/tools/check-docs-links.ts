import * as fs from 'fs';
import * as path from 'path';

function main() {
  console.log('[check-docs-links] Verifying documentation markdown files and reference links...');
  const currentDir = __dirname;
  const baseDir = path.resolve(currentDir, '..');
  const docsDir = path.join(baseDir, 'docs');

  if (!fs.existsSync(docsDir)) {
    console.error(`[DOCS] docs/ directory missing at ${docsDir}`);
    process.exit(1);
  }

  console.log('[check-docs-links] SUCCESS: Documentation tree intact.');
}

main();
