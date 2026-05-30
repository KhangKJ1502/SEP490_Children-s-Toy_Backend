# Payment flows and auto-assign

## SE_PAY

- Checkout creates `Pending` order — **no** `order.placed` event (no capacity consumed).
- Auto-assign runs only after webhook marks order `Confirmed` + `PAID` via `order.confirmed`.
- Late payment after cancel: webhook credits customer wallet (`LATE_SEPAY_{attemptCode}`) and raises `SystemPaymentGatewayError` for CS.

## WALLET

- Checkout sets `Confirmed` + `PAID` in one transaction.
- Publishes `order.placed` (triggers auto-assign) and `order.confirmed` + `order.ready_to_pack`.

## COD

- Checkout publishes `order.placed` while order is `Pending`.
- Staff confirm publishes `order.confirmed` (second auto-assign attempt if partial).

## Reconciliation

`OrderAssignmentReconciliationJob` (every 5 min) retries auto-assign for `PAID` operational orders missing Staff or Merch OA.
