# 📘 Guía Completa de Arquitectura — Customer-Payment System

> **Objetivo de este documento**: Que puedas entender, explicar y defender cómo funciona cada pieza de este sistema, desde que el usuario hace click en el navegador hasta que el dato llega a MySQL y regresa pintado en pantalla.

---

## 📑 Tabla de Contenidos

1. [Vista General de la Arquitectura](#1-vista-general-de-la-arquitectura)
2. [El Frontend (React + Vite + TypeScript)](#2-el-frontend-react--vite--typescript)
3. [El Backend (.NET 8 Web API)](#3-el-backend-net-8-web-api)
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
│   │   ├── Button.tsx          → Botón reutilizable con variantes (primary, danger, icon)
│   │   ├── Input.tsx           → Campo de texto estilizado
│   │   ├── Modal.tsx           → Ventana emergente reutilizable
│   │   ├── Select.tsx          → Menú desplegable
│   │   └── StatusBadge.tsx     → Etiqueta visual de estado (Completado, Pendiente, Fallido)
│   │
│   ├── molecules/      ← Combinaciones de átomos
│   │   ├── FormField.tsx       → Label + Input/Select (campo de formulario completo)
│   │   └── DataTable.tsx       → Tabla de datos genérica con columnas configurables
│   │
│   ├── organisms/      ← Secciones completas de la interfaz
│   │   ├── KPICard.tsx                    → Tarjeta con ícono + número + label
│   │   ├── PaymentStatusChart.tsx         → Gráfico circular (Recharts)
│   │   ├── PaymentTrendChart.tsx          → Gráfico de área mensual (Recharts)
│   │   └── CustomerTotalPaymentsChart.tsx → Gráfico de barras por cliente (Recharts)
│   │
│   └── layout/         ← Estructura visual de la app
│       ├── Layout.tsx          → Contenedor principal (sidebar + contenido)
│       └── Sidebar.tsx         → Barra lateral de navegación
│
├── pages/              ← Páginas completas (una por ruta)
│   ├── LoginPage.tsx           → Formulario de inicio de sesión
│   ├── CustomersPage.tsx       → CRUD de clientes (tabla + modales)
│   ├── PaymentsPage.tsx        → CRUD de pagos con filtro por cliente
│   └── KPIsPage.tsx            → Dashboard con tarjetas y gráficos
│
├── services/           ← Comunicación con el Backend
│   ├── api.ts                  → Instancia de Axios (base URL + interceptores JWT)
│   ├── authService.ts          → Login, logout, gestión de sesión
│   ├── customerService.ts      → CRUD de clientes via API
│   └── paymentService.ts       → CRUD de pagos via API
│
├── context/
│   └── AuthContext.tsx          → Estado global de autenticación (React Context)
│
├── hooks/
│   └── useInactivityTimer.ts    → Cierre automático de sesión por inactividad
│
├── types/
│   └── index.ts                 → Interfaces TypeScript del sistema
│
├── App.tsx                      → Rutas y protección de rutas
└── main.tsx                     → Punto de entrada de React
```

### 2.2 ¿Cómo se comunica el Frontend con el Backend?

Todo pasa por el archivo `api.ts`, que configura **Axios** (un cliente HTTP):

```typescript
// api.ts — El "mensajero" entre Frontend y Backend
const api = axios.create({
  baseURL: 'http://localhost:5263/api',  // ← Dirección del Backend
  headers: { 'Content-Type': 'application/json' },
});
```

#### Interceptor de Request (Envío automático del JWT):
Cada vez que el frontend envía una petición al backend, este interceptor **inyecta automáticamente** el token JWT en el header `Authorization`:

```
GET /api/customers HTTP/1.1
Host: localhost:5263
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...    ← El interceptor añade esto
Content-Type: application/json
```

#### Interceptor de Response (Detección de sesión expirada):
Si el backend responde con `401 Unauthorized` (token expirado o inválido), el interceptor:
1. Borra el token del `localStorage`.
2. Redirige al usuario a `/login` automáticamente.

### 2.3 Rutas Protegidas (ProtectedRoute)

En `App.tsx`, cada ruta importante está envuelta en `<ProtectedRoute>`:

```
/login      → Pública (no necesita autenticación)
/dashboard  → Protegida → requiere token JWT válido
/customers  → Protegida → requiere token JWT válido
/payments   → Protegida → requiere token JWT válido
```

Si un usuario intenta acceder a `/customers` sin haber hecho login, `ProtectedRoute` lo redirige automáticamente a `/login`.

### 2.4 Estado Global con AuthContext

`AuthContext` es el **cerebro de la sesión** en el frontend. Mantiene en memoria:
- `isAuthenticated`: ¿El usuario está logueado?
- `currentUser`: Nombre del usuario actual.
- `currentRole`: Rol del usuario (Admin, User).
- `login()`: Guarda token/username/rol en `localStorage` y actualiza el estado.
- `logout()`: Limpia `localStorage` y resetea el estado.

Además, integra un **timer de inactividad** (5 minutos): si el usuario no interactúa con la app, se cierra la sesión automáticamente.

---

## 3. El Backend (.NET 8 Web API)

### 3.1 Arquitectura en Capas (N-Tier)

El backend sigue una separación estricta de responsabilidades:

```
PETICIÓN HTTP
     │
     ▼
┌─────────────────────────────────────────────────┐
│  CAPA 1: CONTROLLERS (Capa de Presentación)     │
│  Responsabilidad: Recibir HTTP, delegar, y      │
│  devolver el HTTP Response correcto.             │
│  ⚠️ NUNCA contiene lógica de negocio.           │
│  ⚠️ NUNCA accede al Repository directamente.    │
└──────────────────────┬──────────────────────────┘
                       │ Llama al Service con DTOs
                       ▼
┌─────────────────────────────────────────────────┐
│  CAPA 2: SERVICES (Capa de Negocio)             │
│  Responsabilidad: Validar reglas de negocio      │
│  y mapear entre DTOs ↔ Entidades.               │
│  Ejemplo: "No se puede eliminar un cliente      │
│  si tiene pagos pendientes."                    │
└──────────────────────┬──────────────────────────┘
                       │ Llama al Repository con Entidades
                       ▼
┌─────────────────────────────────────────────────┐
│  CAPA 3: REPOSITORIES (Capa de Datos)           │
│  Responsabilidad: Ejecutar Stored Procedures     │
│  contra MySQL usando EF Core y ADO.NET.         │
│  ⚠️ NUNCA contiene lógica de negocio.           │
└──────────────────────┬──────────────────────────┘
                       │ Ejecuta Stored Procedures
                       ▼
┌─────────────────────────────────────────────────┐
│  MySQL (Stored Procedures)                      │
└─────────────────────────────────────────────────┘
```

### 3.2 ¿Qué son los DTOs y por qué existen?

**DTO (Data Transfer Object)** es un objeto que transporta datos entre capas sin exponer la estructura interna de la base de datos.

| Tipo | ¿Quién lo usa? | ¿Para qué? |
|---|---|---|
| `CustomerRequestDto` | Frontend → Controller | Lo que el usuario ENVÍA (nombre, email, etc.) |
| `CustomerResponseDto` | Controller → Frontend | Lo que el usuario RECIBE (id, nombre, fechaCreacion, etc.) |
| `Customer` (Entity) | Service ↔ Repository | Representación de la tabla en la BD. NUNCA sale del backend. |

**¿Por qué no enviar la Entity directamente al frontend?**
- **Seguridad**: La entidad User tiene `PasswordHash`. Si la envías al frontend, expones el hash.
- **Desacoplamiento**: Si cambias una columna en la BD, solo ajustas el mapeo en el Service. El frontend no se entera.
- **Control**: Con DTOs decides exactamente qué campos mostrar y qué campos ocultar.

### 3.3 Inyección de Dependencias (Program.cs)

En `Program.cs`, registramos todas las piezas del sistema:

```csharp
// Repositorios (Scoped = 1 instancia por petición HTTP)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Servicios
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Seguridad (Singleton = 1 instancia para toda la app)
builder.Services.AddSingleton<JwtHelper>();
```

**¿Cómo funciona?** Cuando el Controller necesita un `ICustomerService`, .NET busca en su registro y dice: "Ah, para ICustomerService debo crear un CustomerService". Y como CustomerService pide un `ICustomerRepository`, .NET crea un CustomerRepository. Todo automático.

### 3.4 Pipeline de Middlewares (Orden CRÍTICO)

Cada petición HTTP pasa por estos middlewares **en este orden exacto**:

```
Petición entrante
     │
     ▼
 SecurityHeadersMiddleware    → Agrega headers OWASP de seguridad
     │
     ▼
 UseCors("PermitirFrontend")  → ¿El origen (localhost:5173) está permitido?
     │
     ▼
 UseAuthentication            → ¿El token JWT del header es válido?
     │
     ▼
 UseAuthorization             → ¿El usuario tiene permiso para este endpoint?
     │
     ▼
 MapControllers               → Ejecuta el Controller correspondiente
```

> ⚠️ Si inviertes `UseAuthentication` y `UseAuthorization`, los atributos `[Authorize]` en los Controllers dejan de funcionar.

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

- **Users**: Tabla de autenticación. Almacena credenciales hasheadas.
- **Customers**: Clientes del sistema. Usa **Soft Delete** (campo `Activo`).
- **Payments**: Pagos de clientes. Usa **Hard Delete** (se borra físicamente).
- **Relación**: Un Customer puede tener muchos Payments (`1:N`), con `ON DELETE RESTRICT` para proteger la integridad referencial.

### 4.2 ¿Por qué Stored Procedures y no consultas directas?

| Aspecto | Consultas directas | Stored Procedures ✓ |
|---|---|---|
| **Seguridad** | Riesgo de SQL Injection | Parámetros tipados, sin riesgo |
| **Rendimiento** | Se compila en cada ejecución | Se compila una vez y se reutiliza |
| **Mantenimiento** | SQL disperso en el código C# | SQL centralizado en la BD |
| **Control DBA** | Desarrolladores escriben SQL libre | El DBA controla exactamente qué se ejecuta |

### 4.3 Técnicas de ejecución en los Repositories

Los Repositories usan dos técnicas para ejecutar los SPs:

**1. `FromSqlRaw` (Lecturas / SELECT)**:
```csharp
// Cuando el SP retorna filas que coinciden con la Entity
var customers = await _context.Customers
    .FromSqlRaw("CALL sp_Customer_GetAll")
    .AsNoTracking()
    .ToListAsync();
```
EF Core mapea automáticamente cada columna del resultado a una propiedad de la Entity.

**2. ADO.NET directo (Escrituras / INSERT, UPDATE, DELETE)**:
```csharp
// Cuando el SP retorna un valor escalar (LAST_INSERT_ID, ROW_COUNT)
var connection = _context.Database.GetDbConnection();
using var command = connection.CreateCommand();
command.CommandText = "sp_Customer_Create";
command.CommandType = CommandType.StoredProcedure;
command.Parameters.Add(new MySqlParameter("@p_Nombre", customer.Nombre));
var resultado = await command.ExecuteScalarAsync();
```
Se usa cuando el SP no retorna filas completas, sino valores individuales.

---

## 5. Flujo Completo: Login y Autenticación JWT

Este es el flujo más importante del sistema. Cada paso está numerado para que lo sigas como una línea de tiempo:

### Paso 1 — El usuario ingresa sus credenciales
El usuario escribe `admin` y `Admin123!` en `LoginPage.tsx` y hace click en "Iniciar Sesión".

### Paso 2 — React envía la petición HTTP
```
POST http://localhost:5263/api/auth/login
Content-Type: application/json

{ "username": "admin", "password": "Admin123!" }
```
> Nota: Esta petición NO lleva token JWT porque el endpoint `/login` es público (no tiene `[Authorize]`).

### Paso 3 — El Backend recibe la petición
`AuthController.Login()` recibe el `LoginRequestDto` y delega a `AuthService.LoginAsync()`.

### Paso 4 — AuthService busca al usuario en la BD
Llama a `UserRepository.GetByUsernameAsync("admin")`, que ejecuta:
```sql
CALL sp_User_GetByUsername('admin');
```
MySQL retorna la fila del usuario con su `PasswordHash` (algo como `$2a$11$K3g4gJ0Z...`).

### Paso 5 — AuthService verifica la contraseña con BCrypt
```csharp
bool passwordValido = BCrypt.Net.BCrypt.Verify("Admin123!", user.PasswordHash);
```
BCrypt internamente:
1. Extrae el **salt** del hash almacenado.
2. Re-hashea `"Admin123!"` con ese mismo salt.
3. Compara los dos hashes byte a byte.

### Paso 6 — JwtHelper genera el token
Si la contraseña es válida, `JwtHelper.GenerateToken()` crea un JWT con esta estructura:

```
HEADER:    { "alg": "HS256", "typ": "JWT" }
PAYLOAD:   { "nameid": "1", "unique_name": "admin", "role": "Admin", "exp": 1723334400 }
SIGNATURE: HMACSHA256(header + "." + payload, "ClaveSuperSecretaParaJWT...")
```

### Paso 7 — El Backend responde al Frontend
```json
HTTP 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "username": "admin",
  "rol": "Admin",
  "expiracion": "2026-08-10T00:36:00Z"
}
```

### Paso 8 — React guarda la sesión
`AuthContext.login()` guarda en `localStorage`:
```javascript
localStorage.setItem("token", "eyJhbGciOiJIUzI1NiIs...");
localStorage.setItem("username", "admin");
localStorage.setItem("rol", "Admin");
```

### Paso 9 — Todas las peticiones futuras llevan el token
A partir de ahora, el interceptor de Axios agrega automáticamente:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Paso 10 — El Backend valida el token en cada petición
El middleware `UseAuthentication` intercepta cada petición y:
1. Lee el header `Authorization`.
2. Decodifica el JWT.
3. Verifica la firma con la `SecretKey`.
4. Verifica que no haya expirado.
5. Si todo es válido → la petición continúa al Controller.
6. Si es inválido → responde `401 Unauthorized` automáticamente.

---

## 6. Flujo Completo: CRUD de Customer

### 6.1 Listar Clientes (GET)

```
React (CustomersPage)
  → customerService.getAll()
    → Axios: GET /api/customers + Bearer JWT
      → JWT Middleware valida token ✓
        → CustomerController.GetAll()
          → CustomerService.GetAllAsync()
            → CustomerRepository.GetAllAsync()
              → MySQL: CALL sp_Customer_GetAll()
              ← Lista de Customer entities
            ← Mapea Entity → CustomerResponseDto
          ← IEnumerable<CustomerResponseDto>
        ← HTTP 200 OK + JSON array
      ← response.data
    ← Customer[]
  → Renderiza la tabla DataTable
```

### 6.2 Crear Cliente (POST)

```
React: usuario llena formulario en modal
  → customerService.create({ nombre, email, telefono, direccion })
    → Axios: POST /api/customers + body JSON + Bearer JWT
      → CustomerController.Create(CustomerRequestDto)
        → CustomerService.CreateAsync(dto)
          → Mapea RequestDto → Entity Customer
          → CustomerRepository.CreateAsync(entity)
            → MySQL: CALL sp_Customer_Create(p_Nombre, p_Email, ...)
            ← LAST_INSERT_ID() = 21
          → CustomerRepository.GetByIdAsync(21)  ← Para obtener FechaCreacion
            → MySQL: CALL sp_Customer_GetById(21)
            ← Customer completo
          ← Mapea Entity → CustomerResponseDto
        ← HTTP 201 Created + { id: 21, nombre: "...", fechaCreacion: "..." }
      ← response.data
    ← Toast: "Cliente creado correctamente"
  → Recarga la tabla
```

### 6.3 Eliminar Cliente (Soft Delete)

La eliminación tiene una **regla de negocio** especial:

```
React: usuario confirma eliminación
  → customerService.delete(5)
    → CustomerController.Delete(5)
      → CustomerService.DeleteAsync(5)
        → ¿Existe el cliente? → GetByIdAsync(5) → Sí ✓
        → ¿Tiene pagos Pendientes? → GetAllAsync(customerId: 5)
          │
          ├─ SÍ tiene pagos pendientes:
          │   → throw InvalidOperationException("No se puede eliminar...")
          │   → Controller captura → HTTP 409 Conflict
          │   → Frontend muestra toast de error
          │
          └─ NO tiene pagos pendientes:
              → CustomerRepository.DeleteAsync(5)
                → MySQL: CALL sp_Customer_Delete(5)
                  → UPDATE Customers SET Activo = FALSE WHERE Id = 5
                  (el registro NO se borra, solo se oculta)
              → HTTP 204 No Content
              → Frontend muestra toast de éxito
```

---

## 7. Flujo Completo: CRUD de Payment

### 7.1 Crear Pago (con validación cruzada)

Antes de crear un pago, el sistema verifica que el cliente exista:

```
React: usuario selecciona cliente, ingresa monto y método
  → paymentService.create({ customerId: 3, monto: 1500.50, metodoPago: "Transferencia" })
    → PaymentController.Create(PaymentRequestDto)
      → PaymentService.CreateAsync(dto)
        → Validación cruzada: ¿Existe el cliente 3?
          → CustomerRepository.GetByIdAsync(3)
          │
          ├─ Cliente NO existe:
          │   → throw ArgumentException("El cliente con Id 3 no existe")
          │   → Controller captura → HTTP 400 Bad Request
          │
          └─ Cliente SÍ existe:
              → Mapea DTO → Entity Payment
              → PaymentRepository.CreateAsync(payment)
                → MySQL: CALL sp_Payment_Create(3, 1500.50, 'Transferencia')
                ← LAST_INSERT_ID() = 21
              → PaymentRepository.GetByIdAsync(21)
                → MySQL: CALL sp_Payment_GetById(21)
                  (JOIN con Customers para obtener CustomerNombre)
                ← Payment con CustomerNombre = "Marketing Creativo SA"
              ← PaymentResponseDto con toda la info
            ← HTTP 201 Created
```

### 7.2 Cambiar Estado (PATCH)

El botón "Completar" en la tabla de pagos ejecuta:

```
React: click en ícono ✓ de un pago Pendiente
  → paymentService.updateStatus(7, "Completado")
    → Axios: PATCH /api/payments/7/status
      Body: { "estado": "Completado" }
    → PaymentController.UpdateStatus(7, UpdatePaymentStatusDto)
      → PaymentService.UpdateStatusAsync(7, "Completado")
        → Valida que "Completado" sea un estado permitido
        → PaymentRepository.UpdateStatusAsync(7, "Completado")
          → MySQL: CALL sp_Payment_UpdateStatus(7, 'Completado')
            → UPDATE Payments SET Estado = 'Completado' WHERE Id = 7
          ← ROW_COUNT() = 1
        ← true
      ← HTTP 204 No Content
    ← Frontend recarga tabla (badge cambia de amarillo a verde)
```

### 7.3 Eliminar Pago (Hard Delete)

A diferencia de Customers (Soft Delete), los pagos se borran **físicamente**:

```
MySQL: DELETE FROM Payments WHERE Id = 7  ← El registro desaparece para siempre
```

---

## 8. Seguridad del Sistema

### 8.1 Capas de Seguridad Implementadas

| Capa | Mecanismo | ¿Qué protege? |
|---|---|---|
| **Autenticación** | JWT (JSON Web Tokens) | Verifica la identidad del usuario |
| **Contraseñas** | BCrypt (hashing con salt) | Protege contraseñas almacenadas |
| **CORS** | Whitelist de orígenes | Solo `localhost:5173` puede hacer peticiones |
| **Headers OWASP** | SecurityHeadersMiddleware | Previene XSS, clickjacking, sniffing |
| **Validación** | FluentValidation + Data Annotations | Previene datos malformados |
| **Inactividad** | useInactivityTimer (5 min) | Cierra sesión automáticamente |
| **SQL Injection** | Stored Procedures con parámetros | Imposible inyectar SQL |
| **Integridad** | FK con ON DELETE RESTRICT | No se pueden borrar clientes con pagos |

### 8.2 Headers de Seguridad OWASP

El middleware `SecurityHeadersMiddleware` agrega estos headers a **cada respuesta**:

```
X-Content-Type-Options: nosniff           → Evita que el navegador "adivine" tipos MIME
X-Frame-Options: DENY                     → Evita que tu app se embeba en un iframe (anti-clickjacking)
X-XSS-Protection: 1; mode=block           → Activa el filtro anti-XSS del navegador
Strict-Transport-Security: max-age=...     → Fuerza HTTPS en producción
Content-Security-Policy: default-src 'self' → Solo permite cargar recursos del mismo dominio
```

### 8.3 ¿Por qué BCrypt y no SHA256?

| Aspecto | SHA256 | BCrypt ✓ |
|---|---|---|
| Velocidad | Extremadamente rápido (malo para contraseñas) | Lento intencionalmente (10+ ms por hash) |
| Salt | No incluye salt automático | Genera salt aleatorio automáticamente |
| Ataques brute-force | Vulnerable (billones de intentos/segundo) | Resistente (factor de trabajo configurable) |
| Rainbow tables | Vulnerable sin salt | Inmune (cada hash tiene salt único) |

---

## 9. Docker y Despliegue

### 9.1 Orquestación con Docker Compose

`docker-compose.yml` define 3 contenedores conectados en una **red interna de Docker**:

```
┌──────────────────── Red Interna de Docker ────────────────────┐
│                                                                │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐    │
│  │     db       │    │    api      │    │   frontend      │    │
│  │  MySQL 8.0   │    │  .NET 8     │    │  Nginx + React  │    │
│  │  Puerto 3306 │◄───│  Puerto 8080│◄───│  Puerto 80      │    │
│  └──────┬───────┘    └──────┬──────┘    └──────┬──────────┘    │
│         │                   │                   │               │
└─────────┼───────────────────┼───────────────────┼───────────────┘
          │                   │                   │
     Host:3307           Host:5263           Host:5173
```

- **db** → api: El backend se conecta usando `Server=db` (nombre del servicio Docker, no localhost).
- **depends_on**: Docker enciende en orden: db → api → frontend.
- **Volumen `db_data`**: Los datos de MySQL persisten aunque apagues los contenedores.
- **init.sql**: Se ejecuta automáticamente la primera vez que el contenedor `db` se crea.

### 9.2 Levantar todo el proyecto

```bash
git clone https://github.com/Jhelol113/CustomerPayments.git
cd CustomerPayments
docker-compose up --build -d
```

Eso es **todo**. No necesitas instalar Node.js, .NET, ni MySQL.

---

## 10. Glosario de Conceptos Clave

| Concepto | Definición |
|---|---|
| **N-Tier** | Arquitectura que separa la aplicación en capas (Presentación, Negocio, Datos) |
| **Atomic Design** | Metodología que organiza componentes UI en átomos, moléculas, organismos y páginas |
| **DTO** | Objeto que transporta datos entre capas sin exponer la estructura interna |
| **Entity** | Clase C# que representa una tabla de la base de datos |
| **Repository** | Clase que encapsula el acceso a datos (ejecuta los SPs) |
| **Service** | Clase que contiene la lógica de negocio y mapea DTOs ↔ Entities |
| **Controller** | Clase que recibe peticiones HTTP y delega al Service |
| **JWT** | Token firmado digitalmente que identifica al usuario sin consultar la BD |
| **BCrypt** | Algoritmo de hashing diseñado para contraseñas (lento intencionalmente) |
| **Soft Delete** | Marcar un registro como inactivo en vez de borrarlo físicamente |
| **Hard Delete** | Borrar un registro físicamente de la base de datos |
| **CORS** | Mecanismo del navegador que bloquea peticiones entre diferentes orígenes |
| **Stored Procedure** | Bloque de SQL precompilado almacenado en la base de datos |
| **Interceptor (Axios)** | Función que se ejecuta automáticamente antes/después de cada petición HTTP |
| **Middleware (.NET)** | Función que procesa cada petición HTTP antes de llegar al Controller |
| **Inyección de Dependencias** | Patrón donde .NET crea e inyecta automáticamente las dependencias de una clase |
| **FromSqlRaw** | Método de EF Core para ejecutar SQL crudo y mapear el resultado a una Entity |
| **DbContext** | Clase de EF Core que representa la sesión con la base de datos |
| **React Context** | Mecanismo de React para compartir estado global entre componentes |
| **FluentValidation** | Librería para definir reglas de validación complejas en C# |
