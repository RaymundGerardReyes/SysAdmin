const { getDefaultConfig, mergeConfig } = require('@react-native/metro-config');
const { withNativeWind } = require('nativewind/metro');

/**
 * Metro configuration for Pipeline Presentation
 * Rules: R-01 React Native CLI as primary runner
 * https://reactnative.dev/docs/metro
 */
const defaultConfig = getDefaultConfig(__dirname);

const config = {
  transformer: {
    getTransformOptions: async () => ({
      transform: {
        experimentalImportSupport: false,
        inlineRequires: true,
      },
    }),
  },
};

module.exports = withNativeWind(mergeConfig(defaultConfig, config), {
  input: './src/global.css',
  configPath: './tailwind.config.js',
});
