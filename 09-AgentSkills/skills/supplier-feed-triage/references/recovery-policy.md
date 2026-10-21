# Supplier feed recovery policy (synthetic)

## Permanent validation failures

- Do not replay malformed messages unchanged; repeated validation failures do not become transient through retry.
- Ask the supplier integration owner to correct and validate the payload against the published contract.
- Before any replay, verify the corrected message set, deduplication/idempotency behavior, message identifiers, and the approved replay scope with the catalog-platform owner.
- Prefer a bounded replay of the verified rejected message IDs, not an unreviewed whole-batch replay.

## Transient downstream failures

- Keep messages within the service's bounded retry policy while retries remain available.
- Check queue/processor status and idempotency before any manual retry.
- Do not conflate a transient HTTP error with a permanent contract-validation rejection.

## Approval boundary

Supplier corrections, replay, rollback, retry-policy changes, and production notifications require action by the designated service owners under the normal incident/change process. A recommendation or an agent response is not approval.
