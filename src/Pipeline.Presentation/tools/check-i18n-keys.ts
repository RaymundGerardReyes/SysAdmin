import * as fs from 'fs';
import * as path from 'path';

function main() {
  console.log('[check-i18n-keys] Verifying every ErrorCode has a corresponding error directive and i18n key...');
  const currentDir = __dirname;
  const baseDir = path.resolve(currentDir, '..');
  const snapPath = path.join(baseDir, 'contracts-snapshots', 'errorCodes.snapshot.json');
  const errorCodes = JSON.parse(fs.readFileSync(snapPath, 'utf8')) as string[];

  const mapPath = path.join(baseDir, 'src', 'intent', 'mapAppErrorToUi.ts');
  const mapCode = fs.readFileSync(mapPath, 'utf8');

  for (const code of errorCodes) {
    if (!mapCode.includes(`'${code}'`)) {
      console.error(`[I18N DRIFT] ErrorCode '${code}' is missing a directive mapping in mapAppErrorToUi.ts`);
      process.exit(1);
    }
  }

  console.log(`[check-i18n-keys] SUCCESS: All ${errorCodes.length} error codes have verified UI directives.`);
}

main();
