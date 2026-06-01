-- =============================================
-- АНТИ-КАФЕ - ПОЛНАЯ БАЗА ДАННЫХ v2.0
-- =============================================

-- 1. ПОЛЬЗОВАТЕЛИ
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'admin',
    Phone TEXT,
    IsBlocked INTEGER DEFAULT 0,
    FailedAttempts INTEGER DEFAULT 0,
    BlockedUntil TEXT
);

-- 2. ЗАЛЫ
CREATE TABLE IF NOT EXISTS Rooms (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type TEXT DEFAULT 'usual'
);

-- 3. СТОЛЫ
CREATE TABLE IF NOT EXISTS Tables (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId INTEGER NOT NULL,
    TableNumber INTEGER NOT NULL,
    IsActive INTEGER DEFAULT 1,
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);

-- 4. СЕАНСЫ
CREATE TABLE IF NOT EXISTS Sessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuestName TEXT NOT NULL,
    Phone TEXT,
    TableNumber INTEGER NOT NULL,
    RoomId INTEGER DEFAULT 1,
    StartTime TEXT NOT NULL,
    EndTime TEXT,
    DurationMinutes INTEGER DEFAULT 0,
    TariffRate REAL DEFAULT 3.5,
    TotalCost REAL DEFAULT 0,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- 5. БРОНИРОВАНИЯ
CREATE TABLE IF NOT EXISTS Bookings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuestName TEXT NOT NULL,
    Phone TEXT NOT NULL,
    TableNumber INTEGER NOT NULL,
    RoomId INTEGER DEFAULT 1,
    BookingDate TEXT NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT,
    DurationMinutes INTEGER DEFAULT 60,
    Status TEXT DEFAULT 'active',
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- 6. ТАРИФЫ
CREATE TABLE IF NOT EXISTS Tariffs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT DEFAULT 'Стандартный',
    DayOfWeek INTEGER,
    HourFrom INTEGER,
    HourTo INTEGER,
    PricePerMinute REAL NOT NULL,
    MinimumMinutes INTEGER DEFAULT 30,
    IsActive INTEGER DEFAULT 1
);

-- 7. ОТЧЁТЫ (для истории)
CREATE TABLE IF NOT EXISTS Reports (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId INTEGER,
    Receipt TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- НАЧАЛЬНЫЕ ДАННЫЕ
-- =============================================

-- Админ (пароль: admin)
INSERT OR IGNORE INTO Users (Id, Username, PasswordHash, FullName, Role) 
VALUES (1, 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Администратор', 'admin');

-- Залы
INSERT OR IGNORE INTO Rooms (Id, Name, Type) VALUES
(1, 'Основной зал', 'usual'),
(2, 'VIP зал', 'vip');

-- Столы (1-10 в основном зале)
INSERT OR IGNORE INTO Tables (Id, RoomId, TableNumber) VALUES
(1, 1, 1), (2, 1, 2), (3, 1, 3), (4, 1, 4), (5, 1, 5),
(6, 1, 6), (7, 1, 7), (8, 1, 8), (9, 1, 9), (10, 1, 10);

-- Тариф по умолчанию
INSERT OR IGNORE INTO Tariffs (Id, DayOfWeek, HourFrom, HourTo, PricePerMinute, MinimumMinutes) 
VALUES (1, NULL, 0, 23, 3.5, 30);