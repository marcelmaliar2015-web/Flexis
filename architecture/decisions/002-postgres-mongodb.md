# PostgreSQL and MongoDB

## Context

Flexis needs structured records and flexible documents.

## Decision

PostgreSQL via EF Core for relational data. MongoDB via MongoDB.Driver for documents. Both are required at runtime. Health checks name them `postgres` and `mongo`. Mongo selection times out after 3 seconds. PostgreSQL uses connection `Timeout=3`.

## Consequences

Pick PostgreSQL when the model is relational and transactional. Pick MongoDB when the payload is a document. Do not duplicate the same entity in both stores.
