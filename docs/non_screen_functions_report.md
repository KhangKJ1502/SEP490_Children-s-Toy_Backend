# Non-Screen System Functions Analysis Report

This report documents all **Non-Screen System Functions** identified within the backend codebase of the **SEP490 Children's Toy Store** system. Non-screen functions comprise backend processes, scheduled background jobs, webhook callback receivers, event listeners, and utility services that execute autonomously without direct user interface rendering or explicit screen-based user interaction.

---

## 1. Batch Jobs & Scheduled Cron Jobs
These scheduled background tasks inherit from `Microsoft.Extensions.Hosting.BackgroundService` (or implement `IHostedService`) and execute at designated time intervals or daily local times to process data, send reminders, or synchronize system states.

| # | Feature | System Function | Description |
|---|---------|-----------------|-------------|
| 1 | Birthday Greetings | `BirthdayNotificationJob` | **Scheduled Job**: Runs **daily at 08:00 VN Time (calculated dynamically)**.<br>**Input**: Customer `Dob` (Date of Birth) and `CustomerChildren` Dob records.<br>**Output**: Sends birthday promotional greetings via bell and email notifications.<br>**Frequency**: Daily.<br>**Deduplication**: Customer uses a unique `IdempotencyKey` on the `Deliveries` table; child uses `BirthdayNotifiedYear` set on the `CustomerChildren` table. |
| 2 | Voucher Expiry Alert | `VoucherExpiryReminderJob` | **Scheduled Job**: Runs **daily at 09:00 VN Time**.<br>**Input**: Vouchers in `Active` status ending within the next 3 days.<br>**Output**: Dispatches in-app bell notifications to customers who hold eligible unused vouchers (`VoucherUsageLogs` counter < `MaxUsagePerUser`).<br>**Frequency**: Daily. |
| 3 | Low Stock Warnings | `LowStockScanJob` | **Scheduled Job**: Runs **every 1 hour**.<br>**Input**: Products with stock levels at or below their defined `StockThreshold`.<br>**Output**: Dispatches stock alerts to Merchandise staff, respecting the `LowStockNotificationEnabled` flag and enforcing a 24-hour notification cool-down timer per product. |
| 4 | Product Re-stock Alert | `BackInStockJob` | **Scheduled Job**: Runs **every 30 minutes**.<br>**Input**: Products transitioning back into inventory (`Stock` > 0).<br>**Output**: Scans `ProductFollowers` and triggers notifications to alert customers that a followed item is back in stock. |
| 5 | Recommendation Model: Scoring | `ComputeScoresJob` | **Batch Job**: Runs **every 1 hour**.<br>**Input**: MongoDB interaction `Events` from the past 30 days.<br>**Output**: Computes weighted customer affinity score for each product ($Score = \sum weight \times e^{-0.1 \times daysAgo}$) and upserts values to `Recommendation.UserProductScores`. |
| 6 | Recommendation Model: Item Similarity | `ComputeSimilarityJob` | **Batch Job**: Runs **every hour (Interval configured in `RecommendationOptions`)**.<br>**Input**: Product details (Category, Brand, Price Range, Age, Sex, Origin, Material).<br>**Output**: Calculates Item-to-Item cosine similarities using one-hot attribute feature vectors and persists top-10 recommendations for each active product to the `ItemSimilarities` table. |
| 7 | Recommendation Model: Trending Products | `ComputeTrendingJob` | **Batch Job**: Runs **every 1 hour**.<br>**Input**: Customer activity events logged during the last 24 hours.<br>**Output**: Calculates a trending score ($Score = view \times 1 + purchase \times 5 + cart \times 3$) and publishes top-20 popular products globally and per category. |
| 8 | Recommendation Model: Profile Aggregator | `UpdateUserProfilesJob` | **Batch Job**: Runs **every 1 hour**.<br>**Input**: `UserProductScores`, Completed Orders history, and active Product attributes.<br>**Output**: Aggregates top categories, top brands, price ranges, and recently viewed products. Saves profile summaries to MongoDB `user_profiles` and excludes purchased items from recommendations. |
| 9 | Campaign Auto-Expiration | `CampaignApprovedExpireJob` | **Scheduled Job**: Runs **every 1 hour**.<br>**Input**: Campaigns in `Approved` state that have exceeded their scheduled launch deadline (`ApprovedExpireAt`).<br>**Output**: Auto-cancels stagnant campaigns and updates audit trail logs. |
| 10 | Campaign Validation | `CampaignReferenceRevalidationJob` | **Scheduled Job**: Runs **every 15 minutes**.<br>**Input**: ADMIN campaigns in `Scheduled` or `Waiting` status.<br>**Output**: Performs a safety check to re-validate associated email templates and reference data; cancels campaigns with audit trails if reference data is no longer valid. |
| 11 | Campaign Stale Lock Recovery | `CampaignStaleLockRecoveryJob` | **Scheduled Job**: Runs **every 5 minutes**.<br>**Input**: Campaign schedule entries stuck in `Dispatched` status with expired `LockedAt` timestamps (typically caused by worker crashes).<br>**Output**: Releases stale locks and resets schedule statuses back to pending. |

---

## 2. Background Services & Workers
These components perform ongoing background data synchronization, monitor messaging structures, handle automated transaction processing, and consume queue payloads.

| # | Feature | System Function | Description |
|---|---------|-----------------|-------------|
| 12 | Transactional Outbox | `OutboxProcessorJob` | **Background Worker**: Runs **every 10 seconds**.<br>**Input**: Batch rows (max 20) in the `DomainEventOutbox` table that have not been processed.<br>**Output**: Executes matching event subscribers (`IOutboxEventHandler`) via optimistic locking. Handles automatic retry cycles up to 5 times. Dispatches a system administrative alert if failures persist. |
| 13 | Campaign Dispatcher | `CampaignSchedulerJob` | **Background Service**: Runs **every 1 minute**.<br>**Input**: Scheduled ADMIN campaigns whose start time (`ScheduledAt`) has passed.<br>**Output**: Calls `ICampaignNotificationService` to begin asynchronous campaign message delivery. |
| 14 | Automated Order Cancellation | `OrderStatusWorker` | **Background Service**: Runs **every 5 minutes**.<br>**Input**: Core customer orders pending payment.<br>**Output**: Automatically cancels orders unpaid after 24 hours and calls the database stored procedure `SP_SyncPromotionVoucherStatus` to synchronize voucher usage. |
| 15 | SePay Checkout Cleanup | `SePayExpiryJob` | **Background Service**: Runs **every 1 minute**.<br>**Input**: Pending SE_PAY (Bank Transfer) transaction checkouts exceeding their Time-To-Live (TTL).<br>**Output**: Auto-cancels checkout records, restores inventory stock, and releases applied discount vouchers. |
| 16 | Shipping Integration Retry | `GhnShippingRetryJob` | **Background Service**: Runs **every 10 minutes**.<br>**Input**: Shipping provider transactions (`ShippingProviderTransactions`) lacking external order codes (failed creations) where `RetryCount` > 0.<br>**Output**: Retries calls to the external GHN API to generate shipping orders and mirrors current GHN shipment status directly to the `Orders.StatusID` using configured map tables. |
| 17 | Auto-Assign Staff Reconciliation | `OrderAssignmentReconciliationJob` | **Background Service**: Runs **every 5 minutes**.<br>**Input**: Customer orders missing complete staff/shift assignments.<br>**Output**: Retries the auto-assignment engine to dispatch appropriate packing and delivery employees. |
| 18 | Order Assignment Queue | `OrderQueueRetryJob` | **Background Service**: Runs **every 2 minutes**.<br>**Input**: Queued orders waiting for staff availability.<br>**Output**: Triggers shift assignment logic for the oldest queued orders first. |
| 19 | Shift Lifecycle Scheduler | `ShiftLifecycleJob` | **Background Service**: Runs **every 2 minutes**.<br>**Input**: Employee work schedules (`WorkSchedules`) matching current VN time.<br>**Output**: Transition scheduled shifts from `Scheduled` to `OnDuty` once StartTime is reached, and from `OnDuty` to `Completed` after EndTime is passed. Publishes domain events (`ShiftStarted` / `ShiftEndedWithPendingOrders`). |
| 20 | Flash Sale Alerts | `FlashSaleActivationJob` | **Background Service**: Runs **every 1 minute**.<br>**Input**: Upcoming flash sale slots that are transitioning to `Active` status.<br>**Output**: Broadcasts notifications to all customer accounts who opted in to the specific flash sale slot. |
| 21 | Blog Publisher | `BlogPublishWorker` | **Background Service**: Runs **every 1 minute**.<br>**Input**: Blog entries in scheduled status whose publishing deadline has passed.<br>**Output**: Toggles status to `Published` to make the article publicly visible. |
| 22 | AI Blog Moderation | `BlogCommentModerationPollJob` | **Background Service**: Runs **every 30 seconds**.<br>**Input**: Pending blog comments and replies.<br>**Output**: Sends content payloads to an external AI moderation sidecar service to inspect content safety, automatically flagging or approving posts. |
| 23 | Manual Comment Review | `BlogCommentManualReviewTimeoutJob` | **Background Service**: Runs **every 1 hour**.<br>**Input**: Pending comments left in manual review beyond their review deadline.<br>**Output**: Rejects comment publication. |
| 24 | Comment Permission Restorer | `BlogCommentPermissionUnlockJob` | **Background Service**: Runs **every 1 hour**.<br>**Input**: Accounts with temporary comment bans.<br>**Output**: Restores blogging and commenting rights when the ban duration expires. |
| 25 | Recommendation Buffer Flush | `FlushEventsJob` | **Background Service**: Runs **every 5 minutes**.<br>**Input**: Buffered tracking events logged in MongoDB `session_events` where `flushedAt` is null.<br>**Output**: Inserts tracking payloads into the primary SQL Server transactional database (`Interaction.Events`) and stamps a `flushedAt` timestamp onto MongoDB documents. |

---

## 3. API Endpoints (Non-Screen Internal/External)
These endpoints are exposed by controllers to facilitate callbacks, machine-to-machine integrations, and container orchestrations rather than returning rendered HTML or direct screen UI views.

| # | Feature | System Function / Endpoint | Description |
|---|---------|-----------------------------|-------------|
| 26 | Shipping Webhook (GHN) | `GhnWebhookController`<br>`POST /api/webhooks/ghn` | **Webhook Receiver (External)**: Receives state changes from Giao Hang Nhanh (GHN).<br>**Input**: Webhook payload (JSON order status update).<br>**Output**: Processes status mapping, updates databases, and returns HTTP `200 OK`. Records transient DB errors on failure.<br>**Security**: Validates header token `X-Webhook-Token`. |
| 27 | Payment Webhook (SePay) | `PaymentWebhookController`<br>`POST /api/payment/webhook` | **Webhook Receiver (External)**: Bank transfer processor callback.<br>**Input**: Bank transaction webhook content.<br>**Output**: Checks payload validity, maps to corresponding orders, records payments, processes virtual wallet transactions, and updates order states to Paid.<br>**Security**: Verified via signature match header `Authorization: Apikey <Token>`. |
| 28 | Universal Shipping Webhooks | `ShippingWebhooksController`<br>`POST /api/webhooks/shipping/{provider}` | **Webhook Receiver (External)**: Route handler receiving callbacks from different logistics providers (e.g. GHN, ViettelPost, etc.).<br>**Input**: Route parameter `{provider}` and raw payload body.<br>**Output**: Triggers corresponding `IShippingWebhookService` provider strategies and logs status changes.<br>**Security**: Validated via `X-Webhook-Token`. |

---

## 4. Event Handlers & Subscribers
These represent synchronous or asynchronous application-level subscribers implementing `IOutboxEventHandler`. They process serialized domain events triggered by CRUD state changes in the business domain.

| # | Feature | Event / System Function | Description |
|---|---------|-------------------------|-------------|
| 31 | Order Placed Callback | `OrderPlacedHandler`<br>(Event: `OrderPlaced`) | **Event Listener**: Catches order finalizations.<br>**Input**: Placed order identifier and customer details.<br>**Output**: Triggers confirmation dispatch pipelines (bell notifications and customer confirmation emails). |
| 32 | Payment Logging | `PaymentNotificationHandler`<br>(Event: `PaymentProcessed`) | **Event Listener**: Triggers post-payment workflows.<br>**Input**: Transaction logs.<br>**Output**: Notifies customers of success/failure and updates staff dashboard stats. |
| 33 | Shipping Notifications | `ShippingEventHandlers`<br>(Events: `Shipped`, `Delivered`, etc.) | **Event Listener**: Reacts to delivery milestone changes.<br>**Input**: Shipping status changes.<br>**Output**: Notifies customers of package transit status (Dispatched, Out For Delivery, Delivered, or Failed). |
| 34 | Stock Replenishment Alert | `WishlistPriceDropHandler`<br>(Event: `PriceDropped`) | **Event Listener**: Monitors promotional discounts.<br>**Input**: Product ID and discount margins.<br>**Output**: Dispatches wishlist alerts to all customers who have added the product to their wishlist, promoting conversion. |
| 35 | New Voucher Broadcast | `VoucherNewHandler`<br>(Event: `VoucherCreated`) | **Event Listener**: Broadcaster for marketing incentives.<br>**Input**: Voucher details.<br>**Output**: Triggers push alerts and notifications to system-wide customer bases. |
| 36 | Employee Shift Alerts | `ShiftAssignmentHandlers`<br>(Event: `ShiftAssigned`) | **Event Listener**: Internal scheduling notifications.<br>**Input**: Assignment records.<br>**Output**: Dispatches immediate alerts to staff informing them of assigned working hours and store responsibilities. |
| 37 | Merchandiser Inventory Events | `MerchNotificationHandlers` | **Event Listener**: Stock alert dispatcher. Listens to inventory and product stock event triggers and alerts merchandise management employees. |
| 38 | Operations Alerts | `StaffNotificationHandlers` | **Event Listener**: Generic staff dispatcher. Relays warehouse and dispatch operations alerts to on-duty employees. |
| 39 | System Failures | `AdminNotificationHandlers`<br>(Event: `SystemFailure`) | **Event Listener**: System health alerts. Listens for fatal backend anomalies, such as outbox events exceeding maximum retries, and sounds alarms to the system administrator. |

---

## 5. Utility & Helper Services
These services represent internal processing engines that generate files, render templates, validate constraints, and route payloads across hardware/network adapters.

| # | Feature | Utility Interface & Implementation | Description |
|---|---------|-----------------------------------|-------------|
| 40 | SMTP Email Transport | `SmtpNotificationEmailSender`<br>(implements `IEmailSender`) | **Helper Service**: Establishes SMTP socket links to relay email payloads to external destination addresses via standard C# email clients. |
| 41 | SendGrid Gateway | `SendGridEmailSender`<br>(implements `IEmailSender`) | **Helper Service**: Gateway wrapper that connects with the SendGrid cloud Web API to transmit bulk marketing and transactional emails. |
| 42 | Notification Pipeline | `EmailChannel` & `WebBellChannel` | **Helper Service**: Multi-channel delivery routing nodes that process notification items (validates recipient preferences, writes database records, triggers physical adapters, manages local safety retries). |
| 43 | Template Engine | `NotificationTemplateRenderer` | **Helper Service**: Dynamic string rendering engine. Matches keys in HTML files, performs variable substitution (such as username, child birthday profiles, prices, order codes), and outputs formatted email/bell body strings. |
| 44 | Domain Event Outbox | `DomainEventPublisher`<br>(implements `IDomainEventPublisher`) | **Helper Service**: Utility utilized during transactional units of work. Serializes domain triggers into JSON strings and inserts rows into the outbox database table to ensure transactional integrity. |
