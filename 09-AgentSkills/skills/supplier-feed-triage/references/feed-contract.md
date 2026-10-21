# Supplier product feed contract v2 (synthetic)

Applies to the Northstar Foods supplier product-catalog delta feed in the SUP-482 demonstration.

- `schemaVersion` is `2`.
- `casePack` is a JSON integer greater than zero.
- `casePackUom` is a separate string, such as `"EA"`.
- The formatted string `"12 EA"` is not a valid `casePack` value. Do not silently parse or coerce it during triage.
- This feed updates catalog product attributes. It does not publish retail price or on-hand inventory.
- A valid contract does not by itself prove a product update has reached a particular store; verify downstream publication separately.

When diagnosing validation errors, compare the exact raw field type and value with this contract. A schema-version label alone does not make a payload valid.
