# Lembrete de Atualização de Jogos

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Uma ferramenta para registrar e acompanhar o progresso de jogos cuja atualização leva muito tempo. Originalmente criada para **Boom Beach**.

## Recursos

- 🕒 Acompanhar tarefas de atualização em várias contas
- ⏰ Diferente de calendário/alarme, a contagem regressiva é sincronizada com o jogo, evitando o cálculo manual do tempo a cada vez
- 🔔 Notificação do sistema quando a atualização for concluída
- ♻️ Tarefas recorrentes: diárias / semanais / mensais / anuais / personalizadas; hora de término opcional (padrão: nenhuma); suporta regras de pulo
- 🌐 Suporta 27 idiomas

## Requisitos do Sistema

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) ou superior
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) ou superior

> Não sei se outras versões funcionarão :<

## Instalação

1. Baixe a versão mais recente na página [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Extraia em qualquer pasta
3. Execute `Game Upgrade Reminder.exe`

## Guia de Uso

### Adicionar tarefa de atualização

1. Selecione a conta no topo da interface
2. Selecione ou crie um nome de tarefa (opcional)
3. Configure o tempo necessário: hora de início, dias, horas, minutos (se não definir a hora de início, o padrão será a hora atual do sistema)
4. Clique no botão "Adicionar" para criar a tarefa

### Gerenciar tarefas

- As tarefas que chegarem ao prazo serão destacadas; clique em "Concluir" para marcá-las como concluídas
- As tarefas podem ser removidas da lista e a remoção pode ser desfeita em até três segundos

## Perguntas Frequentes

### Não recebo notificações do sistema

- Desative o **Assistente de Concentração (Focus Assist)** ou adicione `Game Upgrade Reminder.exe` à lista de prioridade. Se a regra automática estiver configurada como "Somente alarmes", altere para "Somente prioridade".
- Caso contrário, não sei

### Outros problemas estranhos

- Provavelmente é um bug, apenas ignore
- Pode ser relatado na página Issues, mas provavelmente não saberei como corrigir

## Licença

Este projeto está licenciado sob a [GNU Affero General Public License v3.0](../LICENSE).