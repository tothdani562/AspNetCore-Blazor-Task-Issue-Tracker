-- TaskTracker Database Initialization Script
-- Ez a fájl automatikusan fut, amikor a PostgreSQL konténer elindul

-- Alapvető extension-ök engedélyezése
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Tábla létrehozása később az EF Core Migration-eken keresztül történik
-- Ez a fájl jelenleg csak az extension-öket inicializálja
