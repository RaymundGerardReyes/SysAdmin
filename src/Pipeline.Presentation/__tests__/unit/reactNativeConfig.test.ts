// Rules: R-01 React Native CLI as primary runner, B-01 Windows platform command registration
import * as path from 'path';

interface CliCommand {
  name: string;
  func: unknown;
  description?: string;
}

interface PlatformConfig {
  npmPackageName?: string;
  projectConfig: (projectRoot: string, userConfig?: unknown) => unknown;
  dependencyConfig: (dependencyRoot: string, userConfig?: unknown) => unknown;
  linkConfig?: () => unknown;
}

interface ReactNativeProjectConfig {
  commands: CliCommand[];
  platforms: {
    windows?: PlatformConfig;
    [key: string]: unknown;
  };
}

describe('React Native CLI and Windows Configuration', () => {
  const rootDir = path.resolve(__dirname, '../../');
  const configPath = path.join(rootDir, 'react-native.config.js');

  it('[R-01] loads react-native.config.js without throwing errors', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const config = require(configPath) as ReactNativeProjectConfig;
    expect(config).toBeDefined();
    expect(Array.isArray(config.commands)).toBe(true);
  });

  it('[R-01] registers the start and bundle commands from @react-native/community-cli-plugin', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const config = require(configPath) as ReactNativeProjectConfig;
    const commandNames = config.commands.map((cmd) => cmd.name);

    expect(commandNames).toContain('start');
    expect(commandNames).toContain('bundle');

    const startCmd = config.commands.find((cmd) => cmd.name === 'start');
    expect(startCmd).toBeDefined();
    expect(typeof startCmd?.func).toBe('function');
    expect(startCmd?.description).toBeDefined();

    const bundleCmd = config.commands.find((cmd) => cmd.name === 'bundle');
    expect(bundleCmd).toBeDefined();
    expect(typeof bundleCmd?.func).toBe('function');
    expect(bundleCmd?.description).toBeDefined();
  });

  it('[B-01] registers the run-windows command and windows platform from @react-native-windows/cli', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const config = require(configPath) as ReactNativeProjectConfig;
    const commandNames = config.commands.map((cmd) => cmd.name);

    expect(commandNames).toContain('run-windows');

    const runWindowsCmd = config.commands.find((cmd) => cmd.name === 'run-windows');
    expect(runWindowsCmd).toBeDefined();
    expect(typeof runWindowsCmd?.func).toBe('function');
    expect(runWindowsCmd?.description).toBeDefined();

    expect(config.platforms).toBeDefined();
    expect(config.platforms.windows).toBeDefined();
    expect(config.platforms.windows?.npmPackageName).toBe('react-native-windows');
    expect(typeof config.platforms.windows?.projectConfig).toBe('function');
    expect(typeof config.platforms.windows?.dependencyConfig).toBe('function');
  });

  it('[B-01] resolves @react-native-windows/find-dotnet-tools and findPowerShell returns a valid path', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const findDotnetTools = require('@react-native-windows/find-dotnet-tools');
    expect(typeof findDotnetTools.findPowerShell).toBe('function');
    expect(typeof findDotnetTools.getNugetGlobalPackagesFolder).toBe('function');

    const powershellPath = findDotnetTools.findPowerShell();
    expect(typeof powershellPath).toBe('string');
    expect(powershellPath.length).toBeGreaterThan(0);

    // Also test direct canonical entry point findDotnetTools.js
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const canonicalTools = require('@react-native-windows/find-dotnet-tools/lib-commonjs/findDotnetTools');
    expect(typeof canonicalTools.findPowerShell).toBe('function');
    expect(typeof canonicalTools.getNugetGlobalPackagesFolder).toBe('function');
    expect(canonicalTools.findPowerShell()).toBe(powershellPath);
  });
});
