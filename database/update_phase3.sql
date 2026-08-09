USE customer_payment_db;
-- 1. Agregar FechaCreacion
ALTER TABLE Payments ADD COLUMN FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- 2. Modificar SPs para incluir FechaCreacion y crear el nuevo SP UpdateStatus
DELIMITER //

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
END//

DROP PROCEDURE IF EXISTS sp_Payment_GetById//
CREATE PROCEDURE sp_Payment_GetById(IN p_Id INT)
BEGIN
    SELECT p.Id, p.CustomerId, c.Nombre AS CustomerNombre, p.Monto, p.MetodoPago, p.FechaPago, p.Estado, p.FechaCreacion
    FROM Payments p
    INNER JOIN Customers c ON p.CustomerId = c.Id
    WHERE p.Id = p_Id;
END//

DROP PROCEDURE IF EXISTS sp_Payment_UpdateStatus//
CREATE PROCEDURE sp_Payment_UpdateStatus(
    IN p_Id INT,
    IN p_Estado VARCHAR(20)
)
BEGIN
    UPDATE Payments 
    SET Estado = p_Estado 
    WHERE Id = p_Id;
    
    SELECT ROW_COUNT() AS FilasAfectadas;
END//

DELIMITER ;
