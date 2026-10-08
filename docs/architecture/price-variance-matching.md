# Price variance matching

## Deterministic two-way evaluation — PP-15

`MatchResult.EvaluatePriceVariance` compares the reviewed invoice line unit price with the purchase-order line unit price using the `PRICE_VARIANCE` rule effective at the evaluation timestamp:

```text
variance % = ((invoice unit price - PO unit price) / PO unit price) * 100
failed     = abs(exact variance %) > allowed tolerance %
```

A value exactly on the tolerance boundary passes. A failed result stores `PriceVariance`; a passed result has no exception code. The decision uses .NET `decimal` arithmetic without binary floating point or AI. The displayed/persisted variance is rounded to four decimal places, away from zero, while the decision uses the exact calculated decimal value.

The purchase-order unit price must be greater than zero. A zero reference price is rejected because a percentage would be undefined; no financial outcome is invented.

## Reproducibility and isolation

Each persisted `MatchResult` snapshots the invoice and PO unit prices, allowed tolerance, rule identifier and rule version, signed variance, outcome, exception code, and UTC evaluation timestamp. Tenant-aware composite foreign keys ensure the invoice line, PO line, rule, and result all belong to the same tenant. Global query filters fail closed when no tenant is active, and the shared write guard rejects cross-tenant changes.

Database constraints also enforce positive rule versions, valid tolerance bounds, a positive PO reference price, and consistency between the status and exception code. Historical results are retained independently from later rule versions.

## Unit-test coverage — PP-16

The unit suite covers the two acceptance references and the deterministic boundary rules: variance above tolerance fails, variance within tolerance passes, positive and negative values exactly on the boundary pass, equal prices pass with zero tolerance, and a 100% variance passes at the inclusive maximum tolerance. It also covers exact-versus-rounded comparison, invalid zero PO price, cross-tenant inputs, and rule effective dates.

PP-15 does not select candidate PO lines, automate exception resolution, or add a matching UI. Those remain separate backlog slices.
