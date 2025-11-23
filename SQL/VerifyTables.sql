-- ========================================
-- Script SOLO para Verificar si las Tablas Existen
-- ========================================

PRINT '=========================================='
PRINT 'VERIFICACIÓN DE TABLAS'
PRINT '=========================================='
PRINT ''

-- 1. Verificar ConfiguracionesDisponibilidad
IF OBJECT_ID('dbo.ConfiguracionesDisponibilidad', 'U') IS NULL
BEGIN
    PRINT '❌ La tabla ConfiguracionesDisponibilidad NO EXISTE'
END
ELSE
BEGIN
    PRINT '✅ La tabla ConfiguracionesDisponibilidad EXISTE'
    
    -- Mostrar columnas
    SELECT 'ConfiguracionesDisponibilidad - Columnas:' AS Info
    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ConfiguracionesDisponibilidad'
    ORDER BY ORDINAL_POSITION;
END

PRINT ''

-- 2. Verificar HorariosBarbero
IF OBJECT_ID('dbo.HorariosBarbero', 'U') IS NULL
BEGIN
    PRINT '❌ La tabla HorariosBarbero NO EXISTE'
END
ELSE
BEGIN
    PRINT '✅ La tabla HorariosBarbero EXISTE'
    
    -- Mostrar columnas
    SELECT 'HorariosBarbero - Columnas:' AS Info
    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'HorariosBarbero'
    ORDER BY ORDINAL_POSITION;
    
    PRINT ''
    
    -- Verificar columnas específicas
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HorariosBarbero') AND name = 'FechaFin')
        PRINT '❌ Columna FechaFin NO EXISTE en HorariosBarbero'
    ELSE
        PRINT '✅ Columna FechaFin EXISTE en HorariosBarbero'
        
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HorariosBarbero') AND name = 'LunesLibre')
        PRINT '❌ Columnas de días libres NO EXISTEN en HorariosBarbero'
    ELSE
        PRINT '✅ Columnas de días libres EXISTEN en HorariosBarbero'
END

PRINT ''
PRINT '=========================================='
PRINT 'VERIFICACIÓN COMPLETADA'
PRINT '=========================================='
