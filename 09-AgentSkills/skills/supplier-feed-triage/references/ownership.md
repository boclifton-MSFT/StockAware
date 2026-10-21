# Supplier feed incident ownership (synthetic)

| Responsibility | Team | Handoff |
| --- | --- | --- |
| Supplier payload and producer serializer | Supplier Integrations | Confirm the deployed producer version, correct the field representation, and provide a contract-valid sample. |
| Catalog ingestion, validation, and replay | Catalog Platform | Confirm rejected message IDs, idempotency/deduplication behavior, and approved bounded replay scope. |
| Store-level product publication | Store Catalog Distribution | Verify whether the affected SKUs were published to stores and identify actual store coverage. |
| Promotion assortment | Merchandising Operations | Assess whether the 42 promotion-assortment SKUs require a business contingency after catalog publication is checked. |

These names and escalation routes are illustrative. A real deployment must replace them with the organization's verified service catalog, on-call procedures, and incident-command process.
