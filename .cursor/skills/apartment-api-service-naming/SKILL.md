---
name: apartment-api-service-naming
description: >-
  Names C# service methods and related controller flows for the Apartment API:
  apartment-scoped lists use List{Entity}ForApartmentAsync; tenant data uses
  ICurrentUser.IdApartment from the JWT, not query/body client-supplied
  apartment ids. Use when adding or refactoring IBankAccountService,
  IVendorService, IExpenseHeadService, IIncomeHeadService, similar services,
  or their controllers, or when the user asks for naming consistency in this
  codebase.
---

# Apartment API — service and controller naming

## List methods (apartment-scoped)

Use **entity-specific** names, not a generic `ListForApartmentAsync` or `GetByApartmentIdAsync`.

**Pattern:** `List{EntityPlural}ForApartmentAsync(int apartmentId, CancellationToken cancellationToken = default)`

- Returns `Task<IReadOnlyList<{Entity}Dto>>` (or `IReadOnlyCollection` if the project already uses that for a given service).
- Name the **plural** to match the domain noun used in the API (e.g. Vendors, BankAccounts, ExpenseHeads, IncomeHeads).

**Examples (reference implementations):**

| Service | Method |
|--------|--------|
| `IBankAccountService` | `ListBankAccountsForApartmentAsync` |
| `IVendorService` | `ListVendorsForApartmentAsync` |
| `IExpenseHeadService` | `ListExpenseHeadsForApartmentAsync` |
| `IIncomeHeadService` | `ListIncomeHeadsForApartmentAsync` |

**Do not** use vague names like `GetByApartmentIdAsync`, `ListForApartmentAsync`, or `GetListAsync` for these apartment-scoped list operations when the return type is a specific entity collection.

## Single-entity read / write (apartment in token)

- **`GetByIdAsync`**: keep signature scoped to the tenant, e.g. `GetByIdAsync(int id, int apartmentId, …)` and filter in SQL so a user cannot read another apartment’s row by id.
- **`SaveAsync`**: pass `apartmentId` from the controller (from `ICurrentUser.IdApartment`), and use it in insert/update `WHERE` clauses; for DTOs with `ApartmentId`, **set from the server** in the controller (override the client) when the product rule is “always current tenant from JWT”.

## Controllers

- **GET list (collection):** no `apartmentId` query parameter. Resolve `apartmentId` with `if (currentUser.IdApartment is not { } apartmentId) return 403` (or project-standard error DTO), then call `List{Entity}ForApartmentAsync(apartmentId, …)`.
- **Action names:** use resource phrasing, e.g. `GetVendors`, `GetBankAccounts`, not `GetVendorsByApartmentId` on the public action (routes stay `/Vendors`, `/BankAccounts`, etc.).

## SuperAdmin / tokens without `apartment_id`

If `IdApartment` is null, tenant-scoped list/save/get endpoints should return **403** with a clear error code (e.g. `NO_APARTMENT_CONTEXT`) unless the product explicitly supports a different flow for super-admins on those routes.

## New services (checklist)

When adding a new apartment-scoped feature:

1. **Interface:** `List{YourEntities}ForApartmentAsync(int apartmentId, …)`.
2. **Implementation:** same method name; query `Where(x => x.ApartmentId == apartmentId)` (or the same global+apartment rules as `ExpenseHead` / `IncomeHead` if shared heads apply).
3. **Controller:** one GET that uses `ICurrentUser.IdApartment` and calls the `List{YourEntities}ForApartmentAsync` method.
4. **GetById / Save:** take `apartmentId` as an argument; never trust a client-provided apartment id for authorization.
