-- =====================================================================
-- ACTUALIZACIÓN: Eliminar columna Referencia de la tabla Payments
-- =====================================================================
USE customer_payment_db;

-- 1. Eliminar la columna
ALTER TABLE Payments DROP COLUMN Referencia;

-- 2. Actualizar SPs
DELIMITER //

DROP PROCEDURE IF EXISTS sp_Payment_GetAll//
CREATE PROCEDURE sp_Payment_GetAll(IN p_CustomerId INT)
BEGIN
    IF p_CustomerId IS NULL THEN
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        ORDER BY p.FechaPago DESC;
    ELSE
        SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado
        FROM Payments p
        INNER JOIN Customers c ON p.CustomerId = c.Id
        WHERE p.CustomerId = p_CustomerId
        ORDER BY p.FechaPago DESC;
    END IF;
END//

DROP PROCEDURE IF EXISTS sp_Payment_GetById//
CREATE PROCEDURE sp_Payment_GetById(IN p_Id INT)
BEGIN
    SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado
    FROM Payments p
    INNER JOIN Customers c ON p.CustomerId = c.Id
    WHERE p.Id = p_Id;
END//

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
END//

DROP PROCEDURE IF EXISTS sp_Payment_Update//
CREATE PROCEDURE sp_Payment_Update(
    IN p_Id INT,
    IN p_CustomerId INT,
    IN p_Monto DECIMAL(18,2),
    IN p_MetodoPago VARCHAR(50),
    IN p_Estado VARCHAR(20)
)
BEGIN
    UPDATE Payments SET CustomerId = p_CustomerId, Monto = p_Monto, MetodoPago = p_MetodoPago, Estado = p_Estado
    WHERE Id = p_Id;
    SELECT ROW_COUNT() AS FilasAfectadas;
END//

DELIMITER ;
