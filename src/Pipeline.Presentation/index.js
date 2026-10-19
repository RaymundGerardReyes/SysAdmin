/**
 * Root Application Entry Point for Pipeline Presentation
 * Rules: L-01 Concentric layers, R-01 One bridge door
 */
import './src/global.css';

import { AppRegistry } from 'react-native';
import { AppBootstrap } from './src/app/AppBootstrap';

AppRegistry.registerComponent('PipelineApp', () => AppBootstrap);
