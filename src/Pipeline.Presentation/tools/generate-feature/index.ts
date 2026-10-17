import * as fs from 'fs';
import * as path from 'path';

function main() {
  const featureName = process.argv[2];
  if (!featureName) {
    console.log('Usage: npm run gen:feature <feature-name>');
    return;
  }

  const targetDir = path.resolve(__dirname, '..', '..', 'src', 'features', featureName);
  if (fs.existsSync(targetDir)) {
    console.error(`Feature '${featureName}' already exists at ${targetDir}`);
    process.exit(1);
  }

  fs.mkdirSync(targetDir, { recursive: true });
  fs.mkdirSync(path.join(targetDir, 'screens'), { recursive: true });
  fs.mkdirSync(path.join(targetDir, 'components'), { recursive: true });
  fs.mkdirSync(path.join(targetDir, 'hooks'), { recursive: true });
  fs.mkdirSync(path.join(targetDir, 'api'), { recursive: true });

  fs.writeFileSync(path.join(targetDir, 'README.md'), `# Feature: ${featureName}\n\nOwner: @team\n`);
  fs.writeFileSync(path.join(targetDir, 'feature.manifest.ts'), `export const ${featureName}Manifest = { id: '${featureName}', routes: [] };\n`);
  fs.writeFileSync(path.join(targetDir, 'index.ts'), `export * from './feature.manifest';\n`);

  console.log(`[gen:feature] Successfully scaffolded feature '${featureName}'`);
}

main();
