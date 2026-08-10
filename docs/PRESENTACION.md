# Customer-Payment System — Presentación de Arquitectura

## Resumen Ejecutivo

Este sistema gestiona **Clientes y Pagos** mediante una arquitectura empresarial de tres capas, completamente contenerizada con Docker. Permite registrar clientes, administrar sus pagos, visualizar métricas en un Dashboard analítico y controlar el acceso mediante autenticación JWT.

**Stack tecnológico:**
- **Frontend:** React 18 + TypeScript + Vite (Diseño Atómico)
- **Backend:** .NET 8 Web API (Arquitectura N-Tier con Stored Procedures)
- **Base de Datos:** MySQL 8.0 (Procedimientos Almacenados)
- **Infraestructura:** Docker Compose (3 contenedores orquestados)

---

## 1. ¿Por qué esta arquitectura?

### Separación en 3 capas independientes

```
  FRONTEND (React)  ──HTTP/JSON──▶  BACKEND (.NET)  ──SQL──▶  BASE DE DATOS (MySQL)
```

Cada capa corre en su propio contenedor Docker y se comunica únicamente por protocolos estándar (HTTP y SQL). Esto significa que:

- **Se pueden escalar por separado.** Si el Frontend tiene mucho tráfico, escalo solo ese contenedor.
- **Se pueden reemplazar por separado.** Si mañana decido cambiar React por Angular, el Backend no se entera. Si cambio MySQL por PostgreSQL, solo toco la capa de Repositories.
- **Se pueden desarrollar en paralelo.** Un equipo trabaja en el Frontend mientras otro trabaja en el Backend, comunicándose únicamente a través del contrato de la API (endpoints documentados en Swagger).

---

## 2. Frontend — ¿Cómo está organizado?

### Patrón: Diseño Atómico

Organizo los componentes de interfaz en niveles de complejidad creciente:

| Nivel | Ejemplo | Responsabilidad |
|---|---|---|
| **Átomos** | `Button`, `Input`, `StatusBadge` | Piezas visuales mínimas. No contienen lógica de negocio. |
| **Moléculas** | `FormField`, `DataTable` | Combinan átomos para crear elementos funcionales. |
| **Organismos** | `KPICard`, `PaymentTrendChart` | Secciones completas con lógica propia (gráficos, tarjetas). |
| **Páginas** | `CustomersPage`, `KPIsPage` | Pantallas completas que orquestan organismos y consumen servicios. |

**¿Por qué Diseño Atómico?** Porque cada componente es **reutilizable**. El mismo `Button` con variante `danger` se usa en "Eliminar Cliente" y en "Eliminar Pago". El mismo `DataTable` renderiza tanto la tabla de clientes como la de pagos, simplemente cambiando las columnas que recibe por props.

### Comunicación con el Backend

El Frontend **nunca** habla directamente con la base de datos. Toda comunicación pasa por una capa de servicios:

```
Página → Servicio (customerService.ts) → Axios (api.ts) → HTTP → Backend
```

El archivo `api.ts` centraliza dos comportamientos críticos:
1. **Interceptor de envío:** Antes de cada petición, inyecta automáticamente el token JWT en el header `Authorization`.
2. **Interceptor de respuesta:** Si el Backend responde con `401 Unauthorized`, cierra la sesión y redirige al login.

Esto garantiza que **ningún componente** tenga que preocuparse por manejar tokens o sesiones expiradas manualmente.

### Protección de Rutas

```
/login       → Pública
/dashboard   → Requiere autenticación
/customers   → Requiere autenticación
/payments    → Requiere autenticación
```

Un componente `ProtectedRoute` verifica si existe un token válido en el estado global (`AuthContext`). Si no lo hay, redirige al login. Adicionalmente, implementé un **timer de inactividad de 5 minutos** que cierra la sesión automáticamente si el usuario deja la aplicación abierta sin interactuar.

---

## 3. Backend — ¿Cómo está organizado?

### Patrón: Arquitectura N-Tier (Capas)

El Backend está dividido en **4 capas** con responsabilidades estrictamente separadas:

```
Petición HTTP
     │
     ▼
 CONTROLLER ──── Recibe HTTP, delega al Service, devuelve el Response.
     │            Nunca contiene lógica de negocio.
     ▼
 SERVICE ─────── Valida reglas de negocio, mapea DTOs ↔ Entidades.
     │            Ejemplo: "¿Tiene pagos pendientes antes de eliminar?"
     ▼
 REPOSITORY ──── Ejecuta los Stored Procedures contra MySQL.
     │            Nunca contiene lógica de negocio.
     ▼
 MYSQL (SPs) ─── Retorna datos crudos.
```

**¿Por qué esta separación?** Porque cada capa tiene una única razón para cambiar:

- Si cambio la respuesta HTTP (de `200` a `204`), solo toco el **Controller**.
- Si cambio una regla de negocio (permitir eliminar clientes con pagos), solo toco el **Service**.
- Si cambio la base de datos (de MySQL a PostgreSQL), solo toco el **Repository**.

### El rol de los DTOs (Data Transfer Objects)

Los DTOs son la **barrera de seguridad** entre las capas:

```
Frontend ◄──── CustomerResponseDto ────── Service
                (solo id, nombre, email,     │
                 telefono, fechaCreacion)     │ Mapeo interno
                                              │
                                       Customer (Entity)
                                       (incluye PasswordHash,
                                        propiedades de navegación,
                                        campos internos)
```

- **`CustomerRequestDto`**: Lo que el usuario **envía** (nombre, email). No incluye Id ni FechaCreacion porque esos los genera la base de datos.
- **`CustomerResponseDto`**: Lo que el usuario **recibe** (id, nombre, email, fechaCreacion). No incluye campos sensibles ni propiedades de navegación de Entity Framework.
- **`Customer` (Entity)**: Representación interna de la tabla. **Nunca sale del backend.**

### Inyección de Dependencias

Todas las dependencias se resuelven automáticamente mediante el contenedor de .NET:

```csharp
// Program.cs — Registro de dependencias
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

Cuando el `CustomerController` solicita un `ICustomerService` en su constructor, .NET automáticamente:
1. Crea un `CustomerService`.
2. Detecta que `CustomerService` necesita un `ICustomerRepository`.
3. Crea un `CustomerRepository`.
4. Inyecta todo en cadena.

**¿Por qué interfaces?** Porque el Controller depende de `ICustomerService` (abstracción), no de `CustomerService` (implementación concreta). Esto permite reemplazar la implementación sin modificar ningún consumidor, y facilita los unit tests con mocks.

---

## 4. Base de Datos — ¿Por qué Stored Procedures?

### Decisión arquitectónica

En lugar de escribir consultas SQL directamente en el código C# (lo cual es válido con Entity Framework), elegí **Stored Procedures** por estas razones:

| Criterio | Consultas en código | Stored Procedures ✓ |
|---|---|---|
| **Seguridad** | Riesgo de SQL Injection si se concatenan strings | Los parámetros se tipan, eliminando el riesgo |
| **Rendimiento** | Se compila en cada ejecución | Se compila una vez en MySQL y se reutiliza |
| **Control del DBA** | Los desarrolladores escriben SQL libremente | El DBA define y audita exactamente qué se ejecuta |
| **Separación de responsabilidades** | La lógica SQL vive mezclada con C# | La lógica SQL vive en la base de datos |

### Ejecución desde C#

Los Repositories usan dos técnicas según el tipo de operación:

**Lecturas (SELECT)** — Uso `FromSqlRaw` de EF Core:
```csharp
var customers = await _context.Customers
    .FromSqlRaw("CALL sp_Customer_GetAll")
    .AsNoTracking()  // Optimización: no rastrear cambios en modo lectura
    .ToListAsync();
```

**Escrituras (INSERT/UPDATE/DELETE)** — Uso ADO.NET directo:
```csharp
command.CommandText = "sp_Customer_Create";
command.CommandType = CommandType.StoredProcedure;
command.Parameters.Add(new MySqlParameter("@p_Nombre", customer.Nombre));
var resultado = await command.ExecuteScalarAsync();  // Retorna LAST_INSERT_ID()
```

La razón del cambio de técnica es que `FromSqlRaw` mapea filas completas a Entities, pero los SPs de escritura retornan valores escalares (`LAST_INSERT_ID`, `ROW_COUNT`) que no encajan en ese mapeo.

---

## 5. Flujo de Autenticación (JWT)

El sistema implementa autenticación **stateless** con JSON Web Tokens:

```
┌───────────┐                    ┌───────────┐                    ┌───────────┐
│  FRONTEND │                    │  BACKEND  │                    │   MYSQL   │
└─────┬─────┘                    └─────┬─────┘                    └─────┬─────┘
      │                                │                                │
      │  POST /api/auth/login          │                                │
      │  { username, password }        │                                │
      │───────────────────────────────▶│                                │
      │                                │  CALL sp_User_GetByUsername    │
      │                                │───────────────────────────────▶│
      │                                │◀───────────────────────────────│
      │                                │  User { PasswordHash }         │
      │                                │                                │
      │                                │  BCrypt.Verify(password, hash) │
      │                                │  ✓ Válido                      │
      │                                │                                │
      │                                │  JwtHelper.GenerateToken()     │
      │                                │  → Firma con HMAC-SHA256       │
      │                                │                                │
      │  200 OK                        │                                │
      │  { token, username, rol }      │                                │
      │◀───────────────────────────────│                                │
      │                                │                                │
      │  localStorage.setItem(token)   │                                │
      │                                │                                │
      │  GET /api/customers            │                                │
      │  Authorization: Bearer <JWT>   │                                │
      │───────────────────────────────▶│                                │
      │                                │  Middleware valida firma + exp │
      │                                │  ✓ Token válido → continúa    │
      │                                │                                │
```

**Puntos clave:**
- Las contraseñas **nunca se almacenan en texto plano**. Se hashean con BCrypt (salt aleatorio + work factor 11).
- El token JWT contiene los **Claims** del usuario (Id, Username, Rol) pero **no la contraseña**.
- El token está **firmado** (no encriptado). Cualquiera puede leer el payload, pero nadie puede alterarlo sin la SecretKey.
- El middleware de autenticación valida **automáticamente** cada petición. Los Controllers no necesitan verificar tokens manualmente.

---

## 6. Reglas de Negocio Implementadas

### 6.1 Eliminación de Clientes (Soft Delete con validación)

**Regla:** No se puede eliminar un cliente si tiene pagos en estado "Pendiente".

```
CustomerService.DeleteAsync(id):
  1. ¿Existe el cliente? → Si no → return false → HTTP 404
  2. ¿Tiene pagos Pendientes? → Consulta PaymentRepository
     ├── Sí → throw InvalidOperationException → HTTP 409 Conflict
     └── No → CustomerRepository.DeleteAsync(id)
              → MySQL: UPDATE SET Activo = FALSE  (Soft Delete)
              → HTTP 204
```

**¿Por qué Soft Delete?** Porque si borramos físicamente un cliente que tiene pagos completados, perdemos el historial de transacciones. El campo `Activo = FALSE` lo oculta del listado sin destruir datos.

### 6.2 Creación de Pagos (Validación cruzada)

**Regla:** No se puede crear un pago para un cliente que no existe.

```
PaymentService.CreateAsync(dto):
  1. ¿Existe el cliente referenciado? → Consulta CustomerRepository
     ├── No → throw ArgumentException → HTTP 400 Bad Request
     └── Sí → PaymentRepository.CreateAsync(payment)
              → HTTP 201 Created
```

**¿Por qué validar en el Service y no solo con la Foreign Key?**
La FK de MySQL sí impediría la inserción, pero lanzaría un error técnico críptico. Validando en el Service, controlamos el mensaje de error y devolvemos algo legible para el usuario.

### 6.3 Integridad Referencial

```sql
FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE RESTRICT
```

`ON DELETE RESTRICT` garantiza que MySQL **bloquee** cualquier intento de borrar un Customer que tenga Payments asociados, como última red de seguridad a nivel de base de datos.

---

## 7. Seguridad — Capas de Protección

| # | Capa | Implementación | Protege contra |
|---|---|---|---|
| 1 | **Autenticación** | JWT con firma HMAC-SHA256 | Acceso no autorizado |
| 2 | **Hashing** | BCrypt con salt aleatorio | Filtración de contraseñas |
| 3 | **CORS** | Whitelist de orígenes permitidos | Peticiones desde dominios no autorizados |
| 4 | **Headers OWASP** | X-Frame-Options, CSP, HSTS | XSS, clickjacking, sniffing |
| 5 | **Validación** | FluentValidation + Data Annotations | Datos malformados o maliciosos |
| 6 | **SQL Injection** | Stored Procedures parametrizados | Inyección de código SQL |
| 7 | **Inactividad** | Timer de 5 minutos (Frontend) | Sesiones abandonadas |
| 8 | **Integridad** | FK + ON DELETE RESTRICT | Datos huérfanos en la BD |

---

## 8. Infraestructura Docker

### ¿Cómo funciona el despliegue?

Todo el sistema se levanta con **un solo comando**:

```bash
docker-compose up --build -d
```

Docker Compose crea 3 contenedores conectados en una red interna:

| Contenedor | Imagen Base | Puerto Expuesto | Función |
|---|---|---|---|
| `customer_payment_db` | `mysql:8.0` | 3307 | Base de datos con init.sql automático |
| `customer_payment_api` | `.NET 8 SDK → ASP.NET Runtime` | 5263 | API REST con Swagger |
| `customer_payment_frontend` | `Node 20 → Nginx` | 5173 | App React compilada servida por Nginx |

**Características DevOps:**
- **Volumen persistente** (`db_data`): Los datos de MySQL sobreviven al reinicio de contenedores.
- **init.sql automático**: Al crear el contenedor de BD por primera vez, Docker ejecuta automáticamente el script que crea tablas, Stored Procedures y datos semilla.
- **Multi-stage builds**: Tanto el Dockerfile del Backend como del Frontend usan builds en dos etapas (compilación + ejecución) para reducir el tamaño de la imagen final.
- **Portabilidad total**: Funciona en cualquier máquina con Docker instalado, sin necesidad de .NET SDK, Node.js o MySQL.

---

## 9. Estructura Completa del Proyecto

```
📦 CustomerPayments/
│
├── 📂 CustomerPaymentAPI/              ← BACKEND (.NET 8)
│   ├── Dockerfile
│   └── 📂 CustomerPaymentAPI/
│       ├── Controllers/                ← Capa HTTP (recibe peticiones)
│       │   ├── AuthController.cs
│       │   ├── CustomerController.cs
│       │   └── PaymentController.cs
│       ├── Services/                   ← Capa de Negocio (reglas y mapeo)
│       │   ├── Interfaces/
│       │   └── Implementations/
│       ├── Repositories/               ← Capa de Datos (ejecuta SPs)
│       │   ├── Interfaces/
│       │   └── Implementations/
│       ├── Entities/                   ← Modelos de la BD
│       ├── DTOs/                       ← Objetos de transferencia
│       ├── Data/                       ← DbContext (EF Core)
│       ├── Security/                   ← JWT + Headers OWASP
│       ├── Validators/                 ← FluentValidation
│       └── Program.cs                  ← Configuración central
│
├── 📂 CustomerPaymentVisual/           ← FRONTEND (React)
│   ├── Dockerfile
│   └── 📂 src/
│       ├── components/                 ← Diseño Atómico
│       │   ├── atoms/
│       │   ├── molecules/
│       │   ├── organisms/
│       │   └── layout/
│       ├── pages/                      ← Pantallas completas
│       ├── services/                   ← Comunicación con API
│       ├── context/                    ← Estado global (Auth)
│       ├── hooks/                      ← Lógica reutilizable
│       └── types/                      ← Interfaces TypeScript
│
├── 📂 database/                        ← SCRIPTS SQL
│   └── init.sql                        ← Tablas + SPs + Seed Data
│
├── 📂 docs/                            ← DOCUMENTACIÓN
│   ├── ARQUITECTURA.md
│   ├── PRESENTACION.md
│   ├── diagrama_clases.puml
│   ├── secuencial_login.puml
│   ├── secuencial_crud_customer.puml
│   └── secuencial_crud_payment.puml
│
├── docker-compose.yml                  ← Orquestador de contenedores
└── README.md                           ← Instrucciones de despliegue
```
