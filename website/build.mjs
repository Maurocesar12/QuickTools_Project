import {mkdir,readFile,writeFile,copyFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const root = path.dirname(fileURLToPath(import.meta.url));
const output = path.join(root, 'dist');
const source = path.join(root, 'downloads');
const manifest = JSON.parse(await readFile(path.join(source, 'manifest.json'), 'utf8'));
await mkdir(path.join(output, 'downloads'), {recursive:true});
const fullHash = createHash('sha256');
let size = 0;
for (const part of manifest.parts) {
  if (!/^quicktools-\d+\.bin$/.test(part.file)) throw new Error('Nome de parte inválido');
  const bytes = await readFile(path.join(source, part.file));
  if (bytes.length !== part.bytes || createHash('sha256').update(bytes).digest('hex') !== part.sha256) throw new Error(`Arquivo inválido: ${part.file}`);
  size += bytes.length;
  fullHash.update(bytes);
  await copyFile(path.join(source, part.file), path.join(output, 'downloads', part.file));
}
if (size !== manifest.bytes || fullHash.digest('hex') !== manifest.sha256) throw new Error('Executável incompleto ou alterado');
await writeFile(path.join(output, 'downloads', 'manifest.json'), JSON.stringify(manifest, null, 2));
for (const file of ['index.html','style.css','download.js','icon.svg']) await copyFile(path.join(root,file),path.join(output,file));
console.log(`Build concluído: página e executável verificados (${size} bytes).`);
