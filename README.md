<div align="center">

# 🍵 Анти-кафе | Система управления

**Современное ПО для автоматизации работы анти-кафе**

![Version](https://img.shields.io/badge/version-1.0.0--beta-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-beta-orange)

</div>

---

## 📋 Оглавление

- [Возможности](#-возможности)
- [Технологии](#-технологии)
- [Установка](#-установка)
- [Запуск](#-запуск)
- [Вход в систему](#-вход-в-систему)
- [Структура проекта](#-структура-проекта)
- [Устранение проблем](#-устранение-проблем)
- [Планы по развитию](#-планы-по-развитию)
- [Лицензия](#-лицензия)

---

## 🚀 Возможности

| Модуль | Описание |
|--------|----------|
| **👤 Авторизация** | Безопасный вход с защитой от брутфорса (блокировка после 3 ошибок) |
| **🪑 Сеансы** | Начало/завершение сеансов, автоматический расчёт стоимости по тарифу |
| **📅 Бронирования** | Бронирование столов с проверкой пересечений и занятости |
| **💰 Тарифы** | Гибкая настройка цен по дням недели и времени суток |
| **📊 Отчёты** | Статистика по выручке, загруженности, посещаемости |
| **⚙️ Настройки** | Управление залами, столами, тарифами |

### Безопасность

- 🔐 JWT-аутентификация
- 🔒 BCrypt-хеширование паролей с солью
- 🛡️ Блокировка аккаунта после 3 неудачных попыток входа
- 🚫 Защита API через `[Authorize]` атрибуты

### Интерфейс

- 🌙 Современный Dark Theme
- ✨ Glassmorphism-эффекты
- 📱 Адаптивный дизайн
- 🎨 Единый стиль всех страниц

---

## 🛠️ Технологии

| Компонент | Технология | Версия |
|-----------|------------|--------|
| **Бэкенд** | ASP.NET Core | 8.0 |
| **ORM** | Entity Framework Core | 8.0 |
| **База данных** | SQLite | 3 |
| **Аутентификация** | JWT Bearer | 8.0 |
| **Фронтенд** | Blazor Server | 8.0 |
| **Хеширование** | BCrypt.Net-Next | 4.0.3 |

---

## 📦 Установка

### Системные требования

- **Windows** 10/11
- **.NET 8 Runtime** → [Скачать](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Браузер** (Chrome, Firefox, Edge)

### Сборка проекта

```bash
# 1. Распаковать архив с проектом

# 2. Запустить сборку
build.bat

# 3. Готовый файл появится в папке publish\
```

### Ручная сборка

```bash
# Очистка
dotnet clean

# Восстановление зависимостей
dotnet restore

# Сборка в Release
dotnet build -c Release

# Публикация в один EXE файл
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o publish
```

---

## 🚀 Запуск

### Способ 1. Через start.bat
```bash
# Двойной клик на файл
start.bat
```

### Способ 2. Через EXE
```bash
# Перейти в папку с собранным приложением
cd publish

# Запустить
Anticafe.exe
```

### Способ 3. Через dotnet (для разработки)
```bash
dotnet run
```

### Открыть в браузере

После запуска автоматически откроется браузер или перейдите по адресу:

```
http://localhost:5154/login
```

---

## 🔑 Вход в систему

| Поле | Значение |
|------|----------|
| **Логин** | `admin` |
| **Пароль** | `admin` |

> ⚠️ **Важно!** После первого входа рекомендуется сменить пароль через настройки системы.

---

## 📁 Структура проекта

```
AnticafeSystem/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # Главный макет
│   │   └── NavMenu.razor         # Навигационное меню
│   └── Pages/
│       ├── Bookings.razor        # Страница бронирований
│       ├── Dashboard.razor       # Панель управления
│       ├── Login.razor           # Страница входа
│       ├── Sessions.razor        # Страница сеансов
│       └── Settings.razor        # Страница настроек
├── Controllers/
│   ├── AuthController.cs         # Авторизация
│   ├── BookingController.cs      # Бронирования
│   ├── ReportController.cs       # Отчёты
│   ├── SessionController.cs      # Сеансы
│   ├── SettingsController.cs     # Настройки
│   └── TariffController.cs       # Тарифы
├── Data/
│   ├── ApplicationDbContext.cs   # Контекст БД
│   └── DbInitializer.cs          # Инициализация БД
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs  # Обработка ошибок
│   └── JwtMiddleware.cs                # JWT валидация
├── Models/
│   ├── Booking.cs                # Модель бронирования
│   ├── Room.cs                   # Модель зала
│   ├── Session.cs                # Модель сеанса
│   ├── Table.cs                  # Модель стола
│   ├── Tariff.cs                 # Модель тарифа
│   └── User.cs                   # Модель пользователя
├── Pages/
│   └── _Host.cshtml              # Хост-страница
├── Services/
│   ├── AuthService.cs            # Сервис авторизации
│   ├── BookingService.cs         # Сервис бронирований
│   ├── JwtService.cs             # JWT генерация
│   ├── PricingService.cs         # Расчёт стоимости
│   └── SecurityService.cs        # Безопасность
├── wwwroot/
│   └── css/
│       └── site.css              # Глобальные стили
├── App.razor                     # Корневой компонент
├── _Imports.razor                # Глобальные using
├── appsettings.json              # Конфигурация
├── Program.cs                    # Точка входа
├── Anticafe.csproj               # Файл проекта
├── README.md                     # Документация
├── build.bat                     # Скрипт сборки
└── start.bat                     # Скрипт запуска
```

---

## 🐛 Устранение проблем

| Проблема | Решение |
|----------|---------|
| ❌ База данных не создаётся | Запустите приложение от имени администратора |
| ❌ Не открывается браузер | Вручную перейдите на `http://localhost:5154/login` |
| ❌ Ошибка "Port 5154 уже используется" | Закройте другой экземпляр приложения |
| ❌ Ошибка сборки | Убедитесь, что установлен .NET 8 SDK |
| ❌ Белый экран после входа | Очистите кэш браузера (Ctrl+F5) |
| ❌ Не работает авторизация | Проверьте, что БД создана и есть пользователь admin |

### Очистка и пересборка

```bash
# Полная очистка
rmdir /s /q bin
rmdir /s /q obj
rmdir /s /q publish

# Пересборка
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

---

## 📋 Планы по развитию

### Ближайшие обновления (Beta → Release)

- [ ] Миграции БД вместо `EnsureCreated()`
- [ ] Расширенные отчёты (графики, диаграммы)
- [ ] Экспорт отчётов в Excel/PDF
- [ ] Уведомления о скором завершении сеанса
- [ ] История изменений тарифов
- [ ] Поддержка PostgreSQL

### Дальнейшее развитие

- [ ] Мобильное приложение
- [ ] API для интеграции с CRM
- [ ] Система лояльности для гостей
- [ ] Онлайн-оплата
- [ ] Управление несколькими филиалами

---

## 📄 Лицензия

MIT License

Copyright (c) 2024 Anticafe

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

---

<div align="center">

**🍵 Сделано с любовью для вашего анти-кафе**

</div>
