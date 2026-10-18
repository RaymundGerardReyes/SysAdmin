/**
 * React Native CLI configuration for Pipeline.Presentation
 * Rules: R-01, B-01
 *
 * Explicitly registers:
 * 1. React Native core packager & bundling commands ('start', 'bundle') from @react-native/community-cli-plugin
 * 2. React Native Windows commands ('run-windows', 'autolink-windows', etc.) & platforms.windows from @react-native-windows/cli
 */

let communityCommands = [];
try {
  const communityCliPlugin = require('@react-native/community-cli-plugin');
  if (communityCliPlugin.startCommand) {
    communityCommands.push(communityCliPlugin.startCommand);
  }
  if (communityCliPlugin.bundleCommand) {
    communityCommands.push(communityCliPlugin.bundleCommand);
  }
} catch (e) {
  // Graceful fallback
}

let windowsCommands = [];
let windowsPlatform = undefined;
try {
  const windowsCli = require('@react-native-windows/cli');
  if (Array.isArray(windowsCli.commands)) {
    windowsCommands.push(...windowsCli.commands);
  }
  windowsPlatform = {
    npmPackageName: 'react-native-windows',
    projectConfig: windowsCli.projectConfig,
    dependencyConfig: windowsCli.dependencyConfig,
    linkConfig: () => null,
  };
} catch (e) {
  // Graceful fallback
}

module.exports = {
  commands: [
    ...communityCommands,
    ...windowsCommands,
  ],
  platforms: {
    ...(windowsPlatform ? { windows: windowsPlatform } : {}),
  },
};
