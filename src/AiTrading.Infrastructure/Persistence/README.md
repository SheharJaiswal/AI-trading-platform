# MySQL persistence

V2 uses EF Core with `Pomelo.EntityFrameworkCore.MySql` for MySQL persistence.

The design-time factory reads `ConnectionStrings__MySql` from the environment. A local fallback is provided only so EF tooling can construct the context; real credentials must be supplied through environment/user-secret configuration and must never be committed.

When durable persistence is enabled, application startup applies pending EF migrations before initializing the durable portfolio. Docker Compose also applies the generated EF bundle before starting the API, so the schema remains explicitly versioned and startup initialization never runs against a pre-migration model.