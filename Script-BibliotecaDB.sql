-- =====================================================================
-- LABORATORIO 07 - BibliotecaDB
-- Arquitectura de capas: Entidades / Datos / Negocio / WPF
-- Ejecutar el script completo en SQL Server Management Studio.
-- =====================================================================

IF DB_ID('BibliotecaDB') IS NOT NULL
BEGIN
    ALTER DATABASE BibliotecaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BibliotecaDB;
END
GO

CREATE DATABASE BibliotecaDB;
GO

USE BibliotecaDB;
GO

-- ---------------------------------------------------------------------
-- Tabla Autores. Activo permite la eliminacion logica.
-- ---------------------------------------------------------------------
CREATE TABLE Autores (
    AutorId      INT IDENTITY(1,1) PRIMARY KEY,
    Nombre       NVARCHAR(100) NOT NULL,
    Nacionalidad NVARCHAR(50)  NULL,
    Activo       BIT           NOT NULL DEFAULT 1
);
GO

-- ---------------------------------------------------------------------
-- Tabla Libros. AutorId es clave foranea a Autores (relacion 1 a N).
-- Ejemplares es el stock disponible: la capa de Negocio lo descuenta
-- al prestar y lo devuelve al devolver.
-- ---------------------------------------------------------------------
CREATE TABLE Libros (
    LibroId   INT IDENTITY(1,1) PRIMARY KEY,
    Titulo    NVARCHAR(150)   NOT NULL,
    ISBN      NVARCHAR(20)    NOT NULL UNIQUE,
    AutorId   INT             NOT NULL REFERENCES Autores(AutorId),
    Ejemplares INT            NOT NULL DEFAULT 1 CHECK (Ejemplares >= 0),
    Activo    BIT             NOT NULL DEFAULT 1
);
GO

-- ---------------------------------------------------------------------
-- Tabla Socios. El DNI es unico.
-- ---------------------------------------------------------------------
CREATE TABLE Socios (
    SocioId INT IDENTITY(1,1) PRIMARY KEY,
    DNI     NVARCHAR(8)   NOT NULL UNIQUE,
    Nombre  NVARCHAR(100) NOT NULL,
    Email   NVARCHAR(100) NULL,
    Activo  BIT           NOT NULL DEFAULT 1
);
GO

-- ---------------------------------------------------------------------
-- Tabla Prestamos (cabecera). Un socio puede tener varios prestamos.
-- ---------------------------------------------------------------------
CREATE TABLE Prestamos (
    PrestamoId    INT IDENTITY(1,1) PRIMARY KEY,
    SocioId       INT          NOT NULL REFERENCES Socios(SocioId),
    FechaPrestamo DATE         NOT NULL,
    FechaLimite   DATE         NOT NULL,
    Estado        NVARCHAR(20) NOT NULL DEFAULT 'Pendiente'
                  CHECK (Estado IN ('Pendiente', 'Devuelto'))
);
GO

-- ---------------------------------------------------------------------
-- Tabla DetallePrestamo (lineas del prestamo).
-- Clave primaria compuesta (PrestamoId, LibroId).
-- FechaDevolucion NULL = el libro sigue prestado.
-- ---------------------------------------------------------------------
CREATE TABLE DetallePrestamo (
    PrestamoId      INT NOT NULL REFERENCES Prestamos(PrestamoId),
    LibroId         INT NOT NULL REFERENCES Libros(LibroId),
    FechaDevolucion DATE NULL,
    PRIMARY KEY (PrestamoId, LibroId)
);
GO

-- Indices de apoyo para las consultas de busqueda y de reportes.
CREATE INDEX IX_Libros_Titulo   ON Libros(Titulo);
CREATE INDEX IX_Libros_AutorId  ON Libros(AutorId);
CREATE INDEX IX_Socios_Nombre   ON Socios(Nombre);
CREATE INDEX IX_Prestamos_Fecha ON Prestamos(FechaPrestamo);
GO

-- =====================================================================
-- DATOS DE PRUEBA
-- =====================================================================

-- 8 autores
INSERT INTO Autores (Nombre, Nacionalidad) VALUES
    (N'Gabriel García Márquez',   N'Colombiana'),
    (N'Mario Vargas Llosa',       N'Peruana'),
    (N'Isabel Allende',           N'Chilena'),
    (N'Jorge Luis Borges',        N'Argentina'),
    (N'Miguel de Cervantes',      N'Espanola'),
    (N'Paulo Coelho',             N'Brasileña'),
    (N'Julio Cortázar',           N'Argentina'),
    (N'José María Arguedas',      N'Peruana');
GO

-- 20 libros
INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
    (N'Cien años de soledad',                 N'9780307474728', 1, 5),
    (N'El amor en los tiempos del cólera',    N'9780307389732', 1, 3),
    (N'La ciudad y los perros',               N'9788437604572', 2, 2),
    (N'La casa de los espíritus',             N'9788401337207', 3, 4),
    (N'El amor en el tiempo del cólera',      N'9788432310462', 3, 0),
    (N'Ficciones',                            N'9780802130303', 4, 6),
    (N'El Aleph',                             N'9780802130304', 4, 3),
    (N'Don Quijote de la Mancha',             N'9788420412146', 5, 2),
    (N'El ingenioso hidalgo don Quijote',     N'9788437600503', 5, 4),
    (N'El principito',                        N'9780156012195', 6, 8),
    (N'La quinta montaña',                    N'9788401352835', 6, 2),
    (N'Rayuela',                              N'9788437604573', 7, 3),
    (N'Cuentos de cronopios y de famas',      N'9788437604574', 7, 1),
    (N'Pampa y cerro',                        N'9788437604575', 8, 5),
    (N'La ciudad de los perros',              N'9788437604576', 8, 0),
    (N'Veinte poemas de amor',                N'9788437604577', 2, 3),
    (N'Noches de tormenta',                   N'9788408062653', 2, 2),
    (N'Tres nombres para un lugar',           N'9788408043652', 4, 4),
    (N'Pedro Páramo',                         N'9788437604578', 8, 3),
    (N'Las mejores historias',                N'9788401355140', 7, 2);
GO

-- 10 socios
INSERT INTO Socios (DNI, Nombre, Email) VALUES
    (N'70123456', N'Fernando Mas',        N'fernando.mas@correo.com'),
    (N'70123457', N'Leonardo Olortegui',   N'leonardo.olortegui@correo.com'),
    (N'45678912', N'Ana Torres Ruiz',      N'ana.torres@correo.com'),
    (N'45678913', N'Luis Ramirez Vega',    N'luis.ramirez@correo.com'),
    (N'78901234', N'Carla Mendoza Paz',    N'carla.mendoza@correo.com'),
    (N'78901235', N'Diego Salazar Lopez',  N'diego.salazar@correo.com'),
    (N'32109876', N'Maria Quispe Huaman',  N'maria.quispe@correo.com'),
    (N'32109877', N'Jorge Castro Paredes', N'jorge.castro@correo.com'),
    (N'65432109', N'Sandra Flores Rojas',  N'sandra.flores@correo.com'),
    (N'65432110', N'Pedro Ramirez Vega',  N'pedro.ramirez@correo.com');
GO

-- =====================================================================
-- 5 prestamos con su detalle.
-- El socio 3 (Ana Torres Ruiz) tiene 3 libros pendientes: al intentar
-- un cuarto prestamo se dispara la regla "maximo 3 libros pendientes".
-- =====================================================================
INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
    (1, '2026-08-01', '2026-08-15', 'Pendiente'),
    (2, '2026-08-05', '2026-08-19', 'Pendiente'),
    (3, '2026-08-10', '2026-08-24', 'Pendiente'),
    (4, '2026-08-12', '2026-08-26', 'Devuelto'),
    (5, '2026-08-20', '2026-09-03', 'Pendiente');
GO

-- Socio 1: 1 libro pendiente.
INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (1, 1,  NULL);
GO

-- Socio 2: 1 libro pendiente.
INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (2, 6,  NULL);
GO

-- Socio 3 (Ana): 3 libros pendientes -> prueba la regla del limite.
INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (3, 10, NULL),
    (3, 12, NULL),
    (3, 16, NULL);
GO

-- Socio 4: devolvio su libro (el prestamo queda en Devuelto).
INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (4, 3, '2026-08-20');
GO

-- Socio 5: 1 libro pendiente.
INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (5, 2,  NULL);
GO

PRINT 'BibliotecaDB creada correctamente.';
GO
