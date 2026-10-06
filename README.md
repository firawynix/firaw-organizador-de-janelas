# Firawynix Organizador de Janelas

Aplicativo independente para Windows que salva uma área e um critério para cada tipo de janela de um programa. A interface usa o mesmo visual escuro e ciano do Firaw SnapCopyText.

- [Site público de demonstração](https://lab.firawynix.com.br/foj/)
- [Download para Windows x64](https://github.com/firawynix/firaw-organizador-de-janelas/releases/download/v0.3.2/FirawynixWindowManager.exe)
- [Ícone do menu Produtos](assets/foj-product-menu.png) em PNG monocromático transparente de 128 × 128 px

O ícone exclusivo do organizador é um nó celta ciano de quatro laços, sem fundo, aplicado ao executável, às janelas e à área de notificação. Os arquivos usados ficam em `assets/celtic-knot-transparent.png` e `assets/celtic-knot-transparent.ico`.

## Como usar

1. Abra `FirawynixWindowManager.exe`.
2. Clique em **Capturar janela aberta** para escolher uma janela ou **Escolher arquivo .exe** para cadastrar um programa que está fechado.
3. Crie a primeira regra. **Desenhar área** permite arrastar um retângulo na tela; a área é salva em proporção à área útil do monitor, para se adaptar a mudanças de resolução.
4. Use **Nova regra** para janelas especiais. É possível reconhecer todas as janelas, o mesmo tipo de uma janela capturada ou um trecho do título.
5. Escolha **Tela cheia no monitor** quando a janela especial deve cobrir o monitor inteiro.

O botão **Monitores e nomes** abre uma lista com número, resolução e indicação do monitor principal. Clique em **Identificar na tela** para ver o número sobre cada monitor físico. Selecione um monitor e clique em **Renomear** para salvar um nome como “Mesa”, “Vertical” ou “TV”. O nome aparece nas regras sem alterar a identificação técnica usada para posicionar as janelas.

Regras específicas têm prioridade sobre a regra geral. Ao fechar a janela principal, o organizador continua na área de notificação. O menu do ícone permite reabrir ou sair. O botão **Pausar** suspende a aplicação de novas regras.

## Exemplo: Microsoft Teams

1. Com o Teams aberto, capture sua janela principal e desenhe a área padrão.
2. Durante uma reunião em que alguém compartilhou conteúdo, use **Abrir em nova janela** no Teams para destacar o conteúdo.
3. No organizador, selecione o perfil do Teams e clique em **Nova regra**. Capture a janela de conteúdo destacada.
4. Se ela pertencer a outro executável, confirme a associação ao Teams. Para processos comuns do WebView2, a associação só é aplicada quando o processo pertence à árvore do Teams.
5. Escolha **Título contém** e use um trecho estável do título dessa janela. Se a janela tiver uma classe exclusiva, é possível usar **Mesma classe**. Escolha **Tela cheia no monitor** e selecione o monitor.

O Teams pode mudar títulos e estrutura entre versões e idiomas. Configure a regra com a janela de apresentação real aberta para conferir qual critério a distingue. Se o conteúdo continuar dentro da mesma janela da reunião, destaque-o primeiro; o organizador não consegue mover apenas uma parte interna de uma janela.

## Dados e limitações

As regras ficam em `%LOCALAPPDATA%\Firawynix\WindowManager\profiles.json` e os nomes de monitores em `monitors.json` na mesma pasta. O aplicativo não envia esses dados para a rede. A versão atual atua sobre janelas independentes visíveis do desktop. Alguns jogos, janelas com privilégios elevados ou aplicativos que impõem sua própria posição podem ignorar a mudança. Ao sair do organizador, ele restaura a moldura das janelas que colocou em tela cheia.

O aplicativo precisa estar em execução para aplicar as regras quando novas janelas abrirem. Ele não se registra automaticamente na inicialização do Windows.

## Compilar

Requer o SDK .NET 10 no Windows:

```powershell
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o release
```

`--preview`, `--preview-rule`, `--preview-monitor` e `--preview-window` geram imagens locais das telas. `--self-check` verifica posicionamento em uma janela temporária invisível e grava `self-check.json` ao lado do executável.
