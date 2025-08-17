# SambaClient

Кроссплатформенное приложение на **Avalonia UI** для работы с Samba (SMB) серверами.  
Поддерживает подключение к удалённым ресурсам, просмотр файлов, загрузку и выгрузку.

## 📂 Структура проекта

- **SambaClient.App** — UI-слой на Avalonia, MVVM (ViewModels, Views, сервисы для UI).
- **SambaClient.Core** — модели данных (DTOs, Entities, Responses), исключения.
- **SambaClient.Infrastructure** — реализация сервисов для работы с SMB (подключение, управление файлами).

## 🚀 Возможности
- Подключение к Samba серверу по IP или имени хоста.
- Авторизация с логином и паролем.
- Просмотр содержимого каталогов.
- Загрузка файлов с сервера.
- Выгрузка файлов на сервер.
- Создание новых папок.
- Управление подключениями.

## 🛠️ Технологии
- .NET 8
- Avalonia UI
- MVVM архитектура  
- Сервисы и DI  
- Работа с SMB протоколом  

## ⚙️ Сборка и запуск

### Требования
- .NET 8 SDK
- Поддерживаемая ОС: Windows, Linux, macOS

### Сборка
```bash
git clone https://github.com/yourusername/SambaClient.git
cd SambaClient-main
dotnet build
