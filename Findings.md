# Findings: Specimen Check-In Assignment Review

## Scope

This review compares the repository implementation against the requirements in `Full-Stack-Developer-Assignment.docx`.

Source of truth used for this review:

- `Full-Stack-Developer-Assignment.docx`
- Backend: `backend/IpiPro.Api`
- Frontend: `frontend/src`
- Tests: `backend/IpiPro.Tests`
- README: `README.md`

Verification performed:

- Backend tests: `dotnet test --no-restore` -> passed
- Frontend production build: `npm run build` -> passed

## Overall Assessment

The project implements most of the requested vertical slice:

- correct stack choices
- relational persistence with EF Core and a committed migration
- server-side tenant scoping
- seeded multi-tenant data
- working API wiring from Vue to ASP.NET Core
- a UI that is broadly faithful to the provided design reference
- a small but relevant backend test suite

The main functional miss is the manifest reconciliation rule.

The assignment says the manifest should only be closed once everything is reconciled. The current implementation allows closing a manifest while discrepancies remain open by moving it to `ClosedWithDiscrepancy`. There is also no implemented resolution flow for discrepancies, which means the system cannot actually complete the reconciliation lifecycle required by the brief.

## Summary Verdict

### Matches well

- Backend stack and API shape
- EF Core code-first model with migration committed
- Seeded multi-tenant sample data
- Server-enforced tenant isolation on reads and writes
- Duplicate receive handling is idempotent
- Vue 3 front-end wired to the API
- Loading, empty, and error states are present
- Automated tests exist and are relevant
- README contains run instructions, stack choices, and an Azure/security write-up

### Partially matches

- Front-end fidelity to the reference screenshot
- Readiness/counting behavior in the sidebar
- Audit/write-up coverage in README

### Does not match

- Close manifest only when reconciled
- Resolve discrepancy workflow

## Requirement-by-Requirement Review

## 1. Core Product Flow

Requirement from brief:

> A lab technician should be able to open a manifest, see its expected specimens, mark bottles as received, flag missing or off-manifest bottles as discrepancies, and close the manifest once everything is reconciled.

Status: **Partially met**

What exists:

- Manifest list for the current lab
- Manifest detail with specimens
- Receive specimen action
- Flag specimen as missing
- Add off-manifest specimen
- Close manifest action

Why only partial:

- The implementation does not enforce "once everything is reconciled".
- Open discrepancies do not block closing.
- There is no discrepancy resolution action.

Relevant code:

- `backend/IpiPro.Api/Services/ManifestService.cs`
- `frontend/src/components/ManifestDetail.vue`
- `frontend/src/components/SpecimenTable.vue`

Necessary fix:

1. Introduce a reconciliation rule in the backend that rejects close when any discrepancy is still `Open`.
2. Add an explicit resolution mechanism for discrepancies.
3. Reflect discrepancy resolution state in the UI and counts.

## 2. Backend Requirements

### 2.1 ASP.NET Core Web API, RESTful JSON endpoints

Status: **Met**

Evidence:

- API project is ASP.NET Core on .NET 8.
- Controller endpoints are REST-style and return JSON.

Relevant code:

- `backend/IpiPro.Api/Program.cs`
- `backend/IpiPro.Api/Controllers/ManifestsController.cs`

Necessary fix:

- None required for this requirement.

### 2.2 EF Core code-first model with at least one committed migration

Status: **Met**

Evidence:

- EF Core is configured with SQL Server.
- Migration files are committed.

Relevant code:

- `backend/IpiPro.Api/Data/AppDbContext.cs`
- `backend/IpiPro.Api/Migrations/20260626172321_InitialCreate.cs`
- `backend/IpiPro.Api/Migrations/AppDbContextModelSnapshot.cs`

Necessary fix:

- None required for the assignment requirement.

### 2.3 Server-enforced tenant scoping on every read and write

Status: **Met**

Evidence:

- Tenant is read from `X-Lab-Id` in the backend.
- Service methods filter by `LabId` for listing, reading, receiving, flagging, adding, and closing.
- Tests cover cross-tenant reads and list isolation.

Relevant code:

- `backend/IpiPro.Api/Services/TenantService.cs`
- `backend/IpiPro.Api/Services/ManifestService.cs`
- `backend/IpiPro.Tests/TenantIsolationTests.cs`

Observations:

- Isolation is enforced in service-layer queries, which is valid for this assignment.
- It is not implemented as EF global query filters or database row-level security. The README correctly frames those as future hardening, not current behavior.

Necessary fix:

1. Keep tenant filtering centralized.
2. For hardening, move common tenant filtering into reusable query helpers or EF global query filters to reduce omission risk as the codebase grows.

### 2.4 Required endpoints

Required:

- list manifests for current lab
- get manifest with specimens
- mark specimen received
- flag specimen missing
- record off-manifest specimen and raise discrepancy
- close manifest only when reconciled

Status: **Partially met**

Implemented endpoints:

- `GET /api/manifests`
- `GET /api/manifests/{id}`
- `POST /api/manifests/{id}/specimens/{sid}/receive`
- `POST /api/manifests/{id}/specimens/{sid}/flag`
- `POST /api/manifests/{id}/specimens`
- `POST /api/manifests/{id}/close`

Relevant code:

- `backend/IpiPro.Api/Controllers/ManifestsController.cs`

Gap:

- `close` exists, but behavior is wrong relative to the brief.

Necessary fix:

1. Change close behavior so open discrepancies also block closure.
2. Add discrepancy-resolution endpoints, for example:
   - `POST /api/manifests/{id}/discrepancies/{did}/resolve`
   - or a more explicit domain action such as `mark-arrived`, `acknowledge`, or `resolve-off-manifest`

### 2.5 Idempotent duplicate receive handling and structured errors

Status: **Met**

Evidence:

- Calling receive on an already received specimen returns the existing state and does not create duplicate check-in events.
- Controller returns structured JSON errors for not found, bad request, unauthorized, and internal errors.

Relevant code:

- `backend/IpiPro.Api/Services/ManifestService.cs`
- `backend/IpiPro.Api/Controllers/ManifestsController.cs`
- `backend/IpiPro.Tests/ManifestServiceTests.cs`

Necessary fix:

- None required for this requirement.

### 2.6 Seed data with at least two labs and realistic states

Status: **Met**

Evidence:

- Two labs are seeded.
- Multiple manifests per lab are seeded.
- Seed data covers `Pending`, `Received`, `Flagged`, and `Added`.
- Discrepancies and check-in events are also seeded.

Relevant code:

- `backend/IpiPro.Api/Data/SeedData.cs`

Necessary fix:

- None required for this requirement.

### 2.7 Automated tests for reconciliation logic and tenant isolation

Status: **Met, but narrow**

Evidence:

- 6 backend tests passed.
- Tests cover:
  - duplicate receive idempotency
  - close rejected with pending specimens
  - missing discrepancy creation
  - off-manifest specimen creation
  - cross-tenant read isolation
  - list isolation

Relevant code:

- `backend/IpiPro.Tests/ManifestServiceTests.cs`
- `backend/IpiPro.Tests/TenantIsolationTests.cs`

What is missing:

- No tests for close rejection when discrepancies remain open
- No tests for cross-tenant write attempts across all mutation paths
- No tests for invalid tenant header handling

Necessary fix:

1. Add tests for "cannot close with open discrepancies".
2. Add tests for cross-tenant receive/flag/add/close attempts.
3. Add tests for missing/invalid `X-Lab-Id`.

## 3. Front-End Requirements

### 3.1 Vue 3 implementation

Status: **Met**

Evidence:

- Vue 3 with SFCs and Composition API is used.

Relevant code:

- `frontend/src/App.vue`
- `frontend/src/components/*.vue`

Necessary fix:

- None required for this requirement.

### 3.2 Reproduce the Check-In screen layout and visual language

Status: **Mostly met**

What matches well:

- Left worklist / right detail split
- Top navigation/header structure
- status pills
- metric cards
- specimen table
- verify/close action area
- clinical dashboard styling

Evidence:

- The extracted screenshot from the docx and the implemented layout are visibly aligned in overall structure.

Relevant code:

- `frontend/src/App.vue`
- `frontend/src/components/ManifestList.vue`
- `frontend/src/components/ManifestDetail.vue`
- `frontend/src/components/SpecimenTable.vue`
- `frontend/src/index.css`

Gaps:

1. Several values are placeholders or hardcoded rather than driven by API state:
   - received by
   - received at
   - location/bay/user strings
2. Some text rendering contains character encoding artifacts in source text such as broken dash and symbol characters.
3. The top-level "Flag discrepancy" button is decorative and only opens an alert, not a real action flow.

Necessary fix:

1. Replace placeholder received-by/time values with real event-derived data from the backend.
2. Remove encoding artifacts and normalize UTF-8 file content.
3. Make the main discrepancy action open a real resolution or create-discrepancy workflow instead of `alert()`.

### 3.3 UI wired to API with live updates

Status: **Mostly met**

What works:

- Selecting a manifest loads detail data
- Receiving a specimen calls the backend
- Flagging a specimen calls the backend
- Adding an off-manifest specimen calls the backend
- Closing a manifest calls the backend
- Detail data refreshes after mutations

Relevant code:

- `frontend/src/services/api.js`
- `frontend/src/components/ManifestDetail.vue`
- `frontend/src/components/SpecimenTable.vue`

Gap:

- Sidebar count/readiness logic is inconsistent with off-manifest additions.

Specific issue:

- `expectedCount` is based on `specimens.length`.
- auto-synced `countedBottles` excludes `addedCount`.
- manifests with added off-manifest specimens can show inconsistent readiness messaging.

Relevant code:

- `frontend/src/App.vue`

Necessary fix:

1. Define what "expected" means in the UI:
   - original manifest count only, or
   - total active specimens including off-manifest additions
2. Use one consistent rule across:
   - metric cards
   - sidebar expected count
   - ready-to-close message
   - manifest list ratios
3. Prefer driving these values from backend-computed fields rather than duplicating count logic in multiple Vue components.

### 3.4 Empty state, loading state, and error state

Status: **Met**

Evidence:

- Empty manifest selection state exists.
- Loading state exists for lists and detail.
- Error banners/messages exist for fetch and mutation failures.

Relevant code:

- `frontend/src/App.vue`
- `frontend/src/components/ManifestDetail.vue`
- `frontend/src/components/SpecimenTable.vue`

Necessary fix:

- None required for this requirement.

### 3.5 Other tabs are context only

Status: **Met**

Observation:

- The other tabs are present as static context and are not implemented as full workflows, which is acceptable per the brief.

Necessary fix:

- None required.

## 4. Data Model Review

Status: **Met**

The data model aligns well with the suggested model in the assignment:

- `Lab`
- `Clinic`
- `Manifest`
- `Specimen`
- `CheckInEvent`
- `Discrepancy`

Relevant code:

- `backend/IpiPro.Api/Models/Lab.cs`
- `backend/IpiPro.Api/Models/Clinic.cs`
- `backend/IpiPro.Api/Models/Manifest.cs`
- `backend/IpiPro.Api/Models/Specimen.cs`
- `backend/IpiPro.Api/Models/CheckInEvent.cs`
- `backend/IpiPro.Api/Models/Discrepancy.cs`

Observation:

- Tenant-owned rows carry `LabId`, which is correct.
- The model includes the lifecycle concepts the assignment expects.

Necessary fix:

1. Add richer discrepancy lifecycle support if reconciliation is to be completed properly.
2. Consider storing event metadata needed by the UI rather than hardcoding it client-side.

## 5. README and Architecture Write-Up

### 5.1 Local run instructions

Status: **Met**

Evidence:

- README includes backend setup, frontend setup, and test instructions.
- `.env.example` is present.

Relevant files:

- `README.md`
- `.env.example`

### 5.2 Stack and database choices

Status: **Met**

Evidence:

- README explicitly states .NET 8, EF Core 8, SQL Server, and Vue 3.

### 5.3 Required Section 6 write-up

Status: **Mostly met**

Covered well:

- Azure topology
- session/state
- HIPAA-aware handling
- tenant isolation reasoning

Relevant file:

- `README.md`

Gaps:

1. I could not find the required "If you had two more days: what would you build or harden next, and why?" section.
2. Some write-up statements describe protections or data captured that are not implemented in the code today:
   - IP address logging in `CheckInEvents`
   - deletion blocked at database level
   - column-level encryption/value converters
   - TLS rejection logic in the app

Necessary fix:

1. Add a short "With two more days" section to the README.
2. Separate current implementation from production hardening clearly:
   - "Implemented now"
   - "Recommended in production"
3. Do not state current behavior that the code does not actually perform.

## Severity-Ordered Findings

## High Severity

### 1. Manifest can be closed before reconciliation is complete

Why this matters:

- This is the main business rule in the assignment.
- It changes core workflow correctness.

Current behavior:

- Close is blocked only when there are pending specimens.
- If discrepancies remain open, the system still closes the manifest as `ClosedWithDiscrepancy`.

Relevant code:

- `backend/IpiPro.Api/Services/ManifestService.cs`

Necessary fix:

1. Reject close when any discrepancy has `Status == Open`.
2. Return a clear structured error such as:
   - `code: "ManifestNotReconciled"`
   - `error: "Cannot close manifest while discrepancies remain open."`
3. Update tests to lock this behavior.

### 2. No discrepancy resolution workflow exists

Why this matters:

- The brief says discrepancies must be resolved before closing.
- The system creates discrepancies but cannot resolve them.

Current behavior:

- Missing and off-manifest discrepancies are created.
- `Resolved` exists in the enum but is never used by API or UI.

Relevant code:

- `backend/IpiPro.Api/Models/Discrepancy.cs`
- `backend/IpiPro.Api/Services/ManifestService.cs`
- `backend/IpiPro.Api/Controllers/ManifestsController.cs`

Necessary fix:

1. Add backend actions to resolve discrepancies.
2. Add UI affordances to resolve them.
3. Refresh close eligibility from actual discrepancy state.

## Medium Severity

### 3. Sidebar counts and ready-to-close status are inconsistent

Why this matters:

- The UI is supposed to communicate live operational state clearly.
- Inconsistent counting erodes trust in the workflow.

Current behavior:

- Different parts of the UI use different counting rules.

Relevant code:

- `frontend/src/App.vue`
- `frontend/src/components/ManifestList.vue`
- `frontend/src/components/ManifestDetail.vue`

Necessary fix:

1. Define one domain rule for expected vs received vs discrepant counts.
2. Compute those counts in one place, preferably backend DTOs.
3. Use the same fields everywhere in the UI.

### 4. Some UI values are hardcoded instead of API-driven

Why this matters:

- It weakens fidelity and makes the UI look functional beyond what the data supports.

Examples:

- `Lab Tech 1`
- received timestamps
- workflow badge text
- top-level manifest context strings

Relevant code:

- `frontend/src/components/SpecimenTable.vue`
- `frontend/src/components/ManifestDetail.vue`
- `frontend/src/App.vue`

Necessary fix:

1. Expose event/user metadata from the API.
2. Bind the UI to those values.
3. Keep placeholders only where the brief explicitly allows stubbing.

## Low Severity

### 5. README overstates some current protections

Why this matters:

- The write-up should be precise about what is implemented versus what is proposed.

Examples:

- IP address in audit trail is described, but there is no IP field in `CheckInEvent`.
- encryption and database protections are discussed as if active.

Necessary fix:

1. Mark these as production recommendations, not implemented features.
2. Add an "Implemented today" subsection.

### 6. Missing "two more days" note in README

Why this matters:

- The brief explicitly requests it.

Necessary fix:

Add a short prioritized list such as:

1. discrepancy resolution workflow
2. close rule hardening and tests
3. real audit/event metadata
4. frontend tests
5. concurrency handling

## Recommended Fix Plan

## Phase 1: Correctness

1. Change close-manifest logic to reject open discrepancies.
2. Add discrepancy resolution endpoints and service methods.
3. Add tests for:
   - cannot close with open discrepancies
   - can close after all discrepancies are resolved
   - cross-tenant resolution/write attempts are blocked

## Phase 2: UI Alignment

1. Add discrepancy resolution UI.
2. Replace alert-only top action with a real workflow.
3. Unify counting rules across sidebar, metrics, manifest list, and readiness messaging.

## Phase 3: Data and Audit Improvements

1. Return event metadata needed by the UI.
2. Store received-by and received-at per specimen based on actual events.
3. Clean up any text encoding issues in Vue files.

## Phase 4: README Accuracy

1. Add the missing "two more days" section.
2. Separate present behavior from production recommendations.
3. Remove or qualify statements that are not implemented in code.

## Final Conclusion

This repository is a solid partial match to the assignment and demonstrates good progress on the intended stack and architecture. The biggest gap is not technical scaffolding but business-rule completeness: the current system can create discrepancies, but it cannot reconcile them, and it still allows closure. That is the one issue that materially prevents the implementation from fully satisfying the brief.

If the required fixes are applied, the project would align much more cleanly with the assignment's core workflow and scoring criteria.
