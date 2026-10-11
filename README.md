# ShelfOps — Desktop POS-система для магазина электроники

> **ShelfOps** изначально создавался как внутренняя система управления товарами и продажами
> для торговли на [PlayerOk](https://playerok.com/) — агрегаторе цифровых товаров и услуг.
> Со временем проект перерос в полноценный retail-шаблон с desktop-клиентом на WebView2,
> ASP.NET Core API и PostgreSQL, который теперь публикуется как **открытый шаблон**
> для всех, кто хочет построить свою систему управления магазином.

---

## Возможности

| Модуль | Описание |
|--------|----------|
| **Товары** | Каталог с SKU, штрихкодами, ценами закупки/продажи, категориями, остатками |
| **Склады** | Неограниченное количество складов, остатки по товарам |
| **Поступления** | Приход товаров на склад от поставщиков |
| **Продажи** | Оформление продаж с корзиной, расчётом сумм, защитой от дублирования (idempotency) |
| **Возвраты** | Возвраты от покупателей с восстановлением остатка на склад |
| **Дашборд** | KPI-показатели, динамика выручки (SVG-график), последние продажи |
| **Пользователи** | Ролевая модель: Admin, Manager, Cashier |
| **Аудит** | Журнал всех изменений с correlation ID |
| **Настройки** | Локальные настройки интерфейса (компактный режим, звук) |

---

## Технологический стек

| Компонент | Технология |
|-----------|------------|
| **Desktop-клиент** | WPF-оболочка + WebView2 (Edge Chromium) + HTML/CSS/JS |
| **UI-движок** | Vanilla JS (ES modules), CSS custom properties, SVG-иконки, glassmorphism |
| **API** | ASP.NET Core 9.0 Web API, контроллерный подход |
| **Бизнес-логика** | Чистая архитектура: Domain → Application → Infrastructure |
| **ORM** | Entity Framework Core 9.0 |
| **База данных** | PostgreSQL 16 |
| **Аутентификация** | JWT Bearer (симметричный ключ) |
| **Хеширование паролей** | BCrypt.Net |
| **Валидация** | FluentValidation |
| **Логирование** | Serilog |
| **Тесты** | xUnit + Moq + WebApplicationFactory |
| **CI** | GitHub Actions (build → test → publish) |

### Почему WebView2 вместо чистого WPF?

HTML/CSS/JS позволяет делать **красивый современный интерфейс** — стеклянные панели
(glassmorphism), плавные анимации, адаптивную вёрстку — без боли XAML-стилизации.
Вся UI-логика вшита прямо в `.exe` как Embedded Resource: приложение работает полностью
офлайн, без npm-сборок и CDN-зависимостей.

---

## Как это выглядит

```
┌────────────────────────────────────────────────────────┐
│  Кастомный titlebar (drag, свернуть/развернуть/закрыть) │
│  ┌──────────┬─────────────────────────────────────────┐│
│  │ Sidebar  │  Содержимое (товары, продажи, дашборд)  ││
│  │ (glassm) │  Карточки, таблицы, модалки — всё       ││
│  │ Навигац. │  на полупрозрачном стекле с blur        ││
│  │ Пользоват│                                         ││
│  └──────────┴─────────────────────────────────────────┘│
└────────────────────────────────────────────────────────┘
        ├── HTML/CSS/JS (внутри .exe)
        ├── C# мост (WebBridge.cs) ⇄ ApiClient
        └── HTTP ⇄ http://localhost:5000 (ASP.NET Core API)
```

---

## Быстрый старт

### Требования

- **Windows 11** (WSL с Ubuntu) или **Linux**
- **.NET 9 SDK** — [скачать](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL 16**

### 1. Настройка PostgreSQL

```bash
sudo apt update && sudo apt install postgresql postgresql-client -y
sudo service postgresql start
sudo -u postgres psql -c "CREATE USER shelfops_user WITH PASSWORD 'postgres';"
sudo -u postgres psql -c "CREATE DATABASE shelfops OWNER shelfops_user;"
```

### 2. Настройка подключения

Скопируйте `appsettings.example.json` в `appsettings.json` и укажите свои данные:

```powershell
cp src\ShelfOps.Api\appsettings.example.json src\ShelfOps.Api\appsettings.json
```

Либо отредактируйте существующий `appsettings.json` — замените `YOUR_USER` / `YOUR_PASSWORD`
на свои учётные данные PostgreSQL и сгенерируйте свой JWT-ключ (не менее 32 символов).

### 3. Запуск API

```powershell
dotnet run --project src\ShelfOps.Api
```

При первом запуске БД создаётся автоматически через миграции + seed-данные
(категории, склады, тестовые товары, пользователи, продажи, возвраты).

Swagger UI: `http://localhost:5000/swagger`

### 4. Запуск desktop-клиента

```powershell
dotnet run --project src\ShelfOps.Desktop
```

Desktop-клиент по умолчанию подключается к `http://localhost:5000`.

### 5. Тестовые учётные данные

После seed-инициализации доступны пользователи:

| Логин | Пароль | Роль |
|-------|--------|------|
| `admin` | `Admin@123` | Administrator (полный доступ) |
| `manager` | `Manager@123` | Manager (товары, отчёты) |
| `cashier` | `Cashier@123` | Cashier (продажи, товары) |

### 6. Сборка и тесты

```powershell
dotnet build
dotnet test tests\ShelfOps.UnitTests
dotnet test tests\ShelfOps.IntegrationTests
```

---

## Архитектура

```
ShelfOps/
├── .editorconfig                   # Настройки форматирования
├── .gitignore                      # Игнорируемые файлы
├── Directory.Build.props           # Общие настройки проектов
├── ShelfOps.sln                    # Файл решения
├── LICENSE                         # MIT
│
├── src/
│   ├── ShelfOps.Domain/            # Модели предметной области
│   │   ├── Models/                 # Product, Sale, User, Warehouse...
│   │   ├── ValueObjects/           # Money, Quantity
│   │   └── Enums/                  # UserRole, SaleStatus, MovementType...
│   │
│   ├── ShelfOps.Application/       # Бизнес-логика
│   │   ├── Services/               # ProductService, SaleService, AuthService...
│   │   ├── Interfaces/             # IProductRepository, IUnitOfWork...
│   │   ├── DTOs/                   # Запросы/ответы API
│   │   ├── Validators/             # FluentValidation
│   │   └── Common/                 # Result<T>, PagedResult<T>
│   │
│   ├── ShelfOps.Infrastructure/    # EF Core, Security
│   │   ├── Data/                   # DbContext, репозитории, миграции
│   │   ├── Data/Seed/              # DataSeeder (55 товаров, 8 категорий, продажи)
│   │   └── Security/               # PasswordHasher (BCrypt)
│   │
│   ├── ShelfOps.Api/               # ASP.NET Core Web API
│   │   ├── Controllers/            # Products, Sales, Auth, Reports...
│   │   └── Middleware/             # ExceptionHandlingMiddleware
│   │
│   └── ShelfOps.Desktop/           # Desktop-клиент (WPF + WebView2)
│       ├── Views/                  # ShellView.xaml — единственное WPF-окно
│       ├── Web/                    # HTML/CSS/JS интерфейс
│       │   ├── index.html          # Точка входа SPA
│       │   ├── css/app.css         # Glassmorphism-стили
│       │   └── js/                 # screens/, bridge.js, app.js, ui.js
│       ├── Services/ApiClient.cs   # HTTP-клиент с Bearer-токеном
│       ├── WebBridge.cs            # C# ⇄ JS мост через postMessage
│       └── WebResourceHandler.cs   # Раздача embedded-ресурсов
│
├── tests/
│   ├── ShelfOps.UnitTests/         # Модульные тесты (Moq)
│   └── ShelfOps.IntegrationTests/  # Интеграционные тесты (WebApplicationFactory)
│
├── docs/
│   ├── architecture.md             # Детальное описание архитектуры
│   ├── database-schema.md          # Схема БД
│   └── decisions/                  # Architecture Decision Records
│
├── scripts/
│   ├── dev-start.ps1               # Запуск PostgreSQL + API
│   └── dev-reset.ps1               # Сброс БД
│
└── assets/branding/                # Логотипы
```

### Clean Architecture

```
┌──────────────┐     HTTP/JSON     ┌──────────────┐
│ Desktop-     │ ────────────────→ │  ASP.NET     │
│ клиент       │ ←──────────────── │  Core API    │
│ (WPF+WebV2)  │   JWT Bearer     │              │
└──────────────┘                   └──────┬───────┘
                                         │
               ┌─────────────────────────┼──────────┐
               │                         │          │
         ┌─────▼─────┐           ┌───────▼──────┐   │
         │ Application            │ Infrastructure│   │
         │ (Services,  │           │ (EF Core,     │   │
         │  DTOs,      │           │  Repos, JWT,  │   │
         │  Validation)│           │  BCrypt)      │   │
         └─────▲───────┘           └───────▲───────┘   │
               │                         │            │
               └─────────┬───────────────┘            │
                         │                            │
                    ┌────▼────┐                 ┌─────┴──────┐
                    │ Domain  │                 │ PostgreSQL │
                    │ (Models)│                 │  Database  │
                    └─────────┘                 └────────────┘
```

---

## API Endpoints

| Метод | Маршрут | Описание | Доступ |
|-------|---------|----------|--------|
| POST | `/api/v1/auth/login` | Вход в систему | Аноним |
| GET | `/api/v1/auth/me` | Текущий пользователь | Авторизован |
| GET | `/api/v1/products` | Список товаров (с пагинацией) | Авторизован |
| GET | `/api/v1/products/{id}` | Карточка товара | Авторизован |
| GET | `/api/v1/products/{id}/stock` | Остатки по складам | Авторизован |
| POST | `/api/v1/products` | Создать товар | Admin |
| PUT | `/api/v1/products/{id}` | Обновить товар | Admin, Manager |
| POST | `/api/v1/products/{id}/deactivate` | Деактивировать товар | Admin |
| GET | `/api/v1/categories` | Список категорий | Авторизован |
| GET | `/api/v1/warehouses` | Список складов | Авторизован |
| POST | `/api/v1/sales/complete` | Оформить продажу | Cashier, Admin, Manager |
| GET | `/api/v1/sales` | История продаж | Авторизован |
| POST | `/api/v1/returns` | Оформить возврат | Admin, Manager |
| GET | `/api/v1/returns` | История возвратов | Авторизован |
| POST | `/api/v1/receipts` | Поступление товаров | Admin, Manager |
| GET | `/api/v1/reports/dashboard` | Дашборд с показателями | Admin, Manager |
| GET | `/api/v1/users` | Список пользователей | Admin |
| POST | `/api/v1/users` | Создать пользователя | Admin |
| DELETE | `/api/v1/users/{id}` | Блокировка пользователя | Admin |
| GET | `/api/v1/audit-logs` | Журнал аудита | Admin |

---

## Ролевая модель

| Право | Admin | Manager | Cashier |
|-------|-------|---------|---------|
| Управление товарами | ✓ | ✓ (кроме удаления) | — |
| Просмотр остатков | ✓ | ✓ | ✓ |
| Оформление продаж | ✓ | ✓ | ✓ |
| Оформление возвратов | ✓ | ✓ | — |
| Поступления на склад | ✓ | ✓ | — |
| Отчёты и дашборд | ✓ | ✓ | — |
| Управление пользователями | ✓ | — | — |
| Просмотр аудит-лога | ✓ | — | — |

---

## Миграции БД

```powershell
# Создать новую миграцию
dotnet ef migrations add MigrationName `
    --project src\ShelfOps.Infrastructure `
    --startup-project src\ShelfOps.Api

# Применить миграции
dotnet ef database update `
    --project src\ShelfOps.Infrastructure `
    --startup-project src\ShelfOps.Api
```

---

## CI/CD

GitHub Actions при пуше в `main`:
1. Устанавливает .NET 9 SDK
2. Восстанавливает зависимости
3. Собирает всё решение
4. Запускает юнит-тесты и интеграционные тесты
5. Публикует артефакты сборки API

---

## История

ShelfOps родился как внутренний инструмент для автоматизации торговли на
[PlayerOk](https://playerok.com/). Со временем кодовая база разрослась,
появились слоистая архитектура, desktop-клиент, PostgreSQL, JWT-аутентификация,
полноценные отчёты — и проект превратился в готовый шаблон POS-системы,
который можно адаптировать под любой магазин.

Публикуется в открытом доступе как **образовательный шаблон** и отправная точка
для создания своих систем управления торговлей.

---

## Лицензия

MIT — можно использовать, модифицировать и распространять свободно.