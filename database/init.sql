-- Таблица пользователей
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    FullName TEXT,
    Role TEXT NOT NULL,
    Phone TEXT,
    IsBlocked INTEGER DEFAULT 0,
    FailedAttempts INTEGER DEFAULT 0,
    BlockedUntil TEXT
);

-- Таблица сеансов
CREATE TABLE IF NOT EXISTS Sessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GuestName TEXT NOT NULL,
    Phone TEXT,
    TableNumber INTEGER NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT,
    TotalMinutes INTEGER DEFAULT 0,
    TotalCost REAL DEFAULT 0,
    IsActive INTEGER DEFAULT 1,
    DrinksCost REAL DEFAULT 0
);

-- Таблица бронирований
CREATE TABLE IF NOT EXISTS Bookings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BookingDate TEXT NOT NULL,
    BookingTime TEXT NOT NULL,
    GuestName TEXT NOT NULL,
    Phone TEXT NOT NULL,
    TableNumber INTEGER NOT NULL,
    Status TEXT DEFAULT 'active',
    IpAddress TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Таблица залов
CREATE TABLE IF NOT EXISTS Rooms (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type TEXT DEFAULT 'usual',
    Tariff REAL DEFAULT 3.5
);

-- Таблица тарифов
CREATE TABLE IF NOT EXISTS Tariffs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DayOfWeek INTEGER,
    HourFrom INTEGER,
    HourTo INTEGER,
    PricePerMinute REAL NOT NULL,
    IsActive INTEGER DEFAULT 1
);

-- Таблица акций
CREATE TABLE IF NOT EXISTS Promotions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Type TEXT,
    Value REAL,
    StartDate TEXT,
    EndDate TEXT,
    IsActive INTEGER DEFAULT 1
);

-- Таблица напитков
CREATE TABLE IF NOT EXISTS Drinks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Quantity INTEGER DEFAULT 0,
    Price REAL
);

-- Журнал автосохранения
CREATE TABLE IF NOT EXISTS ActionLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ActionType TEXT,
    Data TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Вставка начальных данных
INSERT OR IGNORE INTO Users (Id, Username, PasswordHash, FullName, Role) 
VALUES (1, 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Главный администратор', 'admin');

INSERT OR IGNORE INTO Tariffs (Id, DayOfWeek, HourFrom, HourTo, PricePerMinute, IsActive)
VALUES (1, NULL, 0, 23, 3.5, 1);

INSERT OR IGNORE INTO Rooms (Id, Name, Type, Tariff)
VALUES (1, 'Основной зал', 'usual', 3.5);