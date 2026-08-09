-- ============================================================================
-- SCRIPT DE BASE DE DATOS: CUSTOMER-PAYMENT CRUD SYSTEM
-- ============================================================================
-- Tutor de IA: ¡Hola! Este script creará la base de datos completa para tu sistema.
-- Está estructurado de manera lógica: primero creamos la base de datos, luego
-- las tablas (desde las independientes hasta las dependientes) y finalmente
-- los procedimientos almacenados (Stored Procedures).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. CREACIÓN DE BASE DE DATOS
-- ----------------------------------------------------------------------------
-- Tutor de IA: Usamos 'IF NOT EXISTS' para evitar errores si la base de datos
-- ya fue creada anteriormente. Luego usamos 'USE' para asegurar que todos los
-- comandos siguientes se ejecuten en esta base de datos específica.

CREATE DATABASE IF NOT EXISTS customer_payment_db;
USE customer_payment_db;

-- ----------------------------------------------------------------------------
-- 2. TABLA: Users
-- ----------------------------------------------------------------------------
-- Tutor de IA: Esta tabla almacena los usuarios del sistema. 
-- - Username es UNIQUE para evitar usuarios duplicados.
-- - PasswordHash guarda la contraseña encriptada (¡nunca en texto plano!).
-- - El Rol por defecto es 'User' y el usuario está Activo por defecto.

CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Rol VARCHAR(20) NOT NULL DEFAULT 'User',
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- ----------------------------------------------------------------------------
-- 3. TABLA: Customers
-- ----------------------------------------------------------------------------
-- Tutor de IA: Almacena la información de los clientes.
-- - Usamos un campo 'Activo' de tipo BOOLEAN para implementar "Soft Delete"
--   (eliminación lógica), lo que nos permite mantener el historial de datos
--   sin borrar realmente el registro.

CREATE TABLE IF NOT EXISTS Customers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Telefono VARCHAR(20) NULL,
    Direccion VARCHAR(255) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- ----------------------------------------------------------------------------
-- 4. TABLA: Payments
-- ----------------------------------------------------------------------------
-- Tutor de IA: Almacena los pagos realizados por los clientes.
-- - Esta tabla depende de Customers (relación 1 a muchos).
-- - Usamos 'ON DELETE RESTRICT' en la Foreign Key para garantizar la
--   integridad referencial: no podrás eliminar un cliente si tiene pagos asociados.

CREATE TABLE IF NOT EXISTS Payments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CustomerId INT NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    MetodoPago VARCHAR(50) NOT NULL,
    Referencia VARCHAR(100) NULL,
    FechaPago DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
    CONSTRAINT FK_Payment_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE RESTRICT
);

-- ============================================================================
-- PROCEDIMIENTOS ALMACENADOS (STORED PROCEDURES)
-- ============================================================================
-- Tutor de IA: Los Stored Procedures encapsulan la lógica de acceso a datos.
-- Esto mejora la seguridad (evita SQL Injection) y el rendimiento.
-- Usamos 'DELIMITER //' para decirle a MySQL que los ';' dentro del 
-- procedimiento no significan el final de la instrucción general, sino del bloque.

-- ----------------------------------------------------------------------------
-- 5. STORED PROCEDURES: CUSTOMER
-- ----------------------------------------------------------------------------

-- a) sp_Customer_GetAll
-- Tutor de IA: Trae todos los clientes que están activos. Ordenamos por 
-- FechaCreacion DESC para ver primero los clientes más recientes.
DROP PROCEDURE IF EXISTS sp_Customer_GetAll;
DELIMITER //
CREATE PROCEDURE sp_Customer_GetAll()
BEGIN
    SELECT Id, Nombre, Email, Telefono, Direccion, FechaCreacion, Activo 
    FROM Customers 
    WHERE Activo = TRUE 
    ORDER BY FechaCreacion DESC;
END //
DELIMITER ;

-- b) sp_Customer_GetById
-- Tutor de IA: Busca un cliente específico usando su Id (Clave Primaria).
-- Es muy rápido porque busca directamente por el índice principal.
DROP PROCEDURE IF EXISTS sp_Customer_GetById;
DELIMITER //
CREATE PROCEDURE sp_Customer_GetById(IN p_Id INT)
BEGIN
    SELECT Id, Nombre, Email, Telefono, Direccion, FechaCreacion, Activo 
    FROM Customers 
    WHERE Id = p_Id;
END //
DELIMITER ;

-- c) sp_Customer_Create
-- Tutor de IA: Inserta un nuevo cliente. Al finalizar, retorna el ID generado 
-- automáticamente usando LAST_INSERT_ID(), lo cual es útil para la aplicación.
DROP PROCEDURE IF EXISTS sp_Customer_Create;
DELIMITER //
CREATE PROCEDURE sp_Customer_Create(
    IN p_Nombre VARCHAR(100), 
    IN p_Email VARCHAR(100), 
    IN p_Telefono VARCHAR(20), 
    IN p_Direccion VARCHAR(255)
)
BEGIN
    INSERT INTO Customers (Nombre, Email, Telefono, Direccion) 
    VALUES (p_Nombre, p_Email, p_Telefono, p_Direccion);
    
    SELECT LAST_INSERT_ID() AS Id;
END //
DELIMITER ;

-- d) sp_Customer_Update
-- Tutor de IA: Actualiza los datos de un cliente existente. Retorna
-- ROW_COUNT() para que la aplicación sepa cuántas filas fueron modificadas 
-- (debería ser 1 si fue exitoso, 0 si no existía el cliente).
DROP PROCEDURE IF EXISTS sp_Customer_Update;
DELIMITER //
CREATE PROCEDURE sp_Customer_Update(
    IN p_Id INT, 
    IN p_Nombre VARCHAR(100), 
    IN p_Email VARCHAR(100), 
    IN p_Telefono VARCHAR(20), 
    IN p_Direccion VARCHAR(255)
)
BEGIN
    UPDATE Customers 
    SET Nombre = p_Nombre, 
        Email = p_Email, 
        Telefono = p_Telefono, 
        Direccion = p_Direccion 
    WHERE Id = p_Id;
    
    SELECT ROW_COUNT() AS FilasAfectadas;
END //
DELIMITER ;

-- e) sp_Customer_Delete
-- Tutor de IA: Realiza una "Eliminación Lógica" (Soft Delete).
-- En lugar de borrar el registro (DELETE FROM), simplemente cambiamos su estado 
-- a inactivo. Esto evita romper relaciones con la tabla Payments.
DROP PROCEDURE IF EXISTS sp_Customer_Delete;
DELIMITER //
CREATE PROCEDURE sp_Customer_Delete(IN p_Id INT)
BEGIN
    UPDATE Customers 
    SET Activo = FALSE 
    WHERE Id = p_Id;
    
    SELECT ROW_COUNT() AS FilasAfectadas;
END //
DELIMITER ;

-- ----------------------------------------------------------------------------
-- 6. STORED PROCEDURES: PAYMENT
-- ----------------------------------------------------------------------------

-- a) sp_Payment_GetAll
-- Tutor de IA: Obtiene la lista de pagos. Si el p_CustomerId es NULL, trae 
-- todos. Si tiene valor, filtra por ese cliente. Hacemos un JOIN con Customers
-- para traer el nombre del cliente, dándole más contexto a la interfaz de usuario.
DROP PROCEDURE IF EXISTS sp_Payment_GetAll;
DELIMITER //
CREATE PROCEDURE sp_Payment_GetAll(IN p_CustomerId INT)
BEGIN
    IF p_CustomerId IS NULL THEN
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.Referencia, p.FechaPago, p.Estado
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        ORDER BY p.FechaPago DESC;
    ELSE
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.Referencia, p.FechaPago, p.Estado
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        WHERE p.CustomerId = p_CustomerId
        ORDER BY p.FechaPago DESC;
    END IF;
END //
DELIMITER ;

-- b) sp_Payment_GetById
-- Tutor de IA: Busca un pago específico por su Id. Al igual que en GetAll,
-- hacemos el JOIN para enriquecer la información con el nombre del cliente.
DROP PROCEDURE IF EXISTS sp_Payment_GetById;
DELIMITER //
CREATE PROCEDURE sp_Payment_GetById(IN p_Id INT)
BEGIN
    SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.Referencia, p.FechaPago, p.Estado
    FROM Payments p
    INNER JOIN Customers c ON p.CustomerId = c.Id
    WHERE p.Id = p_Id;
END //
DELIMITER ;

-- c) sp_Payment_Create
-- Tutor de IA: Inserta un nuevo pago vinculado a un Customer. Retorna el ID
-- generado. Note que el Estado y FechaPago tomarán sus valores por defecto si 
-- no se proveen (aunque aquí Estado queda 'Pendiente' por la definición de la tabla).
DROP PROCEDURE IF EXISTS sp_Payment_Create;
DELIMITER //
CREATE PROCEDURE sp_Payment_Create(
    IN p_CustomerId INT, 
    IN p_Monto DECIMAL(18,2), 
    IN p_MetodoPago VARCHAR(50), 
    IN p_Referencia VARCHAR(100)
)
BEGIN
    INSERT INTO Payments (CustomerId, Monto, MetodoPago, Referencia) 
    VALUES (p_CustomerId, p_Monto, p_MetodoPago, p_Referencia);
    
    SELECT LAST_INSERT_ID() AS Id;
END //
DELIMITER ;

-- d) sp_Payment_Update
-- Tutor de IA: Actualiza toda la información de un pago. En un caso real,
-- tal vez solo permitiríamos actualizar el Estado, pero aquí permitimos 
-- modificar todos los campos operativos.
DROP PROCEDURE IF EXISTS sp_Payment_Update;
DELIMITER //
CREATE PROCEDURE sp_Payment_Update(
    IN p_Id INT, 
    IN p_CustomerId INT, 
    IN p_Monto DECIMAL(18,2), 
    IN p_MetodoPago VARCHAR(50), 
    IN p_Referencia VARCHAR(100), 
    IN p_Estado VARCHAR(20)
)
BEGIN
    UPDATE Payments 
    SET CustomerId = p_CustomerId, 
        Monto = p_Monto, 
        MetodoPago = p_MetodoPago, 
        Referencia = p_Referencia, 
        Estado = p_Estado 
    WHERE Id = p_Id;
    
    SELECT ROW_COUNT() AS FilasAfectadas;
END //
DELIMITER ;

-- e) sp_Payment_Delete
-- Tutor de IA: Para esta tabla, realizamos un "Hard Delete" (borrado físico).
-- A diferencia de Customers (donde usamos Soft Delete), aquí borramos la fila 
-- de la base de datos permanentemente con DELETE FROM.
DROP PROCEDURE IF EXISTS sp_Payment_Delete;
DELIMITER //
CREATE PROCEDURE sp_Payment_Delete(IN p_Id INT)
BEGIN
    DELETE FROM Payments 
    WHERE Id = p_Id;
    
    SELECT ROW_COUNT() AS FilasAfectadas;
END //
DELIMITER ;

-- ----------------------------------------------------------------------------
-- 7. STORED PROCEDURES: USER
-- ----------------------------------------------------------------------------

-- a) sp_User_GetByUsername
-- Tutor de IA: Busca un usuario por su Username (usado en el Login). 
-- Además verifica que el usuario esté activo ('Activo = TRUE').
DROP PROCEDURE IF EXISTS sp_User_GetByUsername;
DELIMITER //
CREATE PROCEDURE sp_User_GetByUsername(IN p_Username VARCHAR(50))
BEGIN
    SELECT Id, Username, PasswordHash, Rol, FechaCreacion, Activo 
    FROM Users 
    WHERE Username = p_Username AND Activo = TRUE;
END //
DELIMITER ;

-- b) sp_User_Create
-- Tutor de IA: Crea un nuevo usuario en el sistema. Asegúrate que en la 
-- aplicación el parámetro 'p_PasswordHash' ya venga encriptado (por ejemplo, con BCrypt).
DROP PROCEDURE IF EXISTS sp_User_Create;
DELIMITER //
CREATE PROCEDURE sp_User_Create(
    IN p_Username VARCHAR(50), 
    IN p_PasswordHash VARCHAR(255), 
    IN p_Rol VARCHAR(20)
)
BEGIN
    INSERT INTO Users (Username, PasswordHash, Rol) 
    VALUES (p_Username, p_PasswordHash, p_Rol);
    
    SELECT LAST_INSERT_ID() AS Id;
END //
DELIMITER ;

-- ============================================================================
-- FIN DEL SCRIPT
-- Tutor de IA: ¡Excelente trabajo! Has construido una estructura robusta para
-- manejar clientes y pagos con procedimientos almacenados bien definidos.
-- ============================================================================
