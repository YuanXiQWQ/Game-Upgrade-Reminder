# Recordatorio de actualización de juegos

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Una herramienta para registrar y hacer un seguimiento del progreso de juegos cuya mejora requiere mucho tiempo. Originalmente creada para **Boom Beach**.

## Características

- 🕒 Seguimiento de tareas de mejora en múltiples cuentas
- ⏰ A diferencia de calendarios/alarma, la cuenta regresiva se sincroniza con el juego, evitando calcular el tiempo manualmente cada vez
- 🔔 Notificación del sistema al completar la mejora
- ♻️ Tareas recurrentes: diarias / semanales / mensuales / anuales / personalizadas; tiempo de finalización opcional (por defecto: ninguno); admite reglas de omisión
- 🌐 Soporta 27 idiomas

## Requisitos del sistema

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) o superior
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) o superior

> No sé si funcionará en otras versiones :<

## Instalación

1. Descarga la última versión desde la página de [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Extrae en cualquier carpeta
3. Ejecuta `Game Upgrade Reminder.exe`

## Uso

### Añadir tarea de mejora

1. Selecciona la cuenta en la parte superior de la interfaz
2. Elige o crea un nombre de tarea (opcional)
3. Configura el tiempo requerido: hora de inicio, días, horas, minutos (si no se establece hora de inicio, por defecto se usará la hora actual del sistema)
4. Haz clic en el botón "Añadir" para crear la tarea

### Gestionar tareas

- Las tareas que lleguen a su hora se resaltarán; haz clic en "Completar" para marcarlas como terminadas
- Las tareas pueden eliminarse de la lista y la eliminación puede deshacerse en los tres segundos siguientes

## Preguntas frecuentes

### No recibo notificaciones del sistema

- Desactiva **Asistente de concentración (Focus Assist)**, o añade `Game Upgrade Reminder.exe` a la lista de prioridades. Si la regla automática del asistente está configurada en "Solo alarmas", cámbiala a "Solo prioridad".
- Si no, no lo sé

### Otros problemas extraños

- Probablemente sea un bug, simplemente ignóralo
- Puedes reportarlo en la página de Issues, pero probablemente no sabré cómo solucionarlo

## Licencia

Este proyecto está bajo la licencia [GNU Affero General Public License v3.0](../LICENSE).