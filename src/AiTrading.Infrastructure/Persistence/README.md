# MySQL persistence

V2 uses EF Core with `Pomelo.EntityFrameworkCore.MySql` for MySQL persistence.

The design-time factory reads `ConnectionStrings__MySql` from the environment. A local fallback is provided only so EF tooling can construct the context; real credentials must be supplied through environment/user-secret configuration and must never be committed.

Apply migrations explicitly with the EF tooling after configuring a real connection string. Application startup does not automatically mutate the schema.
