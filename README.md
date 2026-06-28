# IPI Pro - Specimen Check-In

A healthcare SaaS scaffold for specimen check-in and discrepancy auditing, built with .NET 8, EF Core 8, MS SQL Server, and Vue 3 (Composition API, SFC, Vite).

## 1. Prerequisites
- **.NET 8.0 SDK** or later
- **MS SQL Server LocalDB** (installed with Visual Studio, or via SQL Server Express)
- **Node.js 18.0** or later
- **npm** (comes with Node.js)

---

## 2. Backend Setup
1. Open a terminal and navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```
3. Run the database migrations (this will create/update the database schema in LocalDB):
   ```bash
   dotnet ef database update --project IpiPro.Api
   ```
4. Start the backend API dev server (listens on port 5000):
   ```bash
   dotnet run --project IpiPro.Api
   ```

---

## 3. Frontend Setup
1. Open a terminal and navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install npm dependencies:
   ```bash
   npm install
   ```
3. Run the development server (runs on port 5173, proxying `/api` calls to port 5000):
   ```bash
   npm run dev
   ```
4. Open your browser and navigate to `http://localhost:5173`.

---

## 4. Running Tests
You can run the backend unit and isolation test suite using the dotnet test runner:
1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Run tests:
   ```bash
   dotnet test
   ```

---

## 5. Switching Tenants
This SaaS application uses a request-header-based multi-tenancy model. The active tenant is identified by the `X-Lab-Id` header.
- To simulate switching tenants, change the value of `VITE_LAB_ID` in `frontend/src/services/api.js` (or in the environment file if customized):
  - `1` = **Riverside Pathology** (Clinic: Riverside Clinic)
  - `2` = **Harbor Labs** (Clinic: Harbor Clinic)
- The database contains fully isolated sets of manifests and specimens for each lab. Changing this ID will display a completely separate interface context.

---

## 6. Stack Choices
- **Backend API**: ASP.NET Core Web API on .NET 8. Provides high performance, native dependency injection, and clean MVC/Controller routing.
- **ORM**: Entity Framework Core 8. Enables robust, type-safe query generation, migrations, and relationship mapping.
- **Database**: Microsoft SQL Server. Chosen for enterprise stability, relational integrity (crucial for audit logs), and direct compatibility with Azure SQL.
- **Frontend**: Vue 3 with Vite. Single File Components (SFC) and the Composition API provide responsive state management, clear reactivity boundaries, and high-performance developer experience.

---

## 7. Architecture Write-Up

### Azure Topology & Deployment
In a cloud environment, this application is deployed using the following Azure services:
1. **Frontend Hosting**: Azure Static Web Apps (SWA). Offers global CDN distribution, automatic SSL certificate provisioning, and cost-effective hosting for Single Page Apps (SPA).
2. **Backend API**: Azure App Service (Web App on Linux, .NET runtime). Scrapes scaling limits dynamically and integrates with Azure Key Vault for application secrets.
3. **Database**: Azure SQL Database (General Purpose or Hyperscale tier). Utilizing **Azure SQL Elastic Pools** to manage resources across multiple customer databases if a database-per-tenant pattern is used.
4. **Networking**: Virtual Network (VNet) integration. The API app service communicates with SQL Server securely via a Private Endpoint, ensuring database traffic never crosses the public internet.

### Session State
The API backend is entirely **stateless**. 
- No sticky sessions or server-side memory caches are used for session state.
- Tenant context is resolved per-request using the `X-Lab-Id` header (which, in production, would be extracted from a secure, cryptographically signed JWT token supplied by an identity provider like Entra ID or Auth0).
- This statelessness enables horizontal auto-scaling of backend instances behind an Azure Application Gateway.

### HIPAA-Aware Handling & PHI Compliance
To meet HIPAA compliance requirements for Protected Health Information (PHI):
1. **Encryption in Transit**: TLS 1.3 is enforced on all API endpoints in production.
2. **Encryption at Rest**: Azure SQL Database enforces **Transparent Data Encryption (TDE)**. For production, column-level encryption is recommended (using EF Core's value converters for high-sensitivity fields like patient names, integrated with Azure Key Vault via Always Encrypted).
3. **Audit Trails (Implemented)**: Every write action (receiving, flagging, resolving, or adding specimens) creates an immutable row in the `CheckInEvents` audit table documenting the Action, Timestamp, Specimen, and User ID. Deletion is blocked via EF/database constraints.
4. **Activity Logs**: Azure Monitor and Log Analytics ingest audit-level application logs. PII/PHI is explicitly filtered out of standard debug and error logs.

### Tenant Isolation Strategy
Tenant isolation in this scaffold is enforced at the software layer via service query scopes:
- **Application Filtering (Implemented)**: The `ManifestService` resolves `ITenantService` and filters by `LabId == tenant.LabId` for all queries and mutations.
- **EF Core Global Query Filters**: For production, we would register a Global Query Filter in `AppDbContext.OnModelCreating` to automatically restrict all queries globally, preventing developer omission.
- **Row-Level Security (RLS)**: At the database tier, SQL Server Row-Level Security can be configured with security policies that match the request context session variable (`SESSION_CONTEXT`), ensuring database-level queries cannot escape tenant scope.

---

## 8. If You Had Two More Days
If given two more days to build or harden this application further, we would prioritize the following:

1. **Authentication & JWT Role-Based Access Control (RBAC)**
   - *Why*: Currently, the tenant ID is passed via a simulated `X-Lab-Id` request header and the user ID is hardcoded as `current-user`. We would integrate Microsoft Entra ID or Auth0 to authenticate users, issuing cryptographically signed JWTs. The tenant ID and user roles (e.g., `LabTech`, `LabManager`, `ClinicalAdmin`) would be securely extracted from claims, preventing spoofing.
   
2. **True Database-Level Tenant Isolation (RLS / EF Filters)**
   - *Why*: Relying on developer-written `.Where(m => m.LabId == _tenant.LabId)` is error-prone as the project grows. Implementing EF Core Global Query Filters and SQL Server Row-Level Security (RLS) ensures that even if a developer forgets a filter, the database or context boundary rejects cross-tenant data leakage.

3. **Optimistic Concurrency & Audit History Tracking**
   - *Why*: Multiple lab technicians might try to receive or resolve discrepancies on the same manifest simultaneously. We would configure EF Core `[ConcurrencyCheck]` on specimens and manifests. We would also implement EF Core Shadow Properties or a library like `Audit.NET` to automatically track pre/post-change history for compliance.

4. **Advanced Scan Workflow (Full Scan Barcode Handling)**
   - *Why*: The UI currently defaults to "Fast Count" verification. We would fully implement the "Full Scan" mode, integrating a real browser barcode-reading library (e.g. `html5-qrcode`) to allow technicians to scan barcodes directly using device cameras, automatically matching scanned specimens in real-time.

