**SEP490 ToyStore  —**  Tài liệu kỹ thuật: Luồng rút tiền (Withdrawal)

**SEP490 — ToyStore**

*Tài liệu kỹ thuật*

**LUỒNG RÚT TIỀN**

*Withdrawal Flow via PayOS Payout*

Database v3.2  •  Migration v1.0

|**Phiên bản**|v1.0|
| :- | :- |
|**Trạng thái**|**Draft**|
|**Phụ thuộc**|ToyStore DB v3.2 (Wallets, WalletTransactions)|
|**Cổng thanh toán**|PayOS Payout API|
|**Tác giả**|Team SEP490|


# **1. Tổng quan**
Chức năng rút tiền cho phép người dùng chuyển số dư từ ví điện tử ToyStore về tài khoản ngân hàng thông qua cổng PayOS Payout. Migration v1.0 bổ sung 8 bước cấu trúc DB hoàn chỉnh trên nền ToyStore v3.2.

## **1.1. Mục tiêu thiết kế**
- Đảm bảo tính nguyên tử (atomicity) — số dư không bao giờ âm và không mất tiền khi có lỗi.
- Chống race condition — sử dụng UPDLOCK + ROWLOCK khi đọc/ghi ví.
- Idempotency — mỗi lệnh rút có ReferenceId duy nhất; webhook lặp lại không xử lý 2 lần.
- Audit trail đầy đủ — mọi thay đổi trạng thái đều được ghi vào WithdrawalStatusHistory.
- Số dư khả dụng = Balance - LockedBalance — tiền đang chờ PayOS không thể rút thêm.

## **1.2. Kiến trúc tổng thể**
Luồng rút tiền bao gồm 3 giai đoạn chính:

|**Giai đoạn**|**Tên**|**Mô tả**|
| :-: | :-: | :-: |
|**1**|**LOCK**|Kiểm tra số dư, khóa tiền tạm thời, tạo WithdrawalRequest|
|**2**|**PROCESSING**|Gọi PayOS Payout API, chờ webhook xác nhận|
|**3A**|**COMMIT (SUCCESS)**|Webhook thành công: trừ Balance thật, mở LockedBalance|
|**3B**|**ROLLBACK (FAILED)**|Webhook thất bại hoặc timeout: mở LockedBalance, hoàn tiền|


# **2. Cấu trúc Database**
## **2.1. Thay đổi bảng Wallets**
Bổ sung cột LockedBalance để theo dõi số tiền đang bị tạm giữ chờ PayOS xác nhận.

|**Cột**|**Kiểu**|**Default**|**Ý nghĩa**|
| :-: | :-: | :-: | :-: |
|**LockedBalance (MỚI)**|DECIMAL(12,0)|0|Số tiền đang bị lock chờ PayOS — không thể rút thêm|
|Balance (CŨ)|DECIMAL(12,0)|0|Tổng số dư (bao gồm cả phần đang lock)|

|**ℹ️ Lưu ý:** Số dư khả dụng (AvailableBalance) = Balance - LockedBalance. View vw\_WalletAvailableBalance được tạo sẵn để query nhanh.|
| :- |

## **2.2. Bảng SavedBankAccounts**
Lưu danh sách tài khoản ngân hàng đã từng dùng của user để gợi ý lần sau.

|**Cột**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**SavedBankAccountID**|INT IDENTITY|PK tự tăng|
|**AccountID**|INT|FK → Accounts. Chủ tài khoản|
|**BankBin**|VARCHAR(10)|Mã BIN ngân hàng. VD: 970415 (VietinBank). Dùng cho PayOS Payout field toBin. Xem thêm cột BankCode bên dưới|
|**BankName**|NVARCHAR(100)|Tên đầy đủ ngân hàng (snapshot lúc lưu)|
|**BankShortName**|VARCHAR(20)|Tên viết tắt. VD: "VietinBank"|
|**BankCode**|VARCHAR(20)|Mã code ngân hàng dùng cho BankLookup API. VD: "VCB", "TCB". Lấy từ field code trả về bởi GET /bank/list. Khác với BankBin — đây là string code, không phải số nguyên BIN.|
|**AccountNumber**|VARCHAR(50)|Số tài khoản ngân hàng|
|**AccountName**|NVARCHAR(200)|Tên chủ TK (lookup từ VietQR/PayOS, cập nhật mỗi lần dùng)|
|**IsDefault**|BIT|Tài khoản mặc định. Chỉ 1 TK/user (UNIQUE INDEX)|
|**IsDeleted**|BIT|Soft delete|
|**LastUsedAt**|DATETIME2(0)|Lần cuối dùng TK này rút tiền|

|**🔒 Constraint:** Constraint UQ\_SavedBankAccounts\_AccountBin: mỗi cặp (AccountID, BankBin, AccountNumber) là unique. SP\_Withdrawal\_Lock tự upsert bảng này mỗi khi user rút.|
| :- |

## **2.3. Bảng WithdrawalRequests**
Bảng trung tâm: mỗi lệnh rút tiền tương ứng 1 row. Lifecycle đầy đủ từ PENDING đến SUCCESS/FAILED/CANCELLED.

|**Cột**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**WithdrawalID**|INT IDENTITY|PK tự tăng|
|**WalletID**|INT|FK → Wallets|
|**AccountID**|INT|FK → Accounts|
|**WalletTransactionID**|INT NULL|FK → WalletTransactions. Chỉ có khi Status = SUCCESS|
|**ReferenceId**|VARCHAR(100)|Idempotency key. Format: WD-{accountId}-{timestamp}. UNIQUE|
|**Amount**|DECIMAL(12,0)|Số tiền rút. Tối thiểu 10,000 VND. CHECK > 0|
|**ToBankBin**|VARCHAR(10)|BIN ngân hàng đích|
|**ToBankName**|NVARCHAR(100)|Tên ngân hàng đích|
|**ToAccountNumber**|VARCHAR(50)|Số TK ngân hàng đích|
|**ToAccountName**|NVARCHAR(200)|Tên chủ TK đích (đã verify qua PayOS)|
|**PayosPayoutId**|VARCHAR(100) NULL|ID payout từ PayOS (data.id trong response). Set khi COMMIT|
|**PayosTransactionId**|VARCHAR(100) NULL|ID transaction PayOS (transactions[0].id). Set khi COMMIT|
|**PayosRawResponse**|NVARCHAR(MAX) NULL|Toàn bộ JSON response từ PayOS để debug|
|**Status**|VARCHAR(20)|Trạng thái hiện tại (xem bảng lifecycle bên dưới)|
|**FailReason**|NVARCHAR(500) NULL|Lý do thất bại. Bắt buộc khi Status = FAILED|
|**RetryCount**|TINYINT|Số lần retry (dùng cho background job)|
|**ProcessingAt**|DATETIME2(0) NULL|Thời điểm gọi PayOS API|
|**CompletedAt**|DATETIME2(0) NULL|Thời điểm hoàn thành (SUCCESS hoặc FAILED)|
|**CancelledAt**|DATETIME2(0) NULL|Thời điểm huỷ lệnh (nếu CANCELLED)|

## **2.4. Lifecycle trạng thái WithdrawalRequests**

|**Status**|**Từ trạng thái**|**Nguồn**|**Mô tả**|
| :-: | :-: | :-: | :-: |
|**PENDING**|(khởi tạo)|SP\_Lock|Vừa tạo, đã lock balance, chờ gọi PayOS|
|**PROCESSING**|PENDING|App/Service|Đã gọi PayOS API, chờ webhook callback|
|**SUCCESS**|PENDING / PROCESSING|SP\_Commit|Webhook SUCCESS: tiền đã chuyển thành công, Balance bị trừ|
|**FAILED**|PENDING / PROCESSING|SP\_Rollback|PayOS thất bại / timeout: LockedBalance được mở, Balance không đổi|
|**CANCELLED**|PENDING|User/Admin|User hoặc hệ thống huỷ trước khi gọi PayOS|

|**🔒 Constraint:** Constraint CK\_WithdrawalRequests\_TxnConsistency: WalletTransactionID chỉ được phép có giá trị khi Status = SUCCESS và CompletedAt IS NOT NULL. Tránh ghi nhầm transaction khi chưa xong.|
| :- |

## **2.5. Bảng PayosWebhookLogs**
Lưu toàn bộ webhook nhận từ PayOS để audit và debug. Hỗ trợ idempotency qua WebhookEventId.

|**Cột**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**WebhookLogID**|BIGINT IDENTITY|PK|
|**WithdrawalID**|INT NULL|FK → WithdrawalRequests. NULL nếu không tìm được ReferenceId|
|**WebhookEventId**|VARCHAR(100) NULL|ID unique của event từ PayOS. UNIQUE INDEX để chặn duplicate|
|**ReferenceId**|VARCHAR(100) NULL|data.referenceId từ payload webhook|
|**EventType**|VARCHAR(50) NULL|Loại event. VD: "payout.completed"|
|**IsSignatureValid**|BIT|Kết quả verify chữ ký webhook từ PayOS|
|**RawPayload**|NVARCHAR(MAX)|Toàn bộ JSON webhook nhận được|
|**ProcessStatus**|VARCHAR(20)|RECEIVED → PROCESSED / IGNORED / ERROR|

## **2.6. Bảng WithdrawalStatusHistory**
Audit trail tự động. Trigger TR\_WithdrawalRequests\_StatusHistory ghi vào bảng này mỗi khi Status trong WithdrawalRequests thay đổi.

|**Cột**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**HistoryID**|INT IDENTITY|PK|
|**WithdrawalID**|INT|FK → WithdrawalRequests|
|**FromStatus**|VARCHAR(20) NULL|Trạng thái trước. NULL cho lần đầu (INSERT)|
|**ToStatus**|VARCHAR(20)|Trạng thái sau|
|**Source**|VARCHAR(20)|USER / SYSTEM / WEBHOOK / ADMIN / JOB|
|**ChangedBy**|INT NULL|FK → Accounts. NULL nếu do system/webhook|
|**Note**|NVARCHAR(500) NULL|Ghi chú lý do thay đổi|
|**CreatedAt**|DATETIME2(0)|Thời điểm ghi log|


# **3. Stored Procedures**
Migration cung cấp 3 SP tương ứng 3 giai đoạn của luồng rút tiền. Tất cả đều sử dụng SET XACT\_ABORT ON và BEGIN/COMMIT TRANSACTION.

## **3.1. SP\_Withdrawal\_Lock — Khởi tạo lệnh rút**
Gọi khi user bấm "Xác nhận rút tiền". Thực hiện toàn bộ Phase 1 trong 1 transaction.

### **Input parameters**

|**Parameter**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**@AccountID**|INT|ID user rút tiền|
|**@Amount**|DECIMAL(12,0)|Số tiền rút (tối thiểu 10,000 VND)|
|**@ToBankBin**|VARCHAR(10)|BIN ngân hàng đích|
|**@ToBankName**|NVARCHAR(100)|Tên ngân hàng đích|
|**@ToBankShortName**|VARCHAR(20)|Tên viết tắt ngân hàng|
|**@ToAccountNumber**|VARCHAR(50)|Số TK đích|
|**@ToAccountName**|NVARCHAR(200)|Tên chủ TK (đã lookup qua VietQR/PayOS)|
|**@Description**|NVARCHAR(255)|Mô tả lệnh rút. Default: "Rút tiền về tài khoản ngân hàng"|

### **Output parameters**

|**Parameter**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**@ReferenceId OUTPUT**|VARCHAR(100)|Idempotency key đã sinh|
|**@WithdrawalID OUTPUT**|INT|ID của WithdrawalRequest vừa tạo|
|**@ErrorCode OUTPUT**|INT|0 = thành công; khác 0 = lỗi (xem bảng error codes)|
|**@ErrorMessage OUTPUT**|NVARCHAR(500)|Mô tả lỗi chi tiết|

### **Error codes**

|**Code**|**Mô tả**|
| :-: | :-: |
|**0**|Thành công|
|**1**|Duplicate ReferenceId|
|**2**|Không tìm thấy ví của AccountID|
|**3**|Ví đang bị Frozen|
|**4**|Số dư khả dụng không đủ (Balance - LockedBalance < Amount)|
|**5**|Amount < 10,000 VND (dưới mức tối thiểu)|
|**99**|Lỗi hệ thống (CATCH block — xem @ErrorMessage để biết chi tiết)|

### **Các bước thực hiện bên trong SP**
1. Sinh ReferenceId = "WD-{AccountID}-{timestamp\_ms}".
1. Kiểm tra duplicate ReferenceId — nếu tồn tại thì báo lỗi 1.
1. Lấy row Wallets với UPDLOCK + ROWLOCK để chặn concurrent update.
1. Validate: ví tồn tại, không bị Frozen, số dư khả dụng đủ, Amount >= 10,000.
1. UPDATE Wallets: LockedBalance += Amount.
1. INSERT WithdrawalRequests với Status = PENDING.
1. Upsert SavedBankAccounts: insert nếu TK chưa có, update LastUsedAt nếu đã có.
1. COMMIT — trigger TR\_WithdrawalRequests\_StatusHistory tự ghi lịch sử.

## **3.2. SP\_Withdrawal\_Commit — Xác nhận thành công**
Gọi khi nhận webhook PayOS với status SUCCESS. Thực hiện Phase 3A.

### **Input parameters**

|**Parameter**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**@WithdrawalID**|INT|ID lệnh rút cần commit|
|**@PayosPayoutId**|VARCHAR(100)|data.id từ PayOS response|
|**@PayosTransactionId**|VARCHAR(100)|transactions[0].id từ PayOS response|
|**@PayosRawResponse**|NVARCHAR(MAX)|Raw JSON response để debug (có thể NULL)|

### **Các bước thực hiện bên trong SP**
1. Lấy WithdrawalRequest với UPDLOCK.
1. Idempotency check: nếu Status đã là SUCCESS thì COMMIT và return ngay (ErrorCode = 0).
1. Validate Status phải là PENDING hoặc PROCESSING.
1. Lấy Balance hiện tại từ Wallets với UPDLOCK.
1. UPDATE Wallets: Balance -= Amount, LockedBalance -= Amount. Cả hai cùng giảm.
1. INSERT WalletTransactions: TxnType = "Withdrawal", Direction = "DR", Status = "Completed".
1. UPDATE WithdrawalRequests: Status = SUCCESS, WalletTransactionID = ID vừa tạo, CompletedAt = GETDATE().
1. COMMIT.

|**💡 Lưu ý kế toán:** Tại bước 5: Balance giảm thật + LockedBalance cũng giảm. Net effect: tiền ra khỏi ví. Nếu dùng ROLLBACK (lỗi), cả hai vẫn giữ nguyên trước khi SP này được gọi.|
| :- |

## **3.3. SP\_Withdrawal\_Rollback — Hoàn lại khi thất bại**
Gọi khi webhook PayOS báo FAILED hoặc khi background job phát hiện timeout. Thực hiện Phase 3B.

### **Input parameters**

|**Parameter**|**Kiểu**|**Mô tả**|
| :-: | :-: | :-: |
|**@WithdrawalID**|INT|ID lệnh rút cần rollback|
|**@FailReason**|NVARCHAR(500)|Lý do thất bại (bắt buộc)|

### **Các bước thực hiện bên trong SP**
1. Lấy WithdrawalRequest với UPDLOCK.
1. Idempotency: nếu Status đã là FAILED hoặc CANCELLED → COMMIT và return.
1. Validate: không thể rollback nếu đã SUCCESS.
1. UPDATE Wallets: LockedBalance -= Amount (hoàn lại phần đã lock, Balance không đổi).
1. UPDATE WithdrawalRequests: Status = FAILED, FailReason, CompletedAt.
1. COMMIT.

|**✅ Kết quả:** Sau Rollback: Balance không thay đổi, LockedBalance giảm về mức trước khi lock. Tức là user được hoàn lại toàn bộ số tiền đã định rút.|
| :- |


# **4. Luồng xử lý chi tiết**
## **4.1. Phase 1: User khởi tạo lệnh rút**
1. Frontend validate: kiểm tra Amount >= 10,000 và không vượt AvailableBalance (query từ view vw\_WalletAvailableBalance).
1. User nhập / chọn thông tin ngân hàng đích.
1. User bấm xác nhận PIN ví (bảng WalletPins kiểm tra).
1. Backend gọi SP\_Withdrawal\_Lock → nhận @WithdrawalID và @ReferenceId.
1. Backend gọi PayOS Payout API với ReferenceId làm idempotency key.
1. Backend cập nhật WithdrawalRequests.Status = PROCESSING, lưu PayosPayoutId.
1. Backend insert PayosWebhookLogs với ProcessStatus = RECEIVED khi có webhook.

## **4.2. Phase 2: Xử lý webhook PayOS**
1. PayOS gọi callback URL với payload JSON có WebhookEventId.
1. Backend verify chữ ký (x-signature header).
1. Backend INSERT PayosWebhookLogs ngay lập tức (ghi vào DB trước khi xử lý).
1. Kiểm tra duplicate: nếu WebhookEventId đã tồn tại → set ProcessStatus = IGNORED, return HTTP 200.
1. Tra cứu WithdrawalID qua ReferenceId trong payload.
1. Nếu status = SUCCESS → gọi SP\_Withdrawal\_Commit.
1. Nếu status = FAILED → gọi SP\_Withdrawal\_Rollback với lý do từ payload.
1. Cập nhật PayosWebhookLogs.ProcessStatus = PROCESSED hoặc ERROR.
1. Return HTTP 200 (luôn return 200 để PayOS không retry vô hạn).

## **4.3. Phase 3: Background job xử lý timeout**
Job chạy định kỳ (mỗi 5-10 phút) để phát hiện lệnh rút bị treo quá lâu:

1. Query WithdrawalRequests WHERE Status IN ('PENDING', 'PROCESSING') AND CreatedAt < DATEADD(MINUTE, -30, GETDATE()).
1. Với mỗi lệnh quá hạn: gọi SP\_Withdrawal\_Rollback với FailReason = "Timeout after 30 minutes".
1. Ghi log vào WithdrawalStatusHistory với Source = "JOB".
1. Nếu SP trả ErrorCode = 3 (đã SUCCESS) → bỏ qua, không xử lý. SP\_Withdrawal\_Rollback đã có guard kiểm tra Status trước khi update, nên dù webhook SUCCESS và job timeout chạy đồng thời, lệnh rút sẽ không bị rollback nhầm — chỉ 1 trong 2 được commit thực sự.

|**⚡ Performance:** Index IX\_WithdrawalRequests\_Pending đã được tạo sẵn (Status IN PENDING/PROCESSING, ORDER BY CreatedAt ASC) để job query hiệu quả.|
| :- |

## **4.4. Sơ đồ flow tóm tắt**

|**Bước**|**Hành động**|**Kết quả DB**|
| :-: | :-: | :-: |
|**1**|User gửi yêu cầu rút, nhập PIN|—|
|**2**|SP\_Withdrawal\_Lock chạy|Wallets.LockedBalance += Amount; INSERT WithdrawalRequests (PENDING)|
|3|Gọi PayOS Payout API|WithdrawalRequests.Status = PROCESSING|
|**4A**|Nhận webhook SUCCESS từ PayOS|SP\_Commit: Balance -= Amount, LockedBalance -= Amount, INSERT WalletTransaction|
|**4B**|Nhận webhook FAILED / timeout|SP\_Rollback: LockedBalance -= Amount (Balance không đổi)|
|5|Gửi notification kết quả cho user|INSERT Notification.Deliveries — trigger từ webhook handler sau khi SP\_Commit / SP\_Rollback thành công. Nếu notification fail: không rollback lệnh rút, ghi log lỗi và để background job retry riêng.|

## **4.5. PayOS Payout API — Tạo lệnh chi đơn**
Backend gọi API này sau khi SP\_Withdrawal\_Lock chạy thành công (Status = PENDING). Đây là bước chuyển tiền thực sự qua cổng PayOS Payout.

**Endpoint & Authentication**

|**Thuộc tính**|**Giá trị**|
| :- | :- |
|**Method & URL**|**POST**  https://api-merchant.payos.vn/v1/payouts|
|**Header: x-api-key**|API Key của kênh. Lấy từ PayOS Dashboard → My payOS|
|**Header: x-client-id**|Client ID của kênh. Lấy từ PayOS Dashboard → My payOS|
|**Header: x-idempotency-key**|Điền vào @ReferenceId trả về từ SP\_Withdrawal\_Lock. Đảm bảo idempotency: gọi lại cùng key không tạo thêm giao dịch mới.|
|**Header: x-signature**|Chữ ký xác thực request. Backend tự sinh theo thuật toán HMAC-SHA256 bằng Checksum Key PayOS cung cấp.|

**Request Body (application/json)**

|**Field**|**Kiểu / Bắt buộc**|**Mô tả**|
| :- | :- | :- |
|**referenceId**|string / required|Idempotency key. Điền vào @ReferenceId OUTPUT từ SP\_Withdrawal\_Lock. VD: WD-42-1720000000000|
|**amount**|integer / required|Số tiền (VND). Lấy từ WithdrawalRequests.Amount. Tối thiểu 10.000 VND.|
|**description**|string / required|Nội dung chuyển tiền hiển trên biên lai ngân hàng. VD: Rút tiền ToyStore WD-42-1720000000000|
|**toBin**|string / required|Mã BIN ngân hàng đích. Lấy từ WithdrawalRequests.ToBankBin. VD: 970415 (VietinBank)|
|**toAccountNumber**|string / required|Số tài khoản ngân hàng đích. Lấy từ WithdrawalRequests.ToAccountNumber|
|**category**|string[] / optional|Danh mục thanh toán. ToyStore dùng ["withdrawal"]|

**Response 200 — Tạo lệnh chi thành công**

|**Field trả về**|**Kiểu**|**Mô tả & Màp vào DB**|
| :- | :- | :- |
|data.id|string|PayOS Payout ID. Lưu vào WithdrawalRequests.PayosPayoutId|
|data.transactions[0].id|string|PayOS Transaction ID. Lưu vào WithdrawalRequests.PayosTransactionId|
|data.transactions[0].toAccountName|string|Tên chủ tài khoản đích do PayOS trả về sau khi xác minh. Lưu vào WithdrawalRequests.ToAccountName và SavedBankAccounts.AccountName|
|data.transactions[0].state|string|Trạng thái xử lý. Khi API trả về thì thường là PROCESSING — kết quả thực tế được xác nhận qua webhook sau.|
|data.approvalState|string|Trạng thái phê duyệt tổng thể. Thường trả về PROCESSING khi mới tạo. Kết thúc là COMPLETED hoặc FAILED qua webhook.|

**Response các mã lỗi khác**

|**HTTP**|**Ý nghĩa & Xử lý trong ToyStore**|
| :- | :- |
|**401**|Unauthorized — x-api-key hoặc x-client-id sai. Kiểm tra lại PayOS credentials trong config.|
|**403**|Forbidden — x-signature sai hoặc thiếu. Kiểm tra lại thuật toán ký HMAC-SHA256 phía backend.|
|**429**|Too Many Requests — Gọi API quá nhiều trong thời gian ngắn. Backend implement exponential backoff: retry tối đa 3 lần, delay 2s → 4s → 8s. Sau 3 lần thất bại: gọi SP\_Withdrawal\_Rollback ngay, không chờ background job.|
|**500**|PayOS Internal Error — Ghi log PayosRawResponse để debug. Retry tối đa 3 lần với exponential backoff (2s → 4s → 8s). Nếu vẫn lỗi sau 3 lần: gọi SP\_Withdrawal\_Rollback để mở lock ngay, không chờ background job 30 phút.|

**⚠ Hạn chế: PayOS Payout không hỗ trợ tra cứu tên chủ tài khoản trước khi chuyển tiền**

API POST /v1/payouts chỉ chấp nhận toBin + toAccountNumber — **không có endpoint riêng để lookup tên chủ tài khoản** trước khi gửi lệnh. Tên chủ TK (toAccountName) chỉ xuất hiện trong response sau khi PayOS xác minh nội bộ và xử lý giao dịch.

**Giải pháp khuyến nghị —** Sử dụng BankLookup API để tra tên chủ TK trước khi user xác nhận. Frontend hiển thị tên cho user xác nhận, sau đó backend truyền @ToAccountName vào SP\_Withdrawal\_Lock để lưu. PayOS sẽ xác minh lại nội bộ khi chuyển tiền thực tế.

**4.4.1. BankLookup API — Tra cứu tên chủ tài khoản**

**Bước 1 — Lấy danh sách ngân hàng:** GET https://api.banklookup.net/bank/list — không cần auth. Response trả về mảng ngân hàng với các field: code, bin, short\_name, lookup\_supported. Cache kết quả (IMemoryCache, TTL 24h). Chỉ hiển thị ngân hàng có lookup\_supported = 1 trong dropdown frontend.

**Bước 2 — Tra cứu tên chủ TK:** POST https://api.banklookup.net — gọi khi user nhập số tài khoản, trước bước xác nhận rút. Headers bắt buộc: x-api-key và x-api-secret (lưu vào appsettings.json "BankLookup:ApiKey" và "BankLookup:ApiSecret").

// Request body

{ "bank": "VCB", "account": "0123456789" }

// Response: 200 OK

{ "code": 200, "success": true, "data": { "ownerName": "NGUYEN VAN A" } }

{ "code": 422, "success": false, "msg": "NOT\_FOUND", "data": null }   // TK không tồn tại hoặc ngân hàng không hỗ trợ → chặn luồng rút

**⚠ Quan trọng:** Field bank nhận code (VD: "VCB") — KHÔNG phải BIN. Backend cần lưu thêm cột BankCode VARCHAR(20) vào bảng SavedBankAccounts để dùng cho lần rút tiếp theo mà không cần gọi API lại (xem schema 2.2). Nếu response là 422: chặn luồng, hiển thị lỗi cho user, không tạo WithdrawalRequest.

## **4.6. Test API trên Postman**
**Collection Variables cần thiết lập**

|**Variable**|**Mô tả**|
| :- | :- |
|x-payout-client-id|Client ID lấy từ PayOS Dashboard → My payOS|
|x-payout-api-key|API Key lấy từ PayOS Dashboard → My payOS|
|payout-checksum-key|Checksum Key dùng để tính HMAC-SHA256 cho x-signature. Lấy từ PayOS Dashboard.|
|api-merchant-host|Base URL. VD: https://api-merchant.payos.vn|
|payout-reference-id|Điền @ReferenceId trả về từ SP\_Withdrawal\_Lock. VD: WD-42-1720000000000|

**Request mẫu chuẩn (Body)**

{

"referenceId": "WD-42-1720000000000",

"amount": 100000,

"description": "Rút tiền ToyStore WD-42-1720000000000",

"toBin": "970418",

"toAccountNumber": "7411190507",

"category": ["withdrawal"]

}

**Lưu ý các lỗi phổ biến khi test**

|**Vấn đề**|**Cách sửa**|
| :- | :- |
|referenceId để rỗng|Phải điền giá trị thực. Lấy {{payout-reference-id}} từ collection variable.|
|x-idempotency-key hardcode|Phải khớp với referenceId trong body. Dùng {{payout-reference-id}} để cùng nguồn.|
|x-signature hardcode / sai|Phải tính HMAC-SHA256 từ raw JSON body + checksum key. Dùng Pre-request Script (xem bảng dưới).|
|category tự đặt như "hoa"|PayOS chỉ chấp nhận enum hợp lệ của họ. Dùng "withdrawal" cho luồng rút tiền ToyStore.|

**Pre-request Script tính x-signature tự động**

Dán đoạn script này vào tab Pre-request Script của request trong Postman:

// Lấy checksum key từ collection variable

const checksumKey = pm.collectionVariables.get("payout-checksum-key");

const rawBody = pm.request.body.raw;

// Tính HMAC-SHA256

const signature = CryptoJS.HmacSHA256(rawBody, checksumKey).toString();

// Gắn vào header x-signature

pm.request.headers.upsert({ key: "x-signature", value: signature });

**C# — Tính x-signature khi gọi PayOS Payout API**

Dùng trong PayOsPayoutService.cs khi build HttpRequestMessage gọi POST /v1/payouts:

// using System.Security.Cryptography; using System.Text;



private static string ComputePayosSignature(string rawJsonBody, string checksumKey)

{

`    `var keyBytes = Encoding.UTF8.GetBytes(checksumKey);

`    `var dataBytes = Encoding.UTF8.GetBytes(rawJsonBody);

`    `using var hmac = new HMACSHA256(keyBytes);

`    `{

`        `var hashBytes = hmac.ComputeHash(dataBytes);

`        `return Convert.ToHexString(hashBytes).ToLower(); // lowercase hex, không dùng BitConverter

`    `}

}

Gọi hàm này trong service: var sig = ComputePayosSignature(jsonBody, \_config["PayOS:ChecksumKey"]); rồi set header request.Headers.Add("x-signature", sig);


## **4.7. PayOS Webhook API — Cấu hình và nhận thông báo rút tiền**
Sau khi gọi POST /v1/payouts, PayOS xử lý giao dịch bất đồng bộ và gọi callback về Webhook URL đã đăng ký để thông báo kết quả. ToyStore phải cấu hình Webhook URL trước, sau đó xác thực và xử lý payload đúng cách.
### **4.7.1. Đăng ký Webhook URL với PayOS**
Gọi API này một lần khi setup môi trường (staging / production). PayOS sẽ gửi một payload mẫu tới URL đó để kiểm tra webhook có hoạt động hay không. Nếu thành công, URL được lưu vào kênh thanh toán.

|**Thuộc tính**|**Giá trị**|
| :- | :- |
|**Method & URL**|**POST**  https://api-merchant.payos.vn/confirm-webhook|
|**Auth Headers**|x-client-id + x-api-key (giống Payout API)|
|**Request Body**|"webhookUrl": "https://api.toystore.vn/webhooks/payos-payout"|
|**Response 200**|Trả về webhookUrl, accountNumber, accountName, name, shortName của kênh thanh toán. Xác nhận đăng ký thành công.|
|**Response 400**|Webhook URL invalid — URL không public, không trả về HTTP 200, hoặc endpoint bị lỗi khi nhận payload mẫu.|
|**Response 5XX**|Lỗi từ hệ thống ToyStore — endpoint của mình crash khi PayOS gọi thử. Kiểm tra log controller webhook.|
### **4.7.2. Payload PayOS gửi về ToyStore (Webhook Callback)**
PayOS gọi HTTP POST tới Webhook URL của ToyStore với cấu trúc JSON sau. ToyStore phải trả về HTTP 200 trong vòng 30 giây để xác nhận đã nhận.

|**Field (JSON)**|**Kiểu**|**Mô tả & Xử lý trong ToyStore**|
| :- | :- | :- |
|code|string|Mã kết quả. "00" = thành công, khác = thất bại|
|success|boolean|true = giao dịch thành công. Kiểm tra cửng với code == "00" trước khi commit|
|signature|string|Chữ ký HMAC-SHA256 của PayOS. **Bắt buộc verify trước khi xử lý**. Xem cách tính tại 4.7.3.|
|data.orderCode|integer|PayOS Payout tự sinh — không map trực tiếp vào WithdrawalID. Dùng data.reference để lookup WithdrawalRequests.ReferenceId thay thế (xem lưu ý bên dưới)|
|data.amount|integer|Số tiền thực tế đã chuyển (VND). Verify khớp với WithdrawalRequests.Amount|
|data.reference|string|Mã giao dịch ngân hàng (bank reference). Lưu vào PayosWebhookLogs.WebhookEventId để chặn duplicate|
|data.transactionDateTime|string|Thời điểm giao dịch. Format yyyy-MM-dd HH:mm:ss. Lưu vào PayosWebhookLogs.RawPayload (cột ReceivedAt không tồn tại trong schema — dùng CreatedAt để ghi thời điểm nhận webhook)|
|data.accountNumber|string|Số tài khoản BaoKim của ToyStore nhận tiền (tài khoản đối chiếu). Dùng để confirm giao dịch đúng kênh.|
|data.paymentLinkId|string|PayOS internal payment link ID. Lưu vào PayosWebhookLogs.RawPayload để audit|

**⚠ Lưu ý quan trọng — Tra cứu WithdrawalID từ webhook:** PayOS Payout tự sinh data.orderCode — giá trị này không bằng WithdrawalID của ToyStore. Để tra cứu lệnh rút, dùng data.reference (bank reference) để join với WithdrawalRequests.ReferenceId, thay vì dùng data.orderCode.
### **4.7.3. Xác thực Signature Webhook (Bắt buộc)**
Mọi request từ PayOS gửi kèm signature = HMAC-SHA256 của nội dung sắp xếp theo key, dùng Checksum Key. Backend phải tính lại và so sánh trước khi gọi SP\_Commit / SP\_Rollback. Nếu sai → bị IgnorePattern, log lại, trả về HTTP 200 để PayOS không retry.

|**Bước**|**Chi tiết**|
| :- | :- |
|**1. Lấy các field data**|Lấy từ payload.data: amount, description, orderCode, reference, transactionDateTime, paymentLinkId, accountNumber, counterAccountBankId, counterAccountBankName, counterAccountName, counterAccountNumber, virtualAccountName, virtualAccountNumber, currency, code, desc|
|**2. Sắp xếp theo key (A→Z)**|Sắp xếp alphabetical: accountNumber=...&amount=...&code=...&counterAccountBankId=...&...|
|**3. Tính HMAC-SHA256**|HMAC\_SHA256(sortedQueryString, ChecksumKey)|
|**4. So sánh**|So sánh kết quả tính được với payload.signature. Không khớp → IsSignatureValid = false, bị qua, trả HTTP 200.|

**⚠ Lưu ý quan trọng khi cấu hình Webhook**

• Webhook URL phải là HTTPS public (không được dùng localhost). Khi dev local dùng ngrok hoặc cloudflare tunnel.

• Controller phải trả HTTP 200 ngay lập tức (kể cả khi signature sai hay duplicate). Xử lý logic chạy bất đồng bộ sau. Nếu trả 5XX, PayOS sẽ retry và gọi lại nhiều lần.

• Webhook của luồng **rút tiền (Payout)** và webhook của luồng **thanh toán đơn hàng** dùng chung 1 URL nhưng khác nhau ở x-client-id đăng ký — khuyến nghị tách thành 2 endpoint riêng (/webhooks/payos-payment và /webhooks/payos-payout) để dễ maintenance.


# **5. Indexes & Ràng buộc quan trọng**
## **5.1. Indexes**

|**Index**|**Bảng**|**Mục đích**|
| :-: | :-: | :-: |
|**UQ\_SavedBankAccounts\_OneDefault**|SavedBankAccounts|Chỉ 1 TK mặc định/user (IsDefault=1, IsDeleted=0)|
|**IX\_WithdrawalRequests\_Account**|WithdrawalRequests|Query lịch sử rút tiền của user theo ngày giảm dần|
|**IX\_WithdrawalRequests\_Pending**|WithdrawalRequests|Background job tìm lệnh PENDING/PROCESSING quá hạn|
|**IX\_WithdrawalRequests\_PayosPayoutId**|WithdrawalRequests|Tra cứu nhanh khi nhận webhook theo PayOS ID|
|**UQ\_PayosWebhookLogs\_EventId**|PayosWebhookLogs|Chặn xử lý duplicate webhook cùng EventId|
|**IX\_PayosWebhookLogs\_Unprocessed**|PayosWebhookLogs|Job retry tìm webhook RECEIVED/ERROR chưa xử lý|
|**IX\_WithdrawalStatusHistory\_Withdrawal**|WithdrawalStatusHistory|Query lịch sử thay đổi trạng thái của 1 lệnh rút|

## **5.2. Business Constraints**

|**Constraint**|**Quy tắc**|
| :-: | :-: |
|**CK\_Wallets\_LockedNotExceedBalance**|LockedBalance <= Balance (không thể lock nhiều hơn số dư thực)|
|**CK\_WithdrawalRequests\_TxnConsistency**|WalletTransactionID IS NOT NULL chỉ khi Status = SUCCESS và CompletedAt IS NOT NULL|
|**CK\_WithdrawalRequests\_FailConsistency**|FailReason IS NOT NULL bắt buộc khi Status = FAILED|
|**CK\_WithdrawalRequests\_Amount**|Amount > 0 (không cho rút 0 đồng)|
|**CHECK Amount >= 10000 (SP)**|SP\_Withdrawal\_Lock validate Amount >= 10,000 VND trước khi lock|
|**CK\_WalletTransactions\_TxnType**|Sau migration: TxnType IN (TopUp, Payment, Refund, Withdrawal, LockReserve, UnlockRelease)|


# **6. Security & Best Practices**
## **6.1. Chống race condition**
- Tất cả SP đọc Wallets với WITH (UPDLOCK, ROWLOCK) trước khi update.
- Không bao giờ đọc balance → kiểm tra → update trong 2 câu lệnh riêng biệt ngoài transaction.
- SET XACT\_ABORT ON đảm bảo rollback tự động nếu có lỗi.

## **6.2. Idempotency**
- ReferenceId = "WD-{accountId}-{timestamp}" là unique theo UNIQUE constraint.
- SP\_Commit và SP\_Rollback đều có guard: nếu Status đã ở trạng thái cuối → return thành công ngay mà không làm gì.
- Webhook được kiểm tra qua UQ\_PayosWebhookLogs\_EventId: duplicate EventId → IGNORED.

## **6.3. Verify chữ ký webhook**
- Luôn verify x-signature header từ PayOS trước khi xử lý payload.
- Kết quả lưu vào PayosWebhookLogs.IsSignatureValid.
- Nếu chữ ký sai: vẫn INSERT log với IsSignatureValid = 0, nhưng không gọi SP nào.

## **6.4. PIN ví**
- Trước khi gọi SP\_Withdrawal\_Lock: backend phải verify PIN ví qua bảng WalletPins.
- WalletPins.FailedAttempts tối đa 3, TotalFailedAttempts tối đa 6.
- Khi LockedUntil IS NOT NULL và chưa qua: báo lỗi "Ví bị khóa tạm thời".

## **6.5. Giới hạn rút tiền (khuyến nghị)**
- Nên thêm validation ở application layer (không có sẵn trong DB). Lưu các giá trị giới hạn trong appsettings.json section "WithdrawalLimits" để dễ thay đổi mà không cần deploy lại DB:
  - Tối thiểu: 10,000 VND (đã validate trong SP).
  - Tối đa mỗi giao dịch: tùy cấu hình (VD: 50,000,000 VND).
  - Tối đa mỗi ngày: tùy cấu hình (VD: 100,000,000 VND).
  - Số lần rút mỗi ngày: tùy cấu hình (VD: tối đa 5 lần).


# **7. Checklist tích hợp cho Developer**
## **7.1. Trước khi deploy migration**
- Backup database production.
- Chạy migration trên môi trường staging trước.
- Kiểm tra constraint CK\_WalletTransactions\_TxnType: drop constraint cũ tự động bằng dynamic SQL đã có trong migration.
- Verify view vw\_WalletAvailableBalance đã tạo thành công.

## **7.2. Sau khi deploy**
- Cấu hình PayOS Payout API key và callback URL.
- Đăng ký background job timeout cleanup (chạy mỗi 5-10 phút).
- Test end-to-end với TK ngân hàng test của PayOS sandbox.
- Monitor bảng PayosWebhookLogs: ProcessStatus = ERROR cần được xử lý.
- Monitor WithdrawalRequests: PENDING > 30 phút là dấu hiệu bất thường.

## **7.3. Queries hữu ích**
**Kiểm tra số dư khả dụng của user:**

|SELECT AccountID, Balance, LockedBalance, AvailableBalance, Status FROM vw\_WalletAvailableBalance WHERE AccountID = @AccountID|
| :- |

**Lịch sử rút tiền của user:**

|SELECT wr.\*, wsh.FromStatus, wsh.ToStatus, wsh.CreatedAt AS ChangedAt FROM WithdrawalRequests wr LEFT JOIN WithdrawalStatusHistory wsh ON wr.WithdrawalID = wsh.WithdrawalID WHERE wr.AccountID = @AccountID ORDER BY wr.CreatedAt DESC|
| :- |

**Tìm lệnh rút bị treo (cần xử lý manual):**

|SELECT \* FROM WithdrawalRequests WHERE Status IN ('PENDING','PROCESSING') AND CreatedAt < DATEADD(MINUTE, -30, GETDATE()) ORDER BY CreatedAt ASC|
| :- |

**Webhook lỗi cần retry:**

|SELECT \* FROM PayosWebhookLogs WHERE ProcessStatus IN ('RECEIVED','ERROR') ORDER BY CreatedAt ASC|
| :- |

# **8. Phụ lục: Sơ đồ quan hệ (ERD tóm tắt)**
Các bảng liên quan đến luồng rút tiền và quan hệ giữa chúng:

|**Bảng**|**Quan hệ**|**Bảng liên kết**|
| :-: | :-: | :-: |
|**Accounts**|1 → N|Wallets, WithdrawalRequests, SavedBankAccounts, WithdrawalStatusHistory|
|**Wallets**|1 → N|WalletTransactions, WithdrawalRequests, WalletPins|
|**WithdrawalRequests**|1 → N|WithdrawalStatusHistory, PayosWebhookLogs|
|**WithdrawalRequests**|N → 1|WalletTransactions (chỉ khi SUCCESS)|
|**PayosWebhookLogs**|N → 1|WithdrawalRequests (nullable)|
|**SavedBankAccounts**|N → 1|Accounts|

*— Hết tài liệu —*
Confidential — Internal Use Only	Trang ...
