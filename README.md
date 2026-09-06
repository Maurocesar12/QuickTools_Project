# IT QuickTools

Central de suporte para Windows, em C# e Windows Forms (.NET 8). Interface em português com tema escuro, menu lateral, cartões de visão geral e busca de ferramentas (`Ctrl+K`).

## Recursos

| Área | Funcionalidades |
| --- | --- |
| Visão geral | Tempo ligado, espaço disponível no disco do Windows e tarefas pendentes. Atualização a cada 30 segundos. |
| Rede | Ping contínuo com resumo, consulta DNS, teste TCP com limite de 5 segundos, adaptadores/IP/gateway/DNS, limpeza de DNS, reset de rede e exportação de log. |
| Sistema | Inventário WMI de hardware, Windows, discos e rede; exportação em TXT. |
| Limpeza | Análise de temporários com mais de 24 horas; exclusão somente dos itens analisados que continuam elegíveis; contagem de arquivos e bytes efetivamente removidos. |
| Manutenção | SFC, DISM e reinício do serviço de impressão, com saída na interface e verificação do código de saída. O reinício preserva a fila de impressão. |
| Ferramentas | Atalhos para aplicativos, dispositivos, tarefas, serviços, eventos, acesso remoto, Windows Update, captura e calculadora; relatório de bateria, chave OEM via WMI e SHA-256 de arquivos. |
| Meu espaço | Checklist e notas com salvamento automático e backup da versão anterior. |

## Executar

A versão portátil gerada localmente está em `artifacts/QuickTools-v2/ITQuickTools.exe`. Execute esse arquivo; ele inclui o .NET e não exige instalação do runtime. A publicação é para Windows x64.

O programa inicia em modo padrão. Reparos e alterações de rede que precisam de administrador oferecem reabertura com elevação. Depois de reabrir, selecione novamente a operação desejada. Operações que interrompem a rede, reparam o Windows ou apagam temporários exigem confirmação na própria interface.

Notas e tarefas ficam em `%LOCALAPPDATA%/ITQuickTools/workspace.json`; o backup anterior fica em `workspace.json.bak`. Esses dados são locais ao usuário e não acompanham automaticamente o executável em um pen drive. Relatórios são salvos no local escolhido pelo usuário. Não há sincronização entre computadores.

## Desenvolvimento

Pré-requisitos: Windows e SDK .NET 8, com acesso ao NuGet na primeira restauração.

```powershell
dotnet build WinFormsApp1/WinFormsApp1.csproj -c Release
dotnet run --project WinFormsApp1/WinFormsApp1.csproj
```

Para gerar o executável portátil:

```powershell
dotnet publish WinFormsApp1/WinFormsApp1.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/QuickTools-v2
```

Para executar as verificações de regressão, sem pacotes de teste adicionais:

```powershell
dotnet run --project tests/QuickTools.Checks.csproj -c Release
```

Os testes criam seus próprios arquivos e verificam contagem/deduplicação da limpeza, preservação de arquivos recentes/alterados/fora do escopo, persistência Unicode, backup, detecção de JSON corrompido e captura simultânea de stdout/stderr com código de saída diferente de zero. Não executam reparos, reset de rede nem limpeza de temporários reais do usuário.

## Conferência manual

- Redimensionar a janela e conferir as sete páginas em escalas de 100%, 125% e 150%.
- Buscar `servicos`, `hardware` e um termo inexistente; limpar o campo e conferir o retorno das ferramentas.
- Iniciar, parar e reiniciar o ping; testar host inválido e porta indisponível.
- Criar uma tarefa, marcá-la, escrever uma nota e reabrir para conferir persistência.
- Gerar inventário e exportar TXT; verificar arquivo com SHA-256 conhecido.
- Validar reparos, elevação e limpeza em máquina de teste antes de uso operacional.

A disponibilidade de bateria, chave OEM, WMI e atalhos depende do equipamento e da edição do Windows. Falhas de acesso são informadas; resultados de WMI indisponíveis aparecem como `N/A`. A limpeza ignora pastas sem acesso, links e arquivos bloqueados; não esvazia a lixeira nem apaga pastas recursivamente.

Desenvolvido por [Maurocesar12](https://github.com/Maurocesar12).
