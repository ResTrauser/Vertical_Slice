# Vertical Slice Template (.NET 10 + DDD)

## Objetivo
Plantilla para APIs escalables basada en **Vertical Slice Architecture** con principios DDD y EF Core. Este proyecto soluciona el problema de quienes requieren registro de usuarios, planes con límites, suscripciones históricas, gestión de negocios y miembros con flujo de invitaciones.

---
## Stack principal
- **.NET 10 Minimal APIs**
- **Entity Framework Core 10** (Npgsql) con configuraciones por entidad
- **Vertical Slice**: cada feature vive en `Features/<Slice>` (DTOs, validators, mappings, servicios, endpoints)
- **DDD**: agregados en `Domain/<BoundedContext>` con Value Objects, eventos y excepciones
- **MediatR + FluentValidation + Mapster**
- **Autenticación JWT + Refresh Tokens**
- **Autorización por Policies (AdminOnly, BusinessOwner, BusinessAdminOrOwner, BusinessMember)**

---
## Arquitectura de carpetas
```
src/Api
 ├─ Domain/                 # Entidades, VO, eventos, excepciones DDD
 ├─ Data/                   # AppDbContext, configuraciones EF, seeds
 ├─ Shared/                 # Infra transversal (options, security, results, policies, behaviors)
 ├─ Features/
 │   ├─ Auth/               # registro/login/refresh/logout/me
 │   ├─ Plans/              # CRUD con protección de system plans
 │   ├─ Subscriptions/      # cambio de plan con modos Block/Enforce + histórico
 │   ├─ Businesses/         # crear/listar/miembros con límites por plan
 │   └─ Invites/            # invitaciones dev-mode (token por email)
 └─ Program.cs              # Minimal API + wiring de slices

tests/Api.Tests
 ├─ Support/TestDb.cs       # Helpers EF InMemory
 ├─ Plans/Subscription/...  # Suites unitarias por feature
```

---
## Decisiones clave
- **Vertical Slice real**: cada caso de uso expone su endpoint, DTOs, validaciones y servicios sin capas genéricas.
- **DDD práctico**: agregados (`User`, `Plan`, `Subscription`, `Business`, `BusinessInvite`) definen invariantes; EF se adapta al dominio.
- **Suscripciones con downgrade configurable**:
  - `Block`: si excede límites (negocios/miembros) no permite bajar de plan.
  - `Enforce`: desactiva los excedentes (mantiene los más antiguos y al Owner).
- **Autorización fina**: policies personalizadas para admin y roles dentro del negocio.
- **Tests unitarios** con EF Core InMemory cubriendo reglas de cada feature.

---
## Funcionalidad inicial
1. **Auth**
   - `POST /auth/register`, `/login`, `/refresh`, `/logout`, `GET /auth/me`
2. **Planes**
   - `GET /plans`, `GET /plans/{id}` (público)
   - CRUD admin (`POST/PUT/DELETE /plans`) respetando `IsSystem`
3. **Suscripciones**
   - `POST /subscriptions/change-plan` (autenticado)
   - `GET /subscriptions/me/active` y `/me/history`
4. **Negocios & Miembros**
   - `POST /businesses`, `GET /businesses/mine`, `GET /businesses/{id}` (miembros)
   - `POST/DELETE/PUT /businesses/{id}/members` solo Owner (rol)
5. **Invites**
   - `POST /invites` (Owner/Admin)
   - `POST /invites/{id}/revoke`
   - `POST /invites/accept` (token dev-mode log)

---
## Cómo ejecutar
1. **Prerequisitos**: .NET 10 SDK, PostgreSQL corriendo (actualiza `ConnectionStrings:Default` en `appsettings.json`).
2. **Migraciones**:
   ```bash
   dotnet ef database update -p src/Api -s src/Api
   ```
3. **API**:
   ```bash
   dotnet run --project src/Api
   ```
   El primer arranque genera planes seed y (si configuras `AdminSeed`) un admin inicial.

---
## Tests
- Proyecto `tests/Api.Tests`
- Ejecuta `dotnet test` (usa EF InMemory). Cubre Auth, Plans, Subscriptions, Businesses e Invites.

---
## Próximos pasos sugeridos
- Eliminar warnings fijando versiones (Bogus 35.5.0 y EFCore.Relational 10.0.1) para Api.Tests.
- Añadir endpoints de invitaciones con envío real (SMTP o provider) si se desea.
- Expandir tests a escenarios integrados/end-to-end si hace falta.

---
## Configuración importante
`appsettings.json` (valores por defecto de ejemplo):
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=verticalslice_db;Username=postgres;Password=postgres"
},
"Jwt": { "...": "..." },
"Subscription": { "DowngradeMode": "Block" },
"Invite": { "InviteExpiryHours": 72 },
"AdminSeed": { "Email": "admin@example.com", "Password": "ChangeMe123!" }
```
Cambiar secrets antes de producción.

---
## Referencias útiles
- Vertical Slice Architecture – Jimmy Bogard
- MediatR & Minimal APIs samples
- EF Core owned types + configuraciones por entidad
