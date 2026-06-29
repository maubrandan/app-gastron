import { copyFileSync, existsSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const outDir = join(root, 'dist', 'frontend', 'browser');
const indexPath = join(outDir, 'index.html');
const notFoundPath = join(outDir, '404.html');

if (!existsSync(indexPath)) {
  console.error('postbuild-demo: index.html no encontrado en', outDir);
  process.exit(1);
}

copyFileSync(indexPath, notFoundPath);
console.log('postbuild-demo: 404.html creado para SPA routing en GitHub Pages');
