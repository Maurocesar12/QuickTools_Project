# QuickTools — GitHub e Netlify

O site faz parte do repositório principal QuickTools_Project. O arquivo netlify.toml na raiz configura a publicação automaticamente:

- Base directory: website
- Build command: npm run build
- Publish directory: dist (relativo à base website)
- Node.js: 24

## Publicar pelo GitHub

1. Envie netlify.toml e a pasta website ao mesmo repositório GitHub do projeto, incluindo website/downloads.
2. No Netlify, importe um projeto existente do GitHub e selecione esse repositório e a branch desejada.
3. Confira as configurações detectadas e publique.

O build usa apenas os arquivos do repositório. Não depende do SDK .NET, da pasta artifacts nem de caminhos do computador do desenvolvedor. As dez partes binárias em downloads e seu manifest.json precisam estar no Git. dist é gerada e não precisa ser enviada.

## Atualizar o aplicativo

Após gerar uma nova versão em artifacts/QuickTools-v2/ITQuickTools.exe, execute na pasta website:

```powershell
npm run prepare-download
npm run build
```

Envie as alterações de downloads junto com a página. O build confere tamanho e SHA-256 do executável; arquivos ausentes ou alterados impedem a publicação. O navegador verifica cada parte e salva ITQuickTools.exe. Use HTTPS ou localhost, com JavaScript habilitado.

## Prévia local

```powershell
npm run build
npm run dev
```

Abra http://127.0.0.1:4173. Sem dependências externas. O download mantém cerca de 155 MiB em memória, além do buffer de salvamento.
