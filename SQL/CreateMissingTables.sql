-- ========================================
-- Script para Verificar y Crear Tablas Faltantes
-- ========================================

-- 1. Verificar si la tabla ConfiguracionesDisponibilidad existe
IF OBJECT_ID('dbo.ConfiguracionesDisponibilidad', 'U') IS NULL
BEGIN
    PRINT 'La tabla ConfiguracionesDisponibilidad NO existe. Se creará ahora.'
    
    -- Crear la tabla ConfiguracionesDisponibilidad
    CREATE TABLE dbo.ConfiguracionesDisponibilidad (
        ConfiguracionID INT IDENTITY(1,1) PRIMARY KEY,
        BarberoID INT NOT NULL,
        FechaInicio DATE NOT NULL,
        FechaFin DATE NOT NULL,
        LunesLibre BIT NOT NULL DEFAULT 0,
        MartesLibre BIT NOT NULL DEFAULT 0,
        MiercolesLibre BIT NOT NULL DEFAULT 0,
        JuevesLibre BIT NOT NULL DEFAULT 0,
        ViernesLibre BIT NOT NULL DEFAULT 0,
        SabadoLibre BIT NOT NULL DEFAULT 0,
        DomingoLibre BIT NOT NULL DEFAULT 0,
        HoraInicioTrabajo TIME NULL,
        HoraFinTrabajo TIME NULL,
        FechaCreacion DATETIME NOT NULL DEFAULT GETDATE(),
        AdminCreadorID INT NOT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_ConfiguracionDisponibilidad_Barberos 
            FOREIGN KEY (BarberoID) REFERENCES dbo.Barberos(BarberoID),
        CONSTRAINT FK_ConfiguracionDisponibilidad_Usuarios 
            FOREIGN KEY (AdminCreadorID) REFERENCES dbo.Usuarios(UsuarioID)
    );
    
    PRINT 'Tabla ConfiguracionesDisponibilidad creada exitosamente.'
END
ELSE
BEGIN
    PRINT 'La tabla ConfiguracionesDisponibilidad YA EXISTE.'
END

GO

-- ========================================
-- 2. Verificar y agregar columnas faltantes en HorariosBarbero
-- ========================================

-- Verificar si FechaFin existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HorariosBarbero') AND name = 'FechaFin')
BEGIN
    PRINT 'Agregando columna FechaFin a HorariosBarbero...'
    ALTER TABLE dbo.HorariosBarbero ADD FechaFin DATE NULL;
    PRINT 'Columna FechaFin agregada.'
END
ELSE
BEGIN
    PRINT 'Columna FechaFin YA EXISTE en HorariosBarbero.'
END

-- Verificar y agregar columnas de días libres
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.HorariosBarbero') AND name = 'LunesLibre')
BEGIN
    PRINT 'Agregando columnas de días libres a HorariosBarbero...'
    
    ALTER TABLE dbo.HorariosBarbero ADD LunesLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD MartesLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD MiercolesLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD JuevesLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD ViernesLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD SabadoLibre BIT NULL;
    ALTER TABLE dbo.HorariosBarbero ADD DomingoLibre BIT NULL;
    
    PRINT 'Columnas de días libres agregadas a HorariosBarbero.'
END
ELSE
BEGIN
    PRINT 'Columnas de días libres YA EXISTEN en HorariosBarbero.'
END

GO

-- ========================================
-- 3. Verificar estructura final
-- ========================================

PRINT ''
PRINT '=========================================='
PRINT 'VERIFICACIÓN FINAL DE ESTRUCTURAS'
PRINT '=========================================='

-- Mostrar columnas de ConfiguracionesDisponibilidad
PRINT ''
PRINT 'Columnas de ConfiguracionesDisponibilidad:'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ConfiguracionesDisponibilidad'
ORDER BY ORDINAL_POSITION;

-- Mostrar columnas de HorariosBarbero
PRINT ''
PRINT 'Columnas de HorariosBarbero:'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'HorariosBarbero'
ORDER BY ORDINAL_POSITION;

GO

PRINT ''
PRINT '=========================================='
PRINT 'SCRIPT COMPLETADO EXITOSAMENTE'
PRINT '=========================================='
