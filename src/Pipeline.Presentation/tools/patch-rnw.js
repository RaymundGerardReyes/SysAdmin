/**
 * Automated postinstall patch for React Native Windows toolchain
 *
 * Rules: R-01, B-01
 *
 * Addresses:
 * 1. @react-native-windows/find-dotnet-tools:
 *    Fallback to standard Windows PowerShell (powershell.exe) when PowerShell 7 (pwsh.exe) is absent.
 * 2. @react-native-windows/cli (WindowsStoreAppUtils.psm1):
 *    - In EnableDevMode: On modern Windows 10/11, AllowDevelopmentWithoutDevLicense = 1 indicates
 *      Developer Mode is active. Returns immediately if active, preventing unnecessary and failing
 *      UAC elevation attempts for legacy AllowAllTrustedApps.
 *    - In Invoke-ElevatedScriptBlock: Resolves powershell.exe if pwsh.exe is not found in $PSHOME.
 */

const fs = require('fs');
const path = require('path');

function patchFindDotnetTools() {
  const targetFile = path.resolve(
    __dirname,
    '../node_modules/@react-native-windows/find-dotnet-tools/lib-commonjs/findDotnetTools.js'
  );

  if (!fs.existsSync(targetFile)) {
    console.log('[patch-rnw] @react-native-windows/find-dotnet-tools not found, skipping.');
    return;
  }

  let content = fs.readFileSync(targetFile, 'utf8');

  if (content.includes('where.exe powershell.exe') || content.includes('/* [patch-rnw-dotnet] */')) {
    console.log('[patch-rnw] findDotnetTools.js already patched with powershell fallback.');
    return;
  }

  const unpatchedPattern = /function findPowerShell\(\) \{[\s\S]*?throw new Error\('Unable to find pwsh\.exe[\s\S]*?\}/;

  const patchedImplementation = `/* [patch-rnw-dotnet] */
function findPowerShell() {
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
    console.log('[patch-rnw] Successfully patched findDotnetTools.js with powershell.exe fallback.');
  } else {
    console.warn('[patch-rnw] findPowerShell pattern did not match in findDotnetTools.js.');
  }
}

function patchWindowsStoreAppUtils() {
  const psm1File = path.resolve(
    __dirname,
    '../node_modules/@react-native-windows/cli/src/powershell/WindowsStoreAppUtils.psm1'
  );

  if (!fs.existsSync(psm1File)) {
    console.log('[patch-rnw] WindowsStoreAppUtils.psm1 not found, skipping.');
    return;
  }

  let content = fs.readFileSync(psm1File, 'utf8');

  const patchedEnableDevMode = `function EnableDevmode {
    # [patch-rnw-devmode]
    $RegistryKeyPath = "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock"

    # On Windows 10/11, AllowDevelopmentWithoutDevLicense = 1 is the canonical Developer Mode indicator
    $devMode = Get-ItemProperty -Path $RegistryKeyPath -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
    if ($devMode -ne $null -and $devMode.AllowDevelopmentWithoutDevLicense -eq 1) {
        Write-Verbose "Developer mode is already enabled in registry."
        return
    }

    # Attempt enabling if missing
    if (-not(Test-Path -Path $RegistryKeyPath)) {
        if (!(IsElevated)) {
            $scriptBlock = {
                param($Path)
                New-Item -Path $Path -ItemType Directory -Force | Out-Null
            }
            Invoke-ElevatedScriptBlock -ScriptBlock $scriptBlock -ArgumentList @($RegistryKeyPath)
        }
        else {
            New-Item -Path $RegistryKeyPath -ItemType Directory -Force | Out-Null
        }
    }

    $value = Get-ItemProperty -Path $RegistryKeyPath -Name AllowDevelopmentWithoutDevLicense -ErrorAction SilentlyContinue
    if (($value -eq $null) -or ($value.AllowDevelopmentWithoutDevLicense -ne 1)) {
        if (!(IsElevated)) {
            $scriptBlock = {
                param($Path, $Name, $Value)
                Set-ItemProperty -Path $Path -Name $Name -Value $Value -ErrorAction Stop
            }
            Invoke-ElevatedScriptBlock -ScriptBlock $scriptBlock -ArgumentList @($RegistryKeyPath, 'AllowDevelopmentWithoutDevLicense', 1)
        }
        else {
            Set-ItemProperty -Path $RegistryKeyPath -Name AllowDevelopmentWithoutDevLicense -Value 1 -ErrorAction Stop
        }
    }
}`;

  // Replace the complete function up to the next region comment. This remains
  // stable across RNW security fixes and avoids leaving an old closing brace.
  const enableDevModeStart = content.search(/function EnableDevmode\s*\{/i);
  const nextRegionComment = enableDevModeStart >= 0
    ? content.indexOf('\n#', enableDevModeStart)
    : -1;

  if (enableDevModeStart >= 0 && nextRegionComment >= 0) {
    const lineBreak = content[nextRegionComment] === '\r' ? '\r\n' : '\n';
    content = content.slice(0, enableDevModeStart)
      + patchedEnableDevMode.replace(/\n/g, lineBreak)
      + lineBreak + lineBreak
      + content.slice(nextRegionComment + 1);
    console.log('[patch-rnw] Patched EnableDevmode in WindowsStoreAppUtils.psm1.');
  } else {
    throw new Error('[patch-rnw] Could not locate the complete EnableDevmode function.');
  }

  // 2. Patch Invoke-ElevatedScriptBlock to fallback from pwsh.exe to powershell.exe
  const pwshExeRegex = /\$process = Start-Process -FilePath "\$PSHOME\\pwsh\.exe"/;
  if (pwshExeRegex.test(content)) {
    const pwshFallback = `$psExe = if (Test-Path "$PSHOME\\pwsh.exe") { "$PSHOME\\pwsh.exe" } else { "$PSHOME\\powershell.exe" }\n        $process = Start-Process -FilePath $psExe`;
    content = content.replace(pwshExeRegex, pwshFallback);
    console.log('[patch-rnw] Patched Invoke-ElevatedScriptBlock in WindowsStoreAppUtils.psm1 with powershell.exe fallback.');
  }

  fs.writeFileSync(psm1File, content, 'utf8');
}

patchFindDotnetTools();
patchWindowsStoreAppUtils();
