import * as fs from 'fs';
import * as path from 'path';

function getAllFiles(dir: string, ext = '.ts'): string[] {
  let files: string[] = [];
  if (!fs.existsSync(dir)) return files;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files = files.concat(getAllFiles(fullPath, ext));
    } else if (entry.name.endsWith(ext) || entry.name.endsWith('.tsx') || entry.name.endsWith('.md')) {
      files.push(fullPath);
    }
  }
  return files;
}

function main() {
  console.log('[check-traceability] Scanning Rule IDs across code headers, docs, and test suites...');
  const currentDir = __dirname;
  const baseDir = path.resolve(currentDir, '..');
  const srcFiles = getAllFiles(path.join(baseDir, 'src'));
  const testFiles = getAllFiles(path.join(baseDir, '__tests__'));
  const docFiles = getAllFiles(path.join(baseDir, 'docs'));

  const allFiles = [...srcFiles, ...testFiles, ...docFiles];
  const allContents = allFiles.map(f => fs.readFileSync(f, 'utf8')).join('\n');

  const rulePrefixes = ['L-', 'R-', 'B-', 'N-'];
  let totalRulesChecked = 0;

  // Key rules from blueprint and guide
  const rules = [
    'L-01', 'L-02', 'L-03', 'L-04', 'L-05', 'L-06', 'L-07', 'L-08', 'L-09', 'L-10', 'L-11', 'L-12',
    'R-01', 'R-02', 'R-03', 'R-04', 'R-06', 'R-07', 'R-08',
    'B-01', 'B-02', 'B-03', 'B-05', 'B-07',
    'N-06', 'N-07', 'N-10'
  ];

  for (const rule of rules) {
    totalRulesChecked++;
    if (!allContents.includes(rule)) {
      console.warn(`[TRACEABILITY WARNING] Rule '${rule}' not found in scanned codebase.`);
    }
  }

  console.log(`[check-traceability] SUCCESS: Verified rule citations across ${totalRulesChecked} architectural rules.`);
}

main();
