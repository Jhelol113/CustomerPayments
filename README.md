# 💼 Customer-Payment CRUD System

Un sistema empresarial Full-Stack diseñado con arquitectura en capas (N-Tier), Frontend estructurado mediante **Atomic Design**, y completamente contenerizado con **Docker**. 

Este proyecto gestiona Clientes, Pagos, incluye un Dashboard analítico con gráficos interactivos y está protegido mediante autenticación con **JSON Web Tokens (JWT)**.

---

## 🏗️ Arquitectura del Proyecto

El sistema está dividido en tres entornos orquestados por Docker Compose:

1. **Frontend (React + Vite + TypeScript)**:
   - Implementa Diseño Atómico (atoms, molecules, organisms, pages, templates).
   - Consume APIs y visualiza datos mediante Recharts.
   - Servido en producción con **Nginx**.

2. **Backend (.NET 8 Web API)**:
   - Arquitectura N-Tier (Controllers, Services, Repositories, Entities, DTOs).
   - Entity Framework Core interactuando mediante **Stored Procedures**.
   - Validación robusta usando FluentValidation.
   - Seguridad OWASP (Security Headers) y JWT.

3. **Base de Datos (MySQL 8.0)**:
   - Esquema relacional robusto con llaves foráneas y eliminación lógica (Soft Delete).
   - Base de datos inicializada automáticamente mediante script maestro (`init.sql`).

---

## 🚀 Guía Rápida para Levantar el Entorno (Docker)

No necesitas instalar Node.js, .NET SDK o MySQL en tu máquina. Todo está encapsulado en Docker.

### Requisitos Previos:
- Tener instalado **Docker** y **Docker Compose** en tu computadora.

### Pasos:

1. **Clona este repositorio**:
   ```bash
   git clone https://github.com/Jhelol113/CustomerPayments.git
   cd CustomerPayments
   ```

2. **Levanta la infraestructura**:
   Ejecuta el siguiente comando en la raíz del proyecto para descargar imágenes, compilar el código fuente y levantar los 3 servidores:
   ```bash
   docker-compose up --build -d
   ```
   *(La bandera `-d` lo ejecuta en segundo plano).*

3. **Accede al Sistema**:
   - 🎨 **Aplicación Web (Frontend)**: [http://localhost:5173](http://localhost:5173)
   - ⚙️ **Documentación API (Swagger)**: [http://localhost:5041/swagger](http://localhost:5041/swagger)
   - 🗄️ **Conexión MySQL local (Opcional)**: Puerto `3307`, Usuario `root`, Contraseña `rootpassword123`.

---

## 🔐 Credenciales de Acceso

Al levantar la base de datos por primera vez, el sistema autogenera un usuario Administrador y carga 20 clientes y 20 pagos (Seed Data) para que puedas ver el dashboard funcionando inmediatamente.

Para ingresar a la aplicación web, utiliza:
- **Usuario:** `admin`
- **Contraseña:** `Admin123!`

---

## 🛑 Detener el Sistema

Cuando termines de trabajar, puedes apagar los contenedores y liberar recursos de tu computadora ejecutando:
```bash
docker-compose down
```
*(Nota: Tu información guardada no se perderá, ya que Docker guarda la base de datos en un volumen persistente `db_data`).*
