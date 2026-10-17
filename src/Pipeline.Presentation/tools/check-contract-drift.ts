import * as fs from 'fs';
import * as path from 'path';

function main() {
  console.log('[check-contract-drift] Verifying contracts against snapshots...');
  const currentDir = __dirname;
  const baseDir = path.resolve(currentDir, '..');
  const snapshotDir = path.join(baseDir, 'contracts-snapshots');
  const contractsDir = path.join(baseDir, 'src', 'contracts');

  const permSnap = JSON.parse(fs.readFileSync(path.join(snapshotDir, 'permissions.snapshot.json'), 'utf8')) as string[];
  const permCode = fs.readFileSync(path.join(contractsDir, 'permissions.ts'), 'utf8');

  for (const perm of permSnap) {
    if (!permCode.includes(`'${perm}'`)) {
      console.error(`[DRIFT] Permission '${perm}' found in snapshot but missing in contracts/permissions.ts`);
      process.exit(1);
    }
  }

  const errSnap = JSON.parse(fs.readFileSync(path.join(snapshotDir, 'errorCodes.snapshot.json'), 'utf8')) as string[];
  const errCode = fs.readFileSync(path.join(contractsDir, 'errors', 'errorCodes.ts'), 'utf8');

  for (const err of errSnap) {
    if (!errCode.includes(`'${err}'`)) {
      console.error(`[DRIFT] ErrorCode '${err}' found in snapshot but missing in contracts/errors/errorCodes.ts`);
      process.exit(1);
    }
  }

  const jobSnap = JSON.parse(fs.readFileSync(path.join(snapshotDir, 'jobStatus.snapshot.json'), 'utf8')) as string[];
  const jobCode = fs.readFileSync(path.join(contractsDir, 'jobStatus.ts'), 'utf8');

  for (const status of jobSnap) {
    if (!jobCode.includes(`'${status}'`)) {
      console.error(`[DRIFT] JobStatus '${status}' found in snapshot but missing in contracts/jobStatus.ts`);
      process.exit(1);
    }
  }

  const cmdSnap = JSON.parse(fs.readFileSync(path.join(snapshotDir, 'commands.snapshot.json'), 'utf8')) as { command: string }[];
  const cmdCode = fs.readFileSync(path.join(contractsDir, 'commands.ts'), 'utf8');

  for (const cmd of cmdSnap) {
    if (!cmdCode.includes(`'${cmd.command}'`)) {
      console.error(`[DRIFT] Command '${cmd.command}' found in snapshot but missing in contracts/commands.ts`);
      process.exit(1);
    }
  }

  console.log('[check-contract-drift] SUCCESS: All 4 contract snapshots are 100% in sync with code.');
}

main();
