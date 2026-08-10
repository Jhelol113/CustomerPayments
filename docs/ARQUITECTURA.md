# 📘 Guía Completa de Arquitectura — Customer-Payment System

> **Objetivo de este documento**: Que puedas entender, explicar y defender cómo funciona cada pieza de este sistema, desde que el usuario hace click en el navegador hasta que el dato llega a MySQL y regresa pintado en pantalla.

---

## 📑 Tabla de Contenidos

1. [Vista General de la Arquitectura](#1-vista-general-de-la-arquitectura)
2. [El Frontend (React + Vite + TypeScript)](#2-el-frontend-react--vite--typescript)
3. [El Backend (.NET 8 Web API)](#3-el-backend-net-8-web-api)
   - 3.1 Arquitectura en Capas (N-Tier)
   - 3.2 ¿Qué son los DTOs?
   - 3.3 Inyección de Dependencias
   - 3.4 Pipeline de Middlewares
   - **3.5 Análisis Profundo: Program.cs, AppDbContext, Entities y Validadores**
4. [La Base de Datos (MySQL + Stored Procedures)](#4-la-base-de-datos-mysql--stored-procedures)
5. [Flujo Completo: Login y Autenticación JWT](#5-flujo-completo-login-y-autenticación-jwt)
6. [Flujo Completo: CRUD de Customer](#6-flujo-completo-crud-de-customer)
7. [Flujo Completo: CRUD de Payment](#7-flujo-completo-crud-de-payment)
8. [Seguridad del Sistema](#8-seguridad-del-sistema)
9. [Docker y Despliegue](#9-docker-y-despliegue)
10. [Glosario de Conceptos Clave](#10-glosario-de-conceptos-clave)

---

## 1. Vista General de la Arquitectura

Nuestro sistema tiene **3 capas principales** que se comunican entre sí:

```
┌──────────────────────────────────────────────────────────────────────┐
│                        USUARIO (Navegador)                          │
└────────────────────────────────┬─────────────────────────────────────┘
                                 │ HTTP (puerto 5173)
                                 ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     FRONTEND (React + Vite)                         │
│  ┌────────┐  ┌────────────┐  ┌──────────────┐  ┌──────────────────┐ │
│  │ Pages  │→ │ Components │→ │   Services   │→ │  Axios (api.ts)  │ │
│  │        │  │ (Atomic)   │  │ (customer,   │  │  Bearer <JWT>    │ │
│  │        │  │            │  │  payment,    │  │                  │ │
│  │        │  │            │  │  auth)       │  │                  │ │
│  └────────┘  └────────────┘  └──────────────┘  └────────┬─────────┘ │
└─────────────────────────────────────────────────────────┼───────────┘
                                                          │ HTTP (puerto 5263)
                                                          ▼
┌──────────────────────────────────────────────────────────────────────┐
│                      BACKEND (.NET 8 Web API)                       │
│  ┌─────────────┐  ┌────────────┐  ┌──────────────┐  ┌───────────┐  │
│  │ Controllers │→ │  Services  │→ │ Repositories │→ │ DbContext │  │
│  │ (HTTP)      │  │ (Negocio)  │  │ (Datos)      │  │ (EF Core) │  │
│  └─────────────┘  └────────────┘  └──────────────┘  └─────┬─────┘  │
│                                                            │        │
│  ┌─────────────────┐  ┌──────────────────────────────────┐ │        │
│  │ JWT Middleware   │  │ SecurityHeaders Middleware       │ │        │
│  └─────────────────┘  └──────────────────────────────────┘ │        │
└────────────────────────────────────────────────────────────┼────────┘
                                                             │ TCP (puerto 3306)
                                                             ▼
┌──────────────────────────────────────────────────────────────────────┐
│                      BASE DE DATOS (MySQL 8.0)                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────────────────┐  │
│  │  Users   │  │Customers │  │ Payments │  │ Stored Procedures   │  │
│  └──────────┘  └──────────┘  └──────────┘  │ sp_Customer_*       │  │
│                     1 ──────── N            │ sp_Payment_*        │  │
│                                            │ sp_User_*           │  │
│                                            └─────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### ¿Por qué esta separación?
- **Desacoplamiento**: Cada capa solo conoce a la capa inmediatamente inferior. El Controller no sabe que MySQL existe. El Repository no sabe qué HTTP status devolver.
- **Mantenibilidad**: Si mañana cambias MySQL por PostgreSQL, solo tocas los Repositories. El resto del sistema ni se entera.
- **Testabilidad**: Puedes probar cada capa de forma independiente usando mocks de las interfaces.

---

## 2. El Frontend (React + Vite + TypeScript)

### 2.1 Estructura de Carpetas (Atomic Design)

```
CustomerPaymentVisual/src/
├── components/
│   ├── atoms/          ← Piezas más pequeñas e indivisibles
│   ├── molecules/      ← Combinaciones de átomos
│   ├── organisms/      ← Secciones completas (Gráficos, Tarjetas)
│   └── layout/         ← Estructura visual de la app
├── pages/              ← Páginas completas (una por ruta)
├── services/           ← Comunicación con el Backend
├── context/            ← Estado global de autenticación (React Context)
├── hooks/              ← Lógica reutilizable
├── types/              ← Interfaces TypeScript del sistema
├── App.tsx             ← Rutas y protección de rutas
└── main.tsx            ← Punto de entrada de React
```

### 2.2 ¿Cómo se comunica el Frontend con el Backend?

Todo pasa por el archivo `api.ts`, que configura **Axios** (un cliente HTTP). Implementa interceptores que inyectan el JWT automáticamente en las peticiones y detectan si la sesión expiró para expulsar al usuario.

---

## 3. El Backend (.NET 8 Web API)

### 3.1 Arquitectura en Capas (N-Tier)

El backend sigue una separación estricta de responsabilidades:
- **CONTROLLERS:** Capa de presentación (HTTP). Delegan en el Service.
- **SERVICES:** Lógica de negocio y mapeo de DTOs.
- **REPOSITORIES:** Acceso a datos. Ejecutan los Stored Procedures.

### 3.2 ¿Qué son los DTOs y por qué existen?

**DTO (Data Transfer Object)** es un objeto que transporta datos entre capas.
- `CustomerRequestDto`: Lo que el usuario ENVÍA (nombre, email).
- `CustomerResponseDto`: Lo que el usuario RECIBE (id, nombre, fechaCreacion).
- `Customer` (Entity): Representación de la tabla en la BD. NUNCA sale del backend.

### 3.3 Inyección de Dependencias (Program.cs)

.NET crea e inyecta automáticamente las clases según su ciclo de vida (`AddScoped`, `AddSingleton`). Esto permite depender de interfaces y facilita las pruebas.

### 3.4 Pipeline de Middlewares

Orden estricto de ejecución para cada petición HTTP:
`SecurityHeaders` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.

---

### 3.5 Análisis Profundo: Program.cs, AppDbContext, Entities y Validadores

Esta subsección es clave para entender cómo arranca el motor del Backend y cómo se aseguran los datos antes de tocar la base de datos.

#### A. `Program.cs` — El cerebro del arranque
Es el archivo principal que se ejecuta al encender la API. Aquí se conectan todas las mangueras. Hace 5 cosas fundamentales:
1. **Configura la Base de Datos:** Lee la cadena de conexión y enlaza `AppDbContext` con MySQL usando Pomelo.
2. **Registra la Inyección de Dependencias (DI):** Le enseña a .NET qué clase instanciar cuando alguien pide una interfaz. Ej: `AddScoped<ICustomerService, CustomerService>()`.
3. **Configura la Seguridad (JWT y CORS):** Define quién puede consumir la API (`localhost:5173`) y cómo validar matemáticamente el Token JWT sin ir a la base de datos (con `SecretKey`).
4. **Conecta los Validadores (FluentValidation):** Escanea el proyecto buscando clases validadoras y las inyecta en el pipeline HTTP automáticamente.
5. **Crea el Middleware Pipeline:** Define el túnel exacto por donde pasa cada petición entrante (CORS -> Auth -> Controllers). Adicionalmente, incluye un bloque de "Seed" que crea el usuario `admin` automáticamente si no existe.

#### B. `Entities` — El reflejo de las tablas
Las **Entities** (`Customer.cs`, `Payment.cs`, `User.cs`) son clases de C# que actúan como "espejos" de las tablas de MySQL.
- Poseen propiedades que coinciden exactamente con las columnas de la tabla.
- **Importante:** Estas clases *nunca* se envían al Frontend (eso lo hacen los DTOs). Son de uso exclusivo para las capas internas (Repository y Service) para mapear resultados que vienen de la base de datos.

#### C. `AppDbContext` — El traductor hacia la Base de Datos
Es el corazón de Entity Framework Core. En nuestra arquitectura, su rol es muy específico:
- Define los `DbSet<T>` (`Customers`, `Payments`, `Users`) que sirven como "recipientes" para los datos.
- Define las reglas de mapeo en `OnModelCreating`, como por ejemplo la relación 1:N entre Customer y Payment, estableciendo `OnDelete(DeleteBehavior.Restrict)` para proteger la integridad.
- **¿Cómo lo usamos?** A diferencia de aplicaciones donde EF Core autogenera SQL, nosotros usamos `AppDbContext` principalmente para invocar `FromSqlRaw("CALL sp_Customer_GetAll")`. Esto significa que EF Core toma la salida del Stored Procedure de MySQL y la mapea mágicamente dentro de las listas de nuestras Entities. Para inserts o updates usamos ADO.NET (`GetDbConnection()`) a través del mismo contexto.

#### D. `Validators` (FluentValidation) — El filtro de seguridad
Antes de que un DTO llegue a tu Controller, debe pasar por los validadores (`CustomerRequestValidator.cs`, `PaymentRequestValidator.cs`).
- Usamos la librería **FluentValidation** porque separa las reglas de validación de la clase DTO, manteniendo el código limpio.
- **Ejemplo en código:**
  ```csharp
  RuleFor(x => x.Email)
      .NotEmpty().WithMessage("El email es obligatorio")
      .EmailAddress().WithMessage("Formato de email inválido");
  ```
- **El flujo automático:** Cuando React hace un POST a `/api/customers`, .NET intercepta el JSON, crea el DTO, y ejecuta automáticamente este validador. Si falla, .NET *aborta* la petición y responde con un `HTTP 400 Bad Request` indicando el error exacto ("El email es obligatorio"), sin siquiera tocar tu Controller ni tu lógica de negocio. Esto ahorra procesamiento y evita inyecciones de datos basura.

---

## 4. La Base de Datos (MySQL + Stored Procedures)

### 4.1 Esquema Relacional

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│    Users     │       │  Customers   │       │   Payments   │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ Id (PK)      │       │ Id (PK)      │──1:N─→│ Id (PK)      │
│ Username     │       │ Nombre       │       │ CustomerId(FK│
│ PasswordHash │       │ Email        │       │ Monto        │
│ Rol          │       │ Telefono     │       │ MetodoPago   │
│ FechaCreacion│       │ Direccion    │       │ FechaPago    │
│ Activo       │       │ FechaCreacion│       │ Estado       │
└──────────────┘       │ Activo       │       │ FechaCreacion│
                       └──────────────┘       └──────────────┘
```

- **Users**: Autenticación. Almacena credenciales hasheadas con BCrypt.
- **Customers**: Clientes. Usa **Soft Delete** (campo `Activo = FALSE`).
- **Payments**: Pagos. Usa **Hard Delete** (borrado físico). Relación 1:N protegida con `ON DELETE RESTRICT`.

### 4.2 ¿Por qué Stored Procedures y no consultas directas?

| Aspecto | Consultas directas (LINQ) | Stored Procedures ✓ |
|---|---|---|
| **Seguridad** | Podría haber SQL Injection | Parámetros tipados, protección nativa |
| **Rendimiento** | Se compila en cada ejecución | Precompilado en el motor de BD |
| **Control DBA** | Código disperso en C# | Lógica centralizada en la base de datos |

### 4.3 Técnicas de ejecución en los Repositories

**1. Lecturas (`FromSqlRaw`)**: EF Core invoca el SP y mapea las columnas automáticamente a una lista de Entities.
**2. Escrituras (ADO.NET directo)**: Usamos `ExecuteScalarAsync()` para SPs que no retornan tablas, sino valores escalares como el `LAST_INSERT_ID()` de MySQL o el `ROW_COUNT()`.

---

## 5. Flujo Completo: Login y Autenticación JWT

Este es el flujo más importante del sistema:

1. **React envía credenciales:** POST a `/api/auth/login`.
2. **Backend recibe:** `AuthController` delega en `AuthService`.
3. **Búsqueda en BD:** `UserRepository` invoca el SP de consulta y retorna el usuario con su Hash.
4. **Verificación BCrypt:** Compara la contraseña en texto plano contra el Hash almacenado (que tiene un Salt interno).
5. **Generación de JWT:** `JwtHelper` crea un Token firmado por `SecretKey` (incluye Rol y expiración).
6. **Respuesta 200 OK:** Frontend recibe el Token y lo almacena en `localStorage`.
7. **Peticiones posteriores:** El interceptor de Axios anexa el Token. El Middleware `.UseAuthentication()` del Backend lo valida automáticamente.

---

## 6. Flujo Completo: CRUD de Customer

### 6.1 Crear Cliente (POST)
- **Frontend** envía JSON.
- **Validators** frenan si falta el Nombre o el Email es inválido (HTTP 400 automático).
- **Service** convierte el DTO válido a Entity.
- **Repository** invoca `CALL sp_Customer_Create`, retorna el Id y busca los datos finales (incluyendo fecha de MySQL).
- **Service** convierte Entity de vuelta a DTO y lo envía a Frontend (HTTP 201).

### 6.2 Eliminar Cliente (Soft Delete con regla de negocio)
```
React → Controller → Service:
  ¿Tiene pagos Pendientes? (Llamada al PaymentRepository)
  ├─ SÍ: throw InvalidOperationException → HTTP 409 Conflict.
  └─ NO: Llamada a CustomerRepository → `UPDATE Customers SET Activo = 0` → HTTP 204.
```

---

## 7. Flujo Completo: CRUD de Payment

### 7.1 Crear Pago (con validación cruzada)
Antes de insertar, el **PaymentService** verifica que el Customer ID exista invocando al `CustomerRepository`. Si no existe, se aborta y retorna HTTP 400.

### 7.2 Cambiar Estado (PATCH)
Botón "Completar" → Llama a `/api/payments/{id}/status` con body `{ "estado": "Completado" }`.
El SP en MySQL actualiza únicamente la columna `Estado` para ese ID. Retorna HTTP 204.

---

## 8. Seguridad del Sistema

### Capas de Seguridad Implementadas

| Capa | Mecanismo | ¿Qué protege? |
|---|---|---|
| **Autenticación** | JWT con firma HMAC-SHA256 | Acceso a Endpoints protegidos |
| **Contraseñas** | BCrypt (Work Factor 11) | Rainbow Tables y ataques de fuerza bruta |
| **CORS** | Whitelist (`localhost:5173`) | Bloquea peticiones desde otras webs |
| **Headers OWASP** | SecurityHeadersMiddleware | Previene XSS, Clickjacking, Sniffing |
| **Validación** | FluentValidation | Bloquea datos corruptos/maliciosos |
| **Inactividad** | Timer React (5 min) | Sesiones de usuario olvidadas abiertas |
| **Base de Datos** | Stored Procedures y FKs | Inyección SQL y pérdida de integridad |

---

## 9. Docker y Despliegue

### 9.1 Orquestación con Docker Compose

`docker-compose.yml` levanta 3 contenedores:
1. `customer_payment_db` (MySQL 3307)
2. `customer_payment_api` (.NET 5263)
3. `customer_payment_frontend` (React 5173)

### 9.2 Automatización Total
No hay que instalar ni configurar bases de datos a mano.
Al hacer `docker-compose up -d`:
- Se monta un Volumen Persistente para que la BD no se borre al apagar Docker.
- Se ejecuta automáticamente `init.sql`, creando tablas, SPs y datos de prueba.
- El API en .NET se conecta al contenedor `db` mediante la red interna de Docker.

---

## 10. Glosario de Conceptos Clave

| Concepto | Definición |
|---|---|
| **N-Tier** | Arquitectura que separa la aplicación en capas (Presentación, Negocio, Datos). |
| **Atomic Design** | Organiza componentes UI en átomos, moléculas y organismos. |
| **DTO** | Objeto que transporta datos seguros entre capas. |
| **Entity** | Clase C# que espejea una tabla de la base de datos. |
| **Repository** | Clase que maneja exclusivamente el acceso a datos (Stored Procedures). |
| **Service** | Clase con la lógica de negocio y mapeo DTO ↔ Entity. |
| **Controller** | Clase receptora HTTP, delega inmediatamente. |
| **FluentValidation** | Librería C# que valida el JSON de entrada de forma automática. |
| **AppDbContext** | Motor central de Entity Framework Core para conectarse a la DB. |
| **JWT** | JSON Web Token, firmado, permite saber quién eres sin consultar la BD. |
| **BCrypt** | Algoritmo lento de hashing con Salting automático, estándar para passwords. |
| **Soft Delete** | Borrado lógico (ocultar) para no romper el historial de la DB. |
| **Stored Procedure** | Lógica SQL compilada y guardada dentro de MySQL. |
