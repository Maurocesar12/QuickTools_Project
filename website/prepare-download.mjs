import {mkdir,readFile,writeFile,copyFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const root = path.dirname(fileURLToPath(import.meta.url));
const output = root;
const binary = await readFile(path.resolve(root, '../artifacts/QuickTools-v2/ITQuickTools.exe'));
await mkdir(path.join(output,'downloads'),{recursive:true});
// Static hosting assets stay under 25 MiB; the browser verifies and assembles the original executable.
const manifest = {name:'ITQuickTools.exe',bytes:binary.length,sha256:createHash('sha256').update(binary).digest('hex'),parts:[]};
const chunkSize = 16 * 1024 * 1024;
for(let offset=0,index=0;offset<binary.length;offset+=chunkSize,index++) {
  const chunk = binary.subarray(offset,offset+chunkSize);
  const file = `quicktools-${String(index).padStart(2,'0')}.bin`;
  await writeFile(path.join(output,'downloads',file),chunk);
  manifest.parts.push({file,bytes:chunk.length,sha256:createHash('sha256').update(chunk).digest('hex')});
}
await writeFile(path.join(output,'downloads','manifest.json'),JSON.stringify(manifest,null,2));
console.log(`Página pronta: ${manifest.parts.length} partes; ${binary.length} bytes; SHA-256 ${manifest.sha256}`);
