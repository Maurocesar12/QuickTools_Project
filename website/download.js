const button = document.getElementById('download');
const label = document.getElementById('button-label');
const status = document.getElementById('download-status');
const progress = document.getElementById('progress');

button.addEventListener('click', async () => {
  button.disabled = true;
  progress.hidden = false;
  progress.value = 0;
  status.classList.remove('error');
  label.textContent = 'Preparando download…';
  status.textContent = 'Mantenha esta página aberta até o download terminar.';
  try {
    const response = await fetch('downloads/manifest.json');
    if (!response.ok) throw new Error('Download indisponível. Tente novamente em instantes.');
    const manifest = await response.json();
    const parts = [];
    let received = 0;
    for (const part of manifest.parts) {
      const response = await fetch(`downloads/${part.file}`);
      if (!response.ok) throw new Error('A conexão foi interrompida. Tente baixar novamente.');
      const bytes = await response.arrayBuffer();
      if (bytes.byteLength !== part.bytes) throw new Error('Download incompleto. Tente novamente.');
      const digest = await crypto.subtle.digest('SHA-256', bytes);
      const hash = Array.from(new Uint8Array(digest), b => b.toString(16).padStart(2, '0')).join('');
      if (hash !== part.sha256) throw new Error('Não foi possível verificar o arquivo. Tente novamente.');
      parts.push(bytes);
      received += bytes.byteLength;
      progress.value = Math.round(received / manifest.bytes * 100);
      label.textContent = `Baixando… ${progress.value}%`;
    }
    const blob = new Blob(parts, {type: 'application/octet-stream'});
    if (blob.size !== manifest.bytes) throw new Error('Download incompleto. Tente novamente.');
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'ITQuickTools.exe';
    document.body.append(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 60000);
    label.textContent = 'Baixar novamente';
    status.textContent = 'Arquivo pronto. Confira os downloads do navegador e abra ITQuickTools.exe.';
  } catch (error) {
    label.textContent = 'Tentar novamente';
    status.classList.add('error');
    status.textContent = error instanceof TypeError ? 'Não foi possível conectar. Confira sua internet e tente novamente.' : error.message;
  } finally {
    button.disabled = false;
  }
});
