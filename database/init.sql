-- ============================================================================
-- SCRIPT DE BASE DE DATOS: CUSTOMER-PAYMENT CRUD SYSTEM
-- INIT.SQL ÚNICO PARA DOCKER
-- ============================================================================

CREATE DATABASE IF NOT EXISTS customer_payment_db;
USE customer_payment_db;

-- ----------------------------------------------------------------------------
-- 1. TABLAS
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Rol VARCHAR(20) NOT NULL DEFAULT 'User',
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS Customers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Telefono VARCHAR(20) NULL,
    Direccion VARCHAR(255) NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS Payments (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CustomerId INT NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    MetodoPago VARCHAR(50) NOT NULL,
    FechaPago DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Payment_Customer FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 2. STORED PROCEDURES
-- ----------------------------------------------------------------------------
DELIMITER //

DROP PROCEDURE IF EXISTS sp_Customer_GetAll//
CREATE PROCEDURE sp_Customer_GetAll()
BEGIN
    SELECT Id, Nombre, Email, Telefono, Direccion, FechaCreacion, Activo 
    FROM Customers 
    WHERE Activo = TRUE 
    ORDER BY FechaCreacion DESC;
END //

DROP PROCEDURE IF EXISTS sp_Customer_GetById//
CREATE PROCEDURE sp_Customer_GetById(IN p_Id INT)
BEGIN
    SELECT Id, Nombre, Email, Telefono, Direccion, FechaCreacion, Activo 
    FROM Customers 
    WHERE Id = p_Id;
END //

DROP PROCEDURE IF EXISTS sp_Customer_Create//
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

DROP PROCEDURE IF EXISTS sp_Customer_Update//
CREATE PROCEDURE sp_Customer_Update(
    IN p_Id INT, 
    IN p_Nombre VARCHAR(100), 
    IN p_Email VARCHAR(100), 
    IN p_Telefono VARCHAR(20), 
    IN p_Direccion VARCHAR(255)
)
BEGIN
    UPDATE Customers 
    SET Nombre = p_Nombre, Email = p_Email, Telefono = p_Telefono, Direccion = p_Direccion 
    WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END //

DROP PROCEDURE IF EXISTS sp_Customer_Delete//
CREATE PROCEDURE sp_Customer_Delete(IN p_Id INT)
BEGIN
    UPDATE Customers SET Activo = FALSE WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END //

DROP PROCEDURE IF EXISTS sp_Payment_GetAll//
CREATE PROCEDURE sp_Payment_GetAll(IN p_CustomerId INT)
BEGIN
    IF p_CustomerId IS NULL THEN
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado, p.FechaCreacion
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        ORDER BY p.FechaPago DESC;
    ELSE
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado, p.FechaCreacion
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        WHERE p.CustomerId = p_CustomerId
        ORDER BY p.FechaPago DESC;
    END IF;
END //

DROP PROCEDURE IF EXISTS sp_Payment_GetById//
CREATE PROCEDURE sp_Payment_GetById(IN p_Id INT)
BEGIN
    SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado, p.FechaCreacion
    FROM Payments p
    INNER JOIN Customers c ON p.CustomerId = c.Id
    WHERE p.Id = p_Id;
END //

DROP PROCEDURE IF EXISTS sp_Payment_Create//
CREATE PROCEDURE sp_Payment_Create(
    IN p_CustomerId INT, 
    IN p_Monto DECIMAL(18,2), 
    IN p_MetodoPago VARCHAR(50)
)
BEGIN
    INSERT INTO Payments (CustomerId, Monto, MetodoPago) 
    VALUES (p_CustomerId, p_Monto, p_MetodoPago);
    SELECT LAST_INSERT_ID() AS Id;
END //

DROP PROCEDURE IF EXISTS sp_Payment_Update//
CREATE PROCEDURE sp_Payment_Update(
    IN p_Id INT, 
    IN p_CustomerId INT, 
    IN p_Monto DECIMAL(18,2), 
    IN p_MetodoPago VARCHAR(50), 
    IN p_Estado VARCHAR(20)
)
BEGIN
    UPDATE Payments 
    SET CustomerId = p_CustomerId, Monto = p_Monto, MetodoPago = p_MetodoPago, Estado = p_Estado 
    WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END //

DROP PROCEDURE IF EXISTS sp_Payment_UpdateStatus//
CREATE PROCEDURE sp_Payment_UpdateStatus(
    IN p_Id INT,
    IN p_Estado VARCHAR(20)
)
BEGIN
    UPDATE Payments SET Estado = p_Estado WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END //

DROP PROCEDURE IF EXISTS sp_Payment_Delete//
CREATE PROCEDURE sp_Payment_Delete(IN p_Id INT)
BEGIN
    DELETE FROM Payments WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END //

DROP PROCEDURE IF EXISTS sp_User_GetByUsername//
CREATE PROCEDURE sp_User_GetByUsername(IN p_Username VARCHAR(50))
BEGIN
    SELECT Id, Username, PasswordHash, Rol, FechaCreacion, Activo 
    FROM Users 
    WHERE Username = p_Username AND Activo = TRUE;
END //

DROP PROCEDURE IF EXISTS sp_User_Create//
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

-- ----------------------------------------------------------------------------
-- 3. SEED DATA
-- ----------------------------------------------------------------------------
INSERT INTO Customers (Nombre, Email, Telefono, Direccion, FechaCreacion, Activo) VALUES
('Tech Solutions Corp', 'contacto@techsolutions.com', '555-0101', 'Av. Innovación 101', '2026-01-15 10:30:00', 1),
('Global Logistics', 'info@globallogistics.com', '555-0102', 'Parque Industrial Sur', '2026-01-22 14:15:00', 1),
('Marketing Creativo SA', 'hola@marketingcreativo.com', '555-0103', 'Plaza Central 45', '2026-02-05 09:00:00', 1),
('Consultoría Alfa', 'admin@alfa-consulting.com', '555-0104', 'Edificio Omega, Piso 5', '2026-02-14 16:45:00', 1),
('Distribuidora del Norte', 'ventas@distnorte.com', '555-0105', 'Av. Las Industrias 99', '2026-03-01 11:20:00', 1),
('Software & Más', 'soporte@softwareymas.com', '555-0106', 'Calle Código 256', '2026-03-10 13:10:00', 1),
('Eco Envases', 'contacto@ecoenvases.com', '555-0107', 'Boulevard Verde 88', '2026-03-25 10:05:00', 1),
('Finanzas Seguras', 'clientes@finanzasseguras.com', '555-0108', 'Torre Financiera L2', '2026-04-02 15:30:00', 1),
('Constructora Horizonte', 'info@horizonte.com', '555-0109', 'Av. Los Constructores 12', '2026-04-18 08:45:00', 1),
('Agencia de Viajes Mundo', 'reservas@viajesmundo.com', '555-0110', 'Plaza Sol 4A', '2026-04-30 12:00:00', 1),
('Clínica Bienestar', 'recepcion@bienestar.com', '555-0111', 'Calle Salud 123', '2026-05-05 09:15:00', 1),
('Educación Moderna', 'inscripciones@edumoderna.com', '555-0112', 'Av. Universidad 400', '2026-05-12 14:40:00', 1),
('Supermercados Ahorro', 'proveedores@ahorro.com', '555-0113', 'Centro Comercial Oeste', '2026-05-20 16:55:00', 1),
('Gimnasio FitLife', 'info@fitlife.com', '555-0114', 'Av. Deportiva 55', '2026-06-01 07:30:00', 1),
('Restaurante El Gourmet', 'reservas@elgourmet.com', '555-0115', 'Calle de los Sabores 7', '2026-06-08 18:20:00', 1),
('Servicios de Limpieza Brillante', 'contacto@brillante.com', '555-0116', 'Av. Limpia 10', '2026-06-15 11:10:00', 1),
('Librería El Sabio', 'pedidos@elsabio.com', '555-0117', 'Calle Lectura 80', '2026-07-02 10:00:00', 1),
('Taller Mecánico Rápido', 'citas@tallerrapi.com', '555-0118', 'Av. Motores 33', '2026-07-10 13:45:00', 1),
('Peluquería Estilo', 'reservas@estilo.com', '555-0119', 'Plaza de la Belleza 2', '2026-07-20 15:15:00', 1),
('Tienda de Mascotas Peludos', 'hola@peludos.com', '555-0120', 'Calle Animales 44', '2026-08-01 09:30:00', 1);

INSERT INTO Payments (CustomerId, Monto, MetodoPago, Estado, FechaPago, FechaCreacion) VALUES
((SELECT Id FROM Customers WHERE Email = 'contacto@techsolutions.com'), 1500.50, 'Transferencia', 'Completado', '2026-01-20 12:00:00', '2026-01-18 10:00:00'),
((SELECT Id FROM Customers WHERE Email = 'info@globallogistics.com'), 3200.00, 'Transferencia', 'Completado', '2026-01-25 15:30:00', '2026-01-24 14:00:00'),
((SELECT Id FROM Customers WHERE Email = 'hola@marketingcreativo.com'), 850.75, 'Tarjeta', 'Completado', '2026-02-10 09:15:00', '2026-02-08 08:00:00'),
((SELECT Id FROM Customers WHERE Email = 'admin@alfa-consulting.com'), 4100.00, 'Transferencia', 'Completado', '2026-02-20 16:00:00', '2026-02-18 11:30:00'),
((SELECT Id FROM Customers WHERE Email = 'ventas@distnorte.com'), 5600.20, 'Efectivo', 'Completado', '2026-03-05 10:45:00', '2026-03-03 09:20:00'),
((SELECT Id FROM Customers WHERE Email = 'soporte@softwareymas.com'), 1200.00, 'Tarjeta', 'Pendiente', '2026-03-15 00:00:00', '2026-03-12 14:10:00'),
((SELECT Id FROM Customers WHERE Email = 'contacto@ecoenvases.com'), 2350.50, 'Transferencia', 'Completado', '2026-03-28 11:20:00', '2026-03-26 10:05:00'),
((SELECT Id FROM Customers WHERE Email = 'clientes@finanzasseguras.com'), 8900.00, 'Transferencia', 'Completado', '2026-04-10 14:30:00', '2026-04-05 12:00:00'),
((SELECT Id FROM Customers WHERE Email = 'info@horizonte.com'), 12500.00, 'Transferencia', 'Pendiente', '2026-04-25 00:00:00', '2026-04-20 16:45:00'),
((SELECT Id FROM Customers WHERE Email = 'reservas@viajesmundo.com'), 3450.00, 'Tarjeta', 'Fallido', '2026-05-02 09:00:00', '2026-05-01 10:30:00'),
((SELECT Id FROM Customers WHERE Email = 'contacto@techsolutions.com'), 1750.00, 'Transferencia', 'Completado', '2026-05-15 13:15:00', '2026-05-10 09:00:00'),
((SELECT Id FROM Customers WHERE Email = 'recepcion@bienestar.com'), 920.50, 'Efectivo', 'Completado', '2026-05-20 11:45:00', '2026-05-18 10:20:00'),
((SELECT Id FROM Customers WHERE Email = 'inscripciones@edumoderna.com'), 4500.00, 'Transferencia', 'Pendiente', '2026-06-05 00:00:00', '2026-06-02 14:00:00'),
((SELECT Id FROM Customers WHERE Email = 'proveedores@ahorro.com'), 6700.80, 'Transferencia', 'Completado', '2026-06-12 15:30:00', '2026-06-10 11:10:00'),
((SELECT Id FROM Customers WHERE Email = 'info@fitlife.com'), 350.00, 'Tarjeta', 'Completado', '2026-06-20 08:45:00', '2026-06-18 07:30:00'),
((SELECT Id FROM Customers WHERE Email = 'reservas@elgourmet.com'), 1800.00, 'Efectivo', 'Pendiente', '2026-07-05 00:00:00', '2026-07-01 18:20:00'),
((SELECT Id FROM Customers WHERE Email = 'contacto@brillante.com'), 540.25, 'Transferencia', 'Completado', '2026-07-15 10:15:00', '2026-07-12 09:40:00'),
((SELECT Id FROM Customers WHERE Email = 'pedidos@elsabio.com'), 1250.00, 'Tarjeta', 'Completado', '2026-07-25 14:00:00', '2026-07-22 13:00:00'),
((SELECT Id FROM Customers WHERE Email = 'citas@tallerrapi.com'), 3100.50, 'Transferencia', 'Pendiente', '2026-08-05 00:00:00', '2026-08-02 11:30:00'),
((SELECT Id FROM Customers WHERE Email = 'hola@peludos.com'), 420.00, 'Efectivo', 'Completado', '2026-08-08 16:45:00', '2026-08-05 10:15:00');
