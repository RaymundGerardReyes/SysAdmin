/**
 * Automated postinstall patch for @react-native-windows/find-dotnet-tools
 *
 * Rules: R-01, B-01
 *
 * Problem:
 * Microsoft's findPowerShell() executes `where pwsh.exe`. If PowerShell 7 (pwsh.exe)
 * is not installed on the system PATH, it prints stderr noise and throws:
 * "Unable to find pwsh.exe. It should have been made available by yarn install."
 * This unhandled exception crashes @react-native-windows/cli at module load time,
 * causing react-native.config.js to silently drop Windows commands ('run-windows').
 *
 * Solution:
 * Patch findDotnetTools.js to mute stderr on `where` queries and safely fall back
 * to standard Windows PowerShell (powershell.exe), which is universally present on Windows.
 */

const fs = require('fs');
const path = require('path');

const targetFile = path.resolve(
  __dirname,
  '../node_modules/@react-native-windows/find-dotnet-tools/lib-commonjs/findDotnetTools.js'
);

if (!fs.existsSync(targetFile)) {
  console.log('[patch-rnw] @react-native-windows/find-dotnet-tools not found, skipping patch.');
  process.exit(0);
}

let content = fs.readFileSync(targetFile, 'utf8');

if (content.includes('where powershell.exe') || content.includes('powershell.exe')) {
  console.log('[patch-rnw] findDotnetTools.js already patched with powershell fallback.');
  process.exit(0);
}

const unpatchedPattern = /function findPowerShell\(\) \{[\s\S]*?throw new Error\('Unable to find pwsh\.exe[\s\S]*?\}/;

const patchedImplementation = `function findPowerShell() {
    // Build agents already have PowerShell (pwsh) installed
    if (!process.env.TF_BUILD) {
        const nugetPackages = getNugetGlobalPackagesFolder();
        const nugetPwsh = path_1.default.join(nugetPackages, 'PowerShell', '7.6.1', 'tools', 'net10.0', 'any', 'win', 'pwsh.exe');
        if (fs_1.default.existsSync(nugetPwsh)) {
            return nugetPwsh;
        }
    }
    try {
        const found = (0, child_process_1.execSync)('where.exe pwsh.exe', { encoding: 'utf8', stdio: ['pipe', 'pipe', 'ignore'] }).trim();
        if (found) {
            return found.split(/\\r?\\n/)[0];
        }
    }
    catch { }
    try {
        const found = (0, child_process_1.execSync)('where.exe powershell.exe', { encoding: 'utf8', stdio: ['pipe', 'pipe', 'ignore'] }).trim();
        if (found) {
            return found.split(/\\r?\\n/)[0];
        }
    }
    catch { }
    const sysPwsh = path_1.default.join(process.env.SystemRoot || 'C:\\\\Windows', 'System32', 'WindowsPowerShell', 'v1.0', 'powershell.exe');
    if (fs_1.default.existsSync(sysPwsh)) {
        return sysPwsh;
    }
    throw new Error('Unable to find pwsh.exe or powershell.exe.');
}`;

if (unpatchedPattern.test(content)) {
  content = content.replace(unpatchedPattern, patchedImplementation);
  fs.writeFileSync(targetFile, content, 'utf8');
  console.log('[patch-rnw] Successfully patched findDotnetTools.js with silent stderr and powershell.exe fallback.');
} else {
  console.warn('[patch-rnw] findPowerShell pattern did not match. Please verify findDotnetTools.js structure.');
}

