# Work schedule manual test stubs

Ghi nhận kịch bản kiểm thử API/feature (không có dự án xUnit trong solution). Copy sang test tự động sau khi có `*.Tests.csproj`.

## Task 1 — Clone previous week (`POST api/work-schedules/clone-week`)

### CloneWeek_ClonesSevenDays
- Arrange: Chuẩn bị lịch source Monday có đủ 7 ngày; target Monday không trùng; Admin token.
- Act: `POST .../clone-week?sourceMonday=...&targetMonday=...`
- Assert: `cloned > 0`, target week có các bản `WorkSchedules` mới `Status = Scheduled`; `skipped` chỉ các skip hợp lệ.

### CloneWeek_SkipsPastDates
- Arrange: `targetMonday + offset` có ngày trước `TodayVN`.
- Act: Clone week.
- Assert: Reasons chứa `Past date skipped`; không insert row cho ngày đó.

### CloneWeek_SkipsDuplicates
- Arrange: Target week đã tồn tại một combo `(AccountId, ShiftTemplateId, WorkDate)`.
- Act: Clone week.
- Assert: `Reasons` chứa `Duplicate`; `skipped` tăng; không có bản clone trùng unique.

### CloneWeek_RejectsNonMonday
- Arrange: Query `sourceMonday` không phải thứ Hai.
- Act: Clone week.
- Assert: `VALIDATION_ERROR` / HTTP 400 tương ứng `Result`.

## Task 2 — Mark absent (`PUT api/work-schedules/{id}/absent`)

### MarkAbsent_ReassignsPendingOrders
- Arrange: Pending order có assignment active cho schedule đó; có ca OnDuty còn slot.
- Act: PUT absent.
- Assert: Body `reassignedCount` tăng; order có assignments mới; capacity schedule cũ giảm.

### MarkAbsent_QueuesWhenNoStaff
- Arrange: Pending order chỉ có thể không gán (không OnDuty đủ cặp Staff+Merchandise hoặc full).
- Act: PUT absent.
- Assert: `queuedCount` tăng; `OrderQueue` pending với reason phù hợp (vd. `NO_STAFF_ON_DUTY`).

### MarkAbsent_DeactivatesAssignmentsFirst
- Arrange: Theo dõi EF/SQL assignments trước absent.
- Act: PUT absent.
- Assert: Active assignments của schedule được `IsActive=false` trước khi retry auto-assign.

### MarkAbsent_NoOpWhenNoPendingOrders
- Arrange: Schedule không có pending orders gắn.
- Act: PUT absent.
- Assert: `reassignedCount` và `queuedCount` = 0; schedule `Absent`.

## Task 3 — Shift full admin notification (`shift.full`)

### ShiftFull_NotifiesOnceAtMaxLoad
- Arrange: Capacity `CurrentLoad == MaxLoad - 1`; một assign đẩy lên đủ; DB có `ADMIN_SHIFT_FULL` template; admins role 2.
- Act: Gán đơn (auto-assign hoặc assign queue) để tăng tải.
- Assert: Một Delivery/Bell/email cho admin; `ShiftFullNotifiedAt` set.

### ShiftFull_DoesNotNotifyBelowMaxLoad
- Arrange: Sau assign `CurrentLoad < MaxLoad`.
- Act: Assign.
- Assert: Không outbox `shift.full`.

### ShiftFull_NotifiesAllAdmins
- Arrange: Nhiều tài khoản Admin active.
- Act: Trigger full một schedule.
- Assert: Mỗi admin nhận thông báo (ReferenceId per admin).

### ShiftFull_NoOverflowToNextShift
- Arrange: Đơn tiếp theo vào sau khi một ca đầy.
- Act: Placement / auto-assign.
- Assert: Không tự nhảy sang ca khững; chỉ queue hoặc chờ manual theo luồng hiện tại.

## Task 4 — Morning after evening rest (`POST api/work-schedules`)

### CreateSchedule_RejectsMorningAfterEveningUnder8h
- Arrange: Cùng `AccountId`, template Evening (3) có lịch `WorkDate-1`; template Morning (1), rest < 8h.
- Act: Create schedule Morning.
- Assert: Validation fails với đúng message 8-hour rest.

### CreateSchedule_AllowsMorningAfterEveningWith8hPlus
- Arrange: Evening kết thúc + Morning gắt đầu cách nhau ≥ 8h với calendar VN/date+TimeSpan.
- Act: Create.
- Assert: 201/`Success`; schedule tạo.

## Task 5 — Minimum coverage (create/delete)

### CreateSchedule_RejectsWhenOnlyStaff
- Arrange: Shift slot đã chỉ có staff (warehouse absent/không lịch) sau add vẫn thiếu merch.
- Act: POST create (sales).
- Assert: Message `Each shift must have at least 1 sales staff and 1 warehouse staff`.

### CreateSchedule_RejectsWhenOnlyMerch
- Arrange: Mirror — chỉ merch.
- Act: POST merchandise schedule.
- Assert: Cùng business message.

### DeleteSchedule_RejectsWhenLastStaff
- Arrange: Một nhân Staff duy nhất trên `(WorkDate, ShiftTemplateId)`; delete sẽ vi phạm.
- Act: DELETE schedule.
- Assert: `BUSINESS_RULE_VIOLATION` và message coverage.

### DeleteSchedule_AllowsWhenCoverageRemains
- Arrange: Còn ≥1 Staff và ≥1 Merch sau exclude row.
- Act: DELETE.
- Assert: 200; không còn row.

## Task 6 — Staff swap on edit (`PUT api/work-schedules/{id}`)

Khi admin đổi `AccountId` trên cùng `ScheduleID`, hệ thống chuyển giao trực tiếp `OrderAssignments` (không dùng AutoAssign mặc định). Response: `UpdateWorkScheduleResultDto` với `transferredOrders[]`.

### SwapStaff_TransfersPendingOrders (T1–T2)
- Arrange (T1): Ca `OnDuty`, Staff A có 2 đơn `Pending`, OA `RoleID=3`, `ScheduleID` = ca; `StaffShiftCapacity.CurrentLoad=2`; A thấy 2 đơn trong API đơn.
- Act (T2): `PUT` đổi `accountId` → Staff B (cùng role Staff).
- Assert: HTTP 200; `transferredCount=2`; OA `AccountID=B`; A không còn active OA trên các đơn đó; B thấy 2 đơn Pending; `WorkSchedules.AccountID=B`; `StaffShiftCapacity.AccountID=B`, `CurrentLoad` vẫn 2; `Orders.AssignedToStaffID=B` cho đơn Pending.

### SwapMerch_TransfersConfirmedProcessing (T3)
- Arrange: Merch A trên ca có đơn `Confirmed`/`Processing` (OA RoleID=4) và đơn `Shipped`.
- Act: PUT đổi sang Merch B.
- Assert: B nhận đơn Confirmed/Processing; Shipped không trong `transferredOrders`; OA Shipped vẫn AccountID=A nếu còn active.

### Swap_NoAssignments (T4)
- Arrange: Ca không có OA active.
- Act: PUT đổi người.
- Assert: 200; `transferredCount=0`; `transferredOrders` rỗng; schedule và capacity account đồng bộ.

### SwapStaff_SkipsConfirmedStaffOa (T5)
- Arrange: Staff A có đơn đã `Confirmed` (OA Staff vẫn active trên ca).
- Act: PUT swap → Staff B.
- Assert: OA Staff trên đơn Confirmed **không** đổi `AccountID`; Merch OA không đổi; `transferredCount` không tính đơn đó.

### Swap_ConcurrencyConflict (T6)
- Arrange: Admin 1 load schedule; Admin 2 PUT thành công (UpdatedAt đổi).
- Act: Admin 1 PUT với `expectedUpdatedAt` cũ.
- Assert: HTTP 409 `CONFLICT`; không có transfer nửa vời (OA và schedule nhất quán).

### Swap_RejectsCrossRole (edge)
- Arrange: Ca Staff (role 3).
- Act: PUT `accountId` của tài khoản Merchandise (role 4).
- Assert: 422 / `BUSINESS_RULE_VIOLATION`; message cùng role.

### Swap_RejectsAbsentOrCompleted
- Arrange: `Status` = `Absent` hoặc `Completed`.
- Act: PUT.
- Assert: `BUSINESS_RULE_VIOLATION`; không update.

### Swap_OptionalAutoAssignFallback
- Arrange: Staff A còn OA active đơn Confirmed trên ca; body `runAutoAssignFallback: true`.
- Act: PUT → B.
- Assert: Sau direct transfer, OA còn của A (Confirmed) bị deactivate; `autoAssignReassignedCount` hoặc `autoAssignQueuedCount` có thể tăng; đơn có thể vào queue (không bắt buộc gán lại B).

SQL xác minh:

```sql
SELECT oa.OrderID, oa.AccountID, oa.RoleID, oa.IsActive, oa.Notes, s.StatusName
FROM OrderAssignments oa
JOIN Orders o ON o.OrderID = oa.OrderID
JOIN StatusOrders s ON s.StatusID = o.StatusID
WHERE oa.ScheduleID = @ScheduleId AND oa.IsActive = 1;
```
