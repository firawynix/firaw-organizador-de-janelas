# DLLs nativas no MSIX

Entry: `tools/msix/build-store.ps1`.

- `packaging/windows/msix/window-manager.json`: payload era arquivo EXE.
- Store 1.0.0.0: app contem somente EXE 0.3.2.0; hash identico ao release
  local. Cinco DLLs nativas presentes no release ficaram fora do pacote.
- Evento Application/.NET Runtime 1026, 15/10/2026 14:40:02:
  `DllNotFoundException`, `NativeMethodsSetLastError.SetWindowLongPtrWndProc`.
- `Program.cs:Main()` abre `OrganizerWindow`, nao inicia so na bandeja.
- `tools/msix/build-msix.ps1`: copia diretorio inteiro; confere cinco DLLs
  configuradas e FileVersion antes de MakeAppx.
- Store 1.0.1.0: versao por propriedades de publish; site fica 0.3.2.
- Falta teste de abertura do MSIX corrigido; build nao prova execucao.
- Nao alterar pacote Microsoft assinado nem substitui-lo no servidor.

Fonte: https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/

Updated: 2026-10-15
