/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'contracts-leaf',
      comment: 'Layer 0 contracts must not import any layer above',
      severity: 'error',
      from: { path: '^src/contracts' },
      to: { path: '^src/(core|bridge|ui|state|security|observability|intent|platform|features|app)' }
    },
    {
      name: 'core-leaf',
      comment: 'Layer 1 core must not import layers above',
      severity: 'error',
      from: { path: '^src/core' },
      to: { path: '^src/(bridge|ui|state|security|observability|intent|platform|features|app)' }
    },
    {
      name: 'bridge-boundary',
      comment: 'Layer 2 bridge must only import contracts, core, utils, config',
      severity: 'error',
      from: { path: '^src/bridge' },
      to: { path: '^src/(ui|state|security|observability|intent|platform|features|app)' }
    },
    {
      name: 'no-cross-feature-imports',
      comment: 'features/<a> must never import from features/<b> (R-04)',
      severity: 'error',
      from: { path: '^src/features/([^/]+)' },
      to: {
        path: '^src/features/([^/]+)',
        pathNot: '^src/features/$1'
      }
    },
    {
      name: 'one-bridge-door',
      comment: 'Only features/*/api and app may import bridge (R-01, B-01)',
      severity: 'error',
      from: {
        path: '^src/',
        pathNot: '^src/(features/[^/]+/api|app)'
      },
      to: { path: '^src/bridge' }
    }
  ],
  options: {
    doNotFollow: {
      path: 'node_modules'
    },
    tsPreCompilationDeps: true
  }
};
