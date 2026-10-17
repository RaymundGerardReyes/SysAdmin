import * as fs from 'fs';
import * as path from 'path';
import { PERMISSIONS } from '../../src/contracts/permissions';
import { ERROR_CODES } from '../../src/contracts/errors/errorCodes';
import { JOB_STATUSES } from '../../src/contracts/jobStatus';
import { COMMANDS } from '../../src/contracts/commands';

describe('Contracts Drift Verification', () => {
  const snapshotsDir = path.resolve(__dirname, '../../contracts-snapshots');

  test('permissions contract matches snapshot exactly', () => {
    const raw = fs.readFileSync(path.join(snapshotsDir, 'permissions.snapshot.json'), 'utf8');
    const snapshot = JSON.parse(raw) as string[];

    expect([...PERMISSIONS]).toEqual(snapshot);
    expect(PERMISSIONS.length).toBe(snapshot.length);
  });

  test('errorCodes contract matches snapshot exactly', () => {
    const raw = fs.readFileSync(path.join(snapshotsDir, 'errorCodes.snapshot.json'), 'utf8');
    const snapshot = JSON.parse(raw) as string[];

    expect([...ERROR_CODES]).toEqual(snapshot);
    expect(ERROR_CODES.length).toBe(snapshot.length);
  });

  test('jobStatus contract matches snapshot exactly', () => {
    const raw = fs.readFileSync(path.join(snapshotsDir, 'jobStatus.snapshot.json'), 'utf8');
    const snapshot = JSON.parse(raw) as string[];

    expect([...JOB_STATUSES]).toEqual(snapshot);
    expect(JOB_STATUSES.length).toBe(snapshot.length);
  });

  test('commands contract descriptors match snapshot exactly', () => {
    const raw = fs.readFileSync(path.join(snapshotsDir, 'commands.snapshot.json'), 'utf8');
    const snapshot = JSON.parse(raw) as any[];

    expect([...COMMANDS]).toEqual(snapshot);
    expect(COMMANDS.length).toBe(snapshot.length);
  });
});
