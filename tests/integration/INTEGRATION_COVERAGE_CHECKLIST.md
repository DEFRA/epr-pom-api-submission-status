# Integration test coverage checklist

Use this when adding or changing HTTP routes so tests stay aligned with the API surface.

Running integration tests requires the `CosmosAccountKey` environment variable (or `Database__AccountKey`); see the **Integration tests** section in [README.md](../README.md).

| Route / area | Primary test class |
|--------------|-------------------|
| `POST /v1/submissions` | `SubmissionTests`, `SubmissionEventTests`, `GoldenPathScenariosTests` |
| `GET /v1/submissions` | `HeaderAndBoundaryTests`, `SubmissionTests` |
| `GET /v1/submissions/{id}` | `SubmissionTests`, `HeaderAndBoundaryTests` (tenancy) |
| `POST /v1/submissions/{id}/submit` | `SubmissionSubmitContractTests` (registration + producer PoM chain) |
| `POST /v1/submissions/{id}/events` | `SubmissionEventTests`, `EventTypeContractTests`, `DecisionIntegrationTests` |
| `GET /v1/submissions/{id}/organisation-details` | `EndpointCoverageTests`, `OrganisationDetailsIntegrationTests` |
| `GET /v1/submissions/get-registration-application-details` | `EndpointCoverageTests`, `ApplicationDetailsIntegrationTests` |
| `GET /v1/submissions/get-packaging-data-resubmission-application-details` | `EndpointCoverageTests`, `ApplicationDetailsIntegrationTests` |
| `GET /v1/submissions/files/{fileId}` | `EndpointCoverageTests` |
| `GET /v1/submissions/{id}/uploadedfile/{fileId}` | `EndpointCoverageTests` |
| `GET /v1/submissions/events/events-by-type/{id}` | `SubmissionEventTests`, `GoldenPathScenariosTests`, `EventTypeContractTests` |
| `GET /v1/submissions/events/get-regulator-*` | `SubmissionEventTests`, `EndpointCoverageTests` |
| `GET /v1/decisions` | `DecisionIntegrationTests`, `GoldenPathScenariosTests` |
| `GET /v1/submissions/{id}/organisation-details-errors` / `warnings` | `ValidationEventErrorTests` |

Update the table when you add a new controller route or a new integration test class.
