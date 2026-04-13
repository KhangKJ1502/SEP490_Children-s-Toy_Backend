You are working on the ToyStore Clean Architecture .NET 8 project.

BEFORE writing any code, you MUST read and strictly follow ALL rules in these files:
1. `CODING_RULES.md` — Architecture, patterns, naming, validation, transaction rules
2. `docs/tests/feature_test.md` — Testing template and workflow

CRITICAL RULES:
- Follow Clean Architecture strictly (Domain → Application → Infrastructure → API)
- Use FluentValidation (AbstractValidator<T>) for ALL DTOs
- Use AutoMapper (Profile classes in Application/Mappings/)
- Use Result<T> pattern for Service returns
- Use UnitOfWork + Transaction for multi-table writes
- ALL user-facing messages in ENGLISH, code comments in VIETNAMESE
- Soft delete only (IsDeleted = true), NEVER hard delete
- After coding a feature, create docs/tests/[feature]_test.md
- After testing, create docs/test-reports/[feature]_report_[date].md
- Use PaginatedResponse<T> for ALL list endpoints
- EF Core Fluent API only, NO Data Annotations on entities
- NO try/catch in Controllers, middleware handles exceptions
- Roles: Guest, Customer, Staff, Merchandise, Admin
- Use ICurrentUserService, NOT HttpContext directly in Services

For the full detailed rules, read CODING_RULES.md at the project root.
