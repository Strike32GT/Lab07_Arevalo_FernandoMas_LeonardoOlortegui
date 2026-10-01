-- Base de datos de la solución Grades (Semana 07: arquitectura en capas)
CREATE DATABASE GradesDB;
GO
USE GradesDB;
GO

-- Estudiantes. La columna IsActive permite la eliminación lógica.
CREATE TABLE Students (
    StudentId  INT IDENTITY(1,1) PRIMARY KEY,
    Code       NVARCHAR(10)  NOT NULL UNIQUE,
    FirstName  NVARCHAR(50)  NOT NULL,
    LastName   NVARCHAR(50)  NOT NULL,
    Email      NVARCHAR(100) NULL,
    IsActive   BIT           NOT NULL DEFAULT 1
);
GO

-- Notas por curso. Un estudiante tiene muchas notas (relación 1 a N).
-- El promedio y la condición NO se guardan: los calcula la capa de Negocio.
CREATE TABLE Grades (
    GradeId    INT IDENTITY(1,1) PRIMARY KEY,
    StudentId  INT           NOT NULL REFERENCES Students(StudentId),
    Course     NVARCHAR(100) NOT NULL,
    Score1     DECIMAL(4,2)  NOT NULL,
    Score2     DECIMAL(4,2)  NOT NULL,
    Score3     DECIMAL(4,2)  NOT NULL,
    IsActive   BIT           NOT NULL DEFAULT 1
);
GO

-- Datos de prueba
INSERT INTO Students (Code, FirstName, LastName, Email) VALUES
    (N'U2024001', N'Ana',   N'Torres', N'ana.torres@mail.com'),
    (N'U2024002', N'Luis',  N'Ramos',  N'luis.ramos@mail.com'),
    (N'U2024003', N'Carla', N'Vega',   NULL);
GO

INSERT INTO Grades (StudentId, Course, Score1, Score2, Score3) VALUES
    (1, N'Programming I', 16, 14, 15),
    (1, N'Databases',     12, 13, 14),
    (2, N'Programming I', 10, 12,  9),
    (2, N'Mathematics',   11, 13, 12),
    (3, N'Programming I', 18, 19, 17),
    (3, N'Databases',     17, 16, 18);
GO
