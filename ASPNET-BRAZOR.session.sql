-- Migráció históriájának ellenőrzése
SELECT * FROM "__EFMigrationsHistory";

-- Tábla szerkezetének megtekintése
SELECT * FROM information_schema.columns 
WHERE table_name = 'Users' 
ORDER BY ordinal_position;

-- Users tábla tartalmának lekérdezése
SELECT * FROM Users;