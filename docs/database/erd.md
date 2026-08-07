# Entity Relationship Diagram — ToyStore (SEP490)

> **Cập nhật lần cuối:** 2026-05-26
> **Schema version:** v3.5 — Add Refund/Return Tables
> **SQL script đầy đủ:** [`docs/database/schema.sql`](./schema.sql)


---

## Sơ đồ ERD

```mermaid
erDiagram

    %% ════════════════════════════════
    %% 1. USER MANAGEMENT
    %% ════════════════════════════════

    Roles {
        tinyint     RoleID      PK
        varchar50   RoleName    UK
        nvarchar255 Description
        datetime2   CreatedAt
    }

    Accounts {
        int         AccountID    PK
        tinyint     RoleID       FK
        varchar20   EmployeeCode UK  "nullable"
        nvarchar100 AccountName
        varchar15   PhoneNumber      "nullable"
        varchar255  Email        UK
        varchar500  Image            "nullable"
        varchar255  PasswordHash
        bit         IsActive
        bit         IsDeleted
        varchar20   Provider         "nullable — Google/Facebook"
        datetime2   CreatedAt
        datetime2   UpdatedAt        "nullable"
    }

    CustomerChildren {
        int      ChildID              PK
        int      AccountID            FK
        tinyint  SexID                FK  "nullable"
        nvarchar100 FullName
        nvarchar50  NickName              "nullable"
        date     DOB
        smallint BirthdayNotifiedYear     "nullable — năm gửi thông báo sinh nhật gần nhất"
        bit      IsDeleted
        datetime2 CreatedAt
        datetime2 UpdatedAt               "nullable"
    }

    BlockReasons {
        smallint    ReasonID   PK
        varchar50   ReasonCode     "nullable"
        nvarchar200 Content
        nvarchar500 Description    "nullable"
        bit         IsDeleted
        datetime2   CreatedAt
    }

    UserBlockHistory {
        int      BlockID         PK
        int      AccountID       FK
        int      BlockedBy       FK  "nullable"
        smallint ReasonID        FK
        int      UnblockedBy     FK  "nullable"
        int      UnblockedByJobID FK "nullable"
        datetime2 BlockedAt
        datetime2 BlockedUntil
        datetime2 UnblockedAt       "nullable"
    }

    Addresses {
        int         AddressID      PK
        int         AccountID      FK
        nvarchar100 RecipientName      "nullable"
        varchar15   RecipientPhone     "nullable"
        nvarchar500 AddressLine
        nvarchar100 Ward               "nullable"
        nvarchar100 City
        bit         IsDefault
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt          "nullable"
    }

    %% ════════════════════════════════
    %% 2. PRODUCT CATALOG (PIM)
    %% ════════════════════════════════

    SuperCategories {
        smallint    SuperCategoryID   PK
        nvarchar100 SuperCategoryName UK
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt         "nullable"
    }

    Categories {
        smallint    CategoryID      PK
        smallint    SuperCategoryID FK
        nvarchar100 CategoryName    UK
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt       "nullable"
    }

    Materials {
        smallint    MaterialID   PK
        nvarchar100 MaterialName UK
        nvarchar500 Description      "nullable"
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt        "nullable"
    }

    Ages {
        tinyint   AgeID    PK
        varchar50 AgeRange UK
        datetime2 CreatedAt
    }

    Sexes {
        tinyint    SexID   PK
        nvarchar20 SexName UK
        datetime2  CreatedAt
    }

    Origins {
        smallint    OriginID   PK
        nvarchar100 OriginName UK
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt  "nullable"
    }

    Brands {
        smallint    BrandID   PK
        nvarchar100 BrandName UK
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt "nullable"
    }

    PriceRanges {
        tinyint      PriceRangeID  PK
        decimal18_2  PriceRangeMin
        decimal18_2  PriceRangeMax
        bit          IsDeleted
        datetime2    CreatedAt
        datetime2    UpdatedAt     "nullable"
    }

    Promotions {
        int          PromotionID   PK
        nvarchar200  PromotionName
        varchar50    PromotionCode UK
        decimal5_2   DiscountPercent "0-100%"
        varchar20    Status          "Scheduled|Active|Expired|Inactive"
        datetime2    StartDate
        datetime2    EndDate
        int          CreatedBy     FK
        bit          IsDeleted
        datetime2    CreatedAt
    }

    Products {
        int         ProductID      PK
        nvarchar255 ProductName
        decimal18_6 Price
        int         Quantity       "CHECK >= 0"
        varchar20   ProductStatus  "Active|Inactive|OutOfStock|Discontinued"
        bit         IsDeleted
        smallint    StockThreshold "default 10"
        bit         LowStockNotificationEnabled
        datetime2   LastLowStockNotifiedAt  "nullable"
        smallint    CategoryID     FK
        smallint    BrandID        FK  "nullable"
        tinyint     PriceRangeID   FK  "nullable"
        int         PromotionID    FK  "nullable"
        datetime2   CreatedAt
        datetime2   UpdatedAt          "nullable"
    }

    ProductDetails {
        int      ProductID  PK  FK  "1-1 với Products"
        smallint MaterialID FK      "nullable"
        tinyint  AgeID      FK      "nullable"
        tinyint  SexID      FK      "nullable"
        smallint OriginID   FK      "nullable"
        nvarcharMAX Description     "nullable"
    }

    ProductImages {
        int       ImageID   PK
        int       ProductID FK
        varchar500 ImageUrl
        bit       IsMain       "Unique per product when = 1"
        datetime2 CreatedAt
        datetime2 UpdatedAt   "nullable"
    }

    %% ════════════════════════════════
    %% 3. ORDER MANAGEMENT (OMS)
    %% ════════════════════════════════

    StatusOrders {
        tinyint     StatusID   PK
        varchar50   StatusName UK
        nvarchar255 Description    "nullable"
        datetime2   CreatedAt
        datetime2   UpdatedAt      "nullable"
    }

    Orders {
        int          OrderID              PK
        int          AccountID            FK
        tinyint      StatusID             FK
        int          AssignedToStaffID    FK  "nullable"
        varchar30    OrderCode            UK
        nvarchar100  ShippingName
        varchar15    ShippingPhone
        nvarchar500  ShippingAddress
        nvarchar100  ShippingDistrict
        nvarchar100  ShippingCity
        datetime2    OrderDate
        varchar20    PaymentMethod        "BANK_TRANSFER|MOMO|SHIP_CODE|ZALOPAY|VNPAY|WALLET"
        varchar20    PaymentStatus        "PENDING|PAID|FAILED|EXPIRED"
        varchar50    PaymentCode
        datetime2    PaidAt               "nullable"
        decimal18_2  SubTotal             "auto-sync by trigger"
        decimal18_2  VoucherDiscountAmount "auto-sync by trigger"
        decimal18_2  EstimatedShippingFee
        decimal18_2  ActualShippingFee    "nullable"
        decimal18_2  TotalAmount          "auto-calc by trigger"
        nvarchar500  CancelReason         "nullable"
        int          CancelledBy          FK  "nullable"
        bit          IsDeleted
        datetime2    CreateAt
        datetime2    UpdatedAt            "nullable"
    }

    OrderDetails {
        int          OrderDetailID PK
        int          OrderID       FK
        int          ProductID     FK
        nvarchar255  ProductName       "snapshot"
        varchar500   ProductImage      "nullable, snapshot"
        smallint     Quantity          "CHECK > 0"
        decimal18_2  UnitPrice
        decimal18_2  DiscountAmount    "default 0"
        decimal18_2  LineTotal         "COMPUTED PERSISTED"
        datetime2    CreatedAt
    }

    OrderStatusHistory {
        int       HistoryID  PK
        int       OrderID    FK
        tinyint   StatusID   FK
        int       ChangedBy  FK  "nullable"
        nvarchar500 Note         "nullable"
        datetime2 ChangedAt
        datetime2 CreateAt
        datetime2 UpdatedAt     "nullable"
    }

    ShiftTemplates {
        tinyint    ShiftTemplateID PK
        nvarchar50 ShiftName       UK
        time       StartTime
        time       EndTime
        smallint   MaxOrdersPerShift
        bit        IsActive
        datetime2  CreatedAt
        datetime2  UpdatedAt       "nullable"
    }

    WorkSchedules {
        int       ScheduleID     PK
        int       AccountID      FK
        tinyint   ShiftTemplateID FK
        date      WorkDate
        varchar20 Status         "Scheduled|OnDuty|Completed|Absent|Cancelled"
        int       CreatedBy      FK
        datetime2 CreatedAt
        datetime2 UpdatedAt      "nullable"
    }

    StaffShiftCapacity {
        int       CapacityID  PK
        int       ScheduleID  FK  UK
        int       AccountID   FK
        smallint  CurrentLoad
        smallint  MaxLoad
        datetime2 UpdatedAt   "nullable"
    }

    OrderAssignments {
        int       AssignmentID PK
        int       OrderID      FK
        int       ScheduleID   FK
        int       AccountID    FK
        tinyint   RoleID       FK  "3=Staff|4=Merchandise"
        bit       IsActive
        datetime2 AssignedAt
        int       AssignedBy   FK  "nullable"
        nvarchar200 Notes         "nullable"
    }

    OrderQueue {
        int       QueueID    PK
        int       OrderID    FK  UK
        datetime2 QueuedAt
        varchar50 Reason
        int       AssignedBy FK  "nullable"
        datetime2 ResolvedAt "nullable"
        bit       IsResolved
    }

    %% ════════════════════════════════
    %% 4. SHOPPING
    %% ════════════════════════════════

    Cart {
        int       CartID    PK
        int       AccountID FK  UK  "1 cart per user"
        datetime2 CreatedAt
        datetime2 UpdatedAt "nullable"
    }

    CartItems {
        int         CartItemID      PK
        int         CartID          FK
        int         ProductID       FK
        smallint    Quantity            "CHECK 1-999"
        decimal18_2 PriceAtThatTime     "giá lúc thêm vào giỏ"
        decimal18_2 CurrentPrice        "giá hiện tại"
        bit         IsSelected
        datetime2   AddedAt
        datetime2   RemovedAt           "nullable — soft remove"
    }

    Wishlists {
        int       WishlistID PK
        int       AccountID  FK
        int       ProductID  FK
        datetime2 CreatedAt
    }

    %% ════════════════════════════════
    %% 5. VOUCHERS & PROMOTIONS
    %% ════════════════════════════════

    VoucherTypes {
        tinyint     VoucherTypeID   PK
        nvarchar100 VoucherTypeName UK
        datetime2   CreatedAt
    }

    Vouchers {
        int          VoucherID       PK
        tinyint      VoucherTypeID   FK
        int          CreatedBy       FK  "nullable"
        varchar30    VoucherCode     UK
        nvarchar255  VoucherName
        decimal18_2  DiscountAmount      "CHECK > 0"
        varchar10    VoucherScope        "Product|Shipping"
        decimal18_2  MinOrderAmount      "nullable"
        int          Quantity            "CHECK >= 0"
        smallint     MaxUsagePerUser     "nullable"
        varchar15    Status              "Scheduled|Active|Inactive|Expired"
        datetime2    StartDate
        datetime2    EndDate
        bit          IsDeleted
        datetime2    CreatedAt
        datetime2    UpdatedAt           "nullable"
    }

    OrderVouchers {
        int         OrderID              FK  PK
        int         VoucherID            FK  PK
        decimal18_2 DiscountAmountApplied
    }

    VoucherUsageLogs {
        int       UsageID   PK
        int       VoucherID FK
        int       AccountID FK
        int       OrderID   FK
        datetime2 UsedAt
    }

    %% ════════════════════════════════
    %% 6. BLOG & CONTENT
    %% ════════════════════════════════

    BlogCategories {
        smallint    BlogCategoryID    PK
        nvarchar100 BlogCategoriesName UK
        nvarchar500 Description           "nullable"
        datetime2   CreatedAt
    }

    BlogPosts {
        int         BlogPostID   PK
        int         AccountID    FK
        int         ApprovedBy   FK  "nullable"
        nvarchar255 BlogTitle
        nvarcharMAX BlogContent
        varchar500  BlogThumbnail    "nullable"
        varchar20   Status           "Draft|Pending|Approved|Rejected|Scheduled|Published"
        nvarchar500 Reason           "nullable"
        bit         IsFeatured
        datetime2   BlogAt           "nullable"
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt        "nullable"
    }

    BlogPostCategories {
        int      BlogPostID     FK  PK
        smallint BlogCategoryID FK  PK
    }

    Banners {
        int       BannerID     PK
        nvarchar255 BannerName
        varchar500 ImageUrl
        varchar500 LinkUrl         "nullable"
        varchar20  Position        "HomePage|CategoryPage|ProductPage|CheckoutPage"
        tinyint   DisplayOrder
        bit       IsActive
        bit       IsDefault
        int       CreatedBy     FK
        datetime2 CreatedAt
        datetime2 UpdatedAt        "nullable"
    }

    %% ════════════════════════════════
    %% 7. REVIEWS & REACTIONS
    %% ════════════════════════════════

    ReviewProducts {
        int         ReviewID   PK
        int         AccountID  FK
        int         ProductID  FK
        int         OrderID    FK
        tinyint     Rating         "1-5"
        nvarcharMAX Comment        "nullable"
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt      "nullable"
    }

    ReviewProductImages {
        int       ReviewProductImageID PK
        int       ReviewProductID      FK
        varchar500 ImageURL
        bit       IsDeleted
        datetime2 CreatedAt
    }

    ReviewProductReplies {
        int         ReplyProductID  PK
        int         ReviewProductID FK
        int         AccountID       FK
        nvarcharMAX Content
        bit         IsDeleted
        datetime2   CreatedAt
    }

    ReviewProductReactions {
        int      ReactionProductID PK
        int      ReviewProductID   FK
        int      AccountID         FK
        varchar10 ReactionType         "Like|Dislike"
        bit      IsDeleted
        datetime2 CreatedAt
    }

    %% ════════════════════════════════
    %% 8. PAYMENT & WALLET
    %% ════════════════════════════════

    Wallets {
        int         WalletID   PK
        int         AccountID  FK  UK  "1 wallet per user"
        char3       Currency       "VND"
        decimal18_2 Balance        "CHECK >= 0"
        varchar10   Status         "Active|Frozen|Closed"
        datetime2   LastTransactionAt "nullable"
        datetime2   CreatedAt
        datetime2   UpdatedAt         "nullable"
    }

    WalletTransactions {
        int         WalletTransactionID PK
        int         WalletID            FK
        int         AccountID           FK
        int         RelatedOrderID      FK  "nullable"
        varchar10   TxnType                 "TopUp|Payment|Refund"
        char2       Direction               "CR|DR"
        decimal18_2 Amount                  "CHECK > 0"
        decimal18_2 BalanceBefore
        decimal18_2 BalanceAfter
        varchar10   Method                  "Bank|EWallet|Cash|Wallet"
        varchar100  IdempotencyKey      UK  "nullable"
        varchar15   Status                  "Pending|Completed|Failed|Cancelled"
        nvarchar500 Reason                  "nullable"
        datetime2   CreatedAt
        datetime2   CompletedAt             "nullable"
    }

    PaymentHistory {
        int         PaymentHistoryID    PK
        int         AccountID           FK
        int         OrderID             FK
        int         WalletTransactionID FK  "nullable"
        varchar20   PaymentStatus
        varchar20   PaymentMethod
        varchar100  TransactionCode         "nullable"
        decimal18_2 Amount
        datetime2   CreatedAt
    }

    SavedBankAccounts {
        int         SavedBankAccountID PK
        int         AccountID          FK
        varchar10   BankBin
        nvarchar100 BankName
        varchar20   BankShortName
        varchar20   BankCode           "nullable"
        varchar50   AccountNumber
        nvarchar200 AccountName
        bit         IsDefault
        bit         IsDeleted
        datetime2   LastUsedAt         "nullable"
        datetime2   CreatedAt
    }

    WithdrawalRequests {
        int         WithdrawalID        PK
        int         WalletID            FK
        int         AccountID           FK
        int         WalletTransactionID FK "nullable"
        varchar100  ReferenceId         UK
        decimal18_2 Amount
        varchar10   ToBankBin
        nvarchar100 ToBankName
        varchar50   ToAccountNumber
        nvarchar200 ToAccountName
        varchar100  PayosPayoutId       "nullable"
        varchar100  PayosTransactionId  "nullable"
        nvarcharMAX PayosRawResponse    "nullable"
        varchar20   Status
        nvarchar500 FailReason          "nullable"
        tinyint     RetryCount
        datetime2   ProcessingAt        "nullable"
        datetime2   CompletedAt         "nullable"
        datetime2   CancelledAt         "nullable"
        datetime2   CreatedAt
    }

    StatusRefunds {
        tinyint     StatusID   PK
        varchar50   StatusName UK
        nvarchar255 Description    "nullable"
        datetime2   CreatedAt
        datetime2   UpdatedAt      "nullable"
    }

    OrderRefunds {
        int         RefundID          PK
        int         OrderID           FK
        int         CustomerID        FK
        tinyint     RefundReasonID    FK  "nullable"
        int         RequestedBy       FK  "nullable"
        int         ApprovedBy        FK  "nullable"
        int         WalletTransactionID FK "nullable"
        varchar30   RefundCode        UK
        varchar50   ShippingOrderCode "nullable — GHN reverse shipping code"
        nvarchar500 ReasonDetails     "nullable"
        decimal18_2 SubTotal          "nullable"
        decimal18_2 ShippingFee
        decimal18_2 TotalAmount       "nullable"
        decimal18_2 ApprovedAmount
        tinyint     StatusID          FK
        nvarchar1000 AdminNote        "nullable"
        datetime2   ApprovedAt        "nullable"
        datetime2   RejectedAt        "nullable"
        datetime2   CompletedAt       "nullable"
        datetime2   CancelledAt       "nullable"
        bit         IsDeleted
        datetime2   CreatedAt
        datetime2   UpdatedAt         "nullable"
    }

    RefundDetails {
        int         RefundDetailID  PK
        int         RefundID        FK
        int         ProductID       FK
        smallint    Quantity        "CHECK > 0"
        decimal18_2 UnitPrice
        decimal18_2 RefundAmount
        datetime2   CreatedAt
    }

    RefundStatusHistory {
        int       HistoryID       PK
        int       RefundID        FK
        tinyint   StatusID        FK
        int       ChangedBy       FK  "nullable"
        nvarchar500 Note          "nullable"
        datetime2 CreatedAt
    }

    %% ════════════════════════════════
    %% 9. NOTIFICATION & CHAT
    %% ════════════════════════════════

    NotificationTemplates {
        smallint    TemplateID   PK
        varchar100  TemplateCode UK
        nvarchar255 TitleTemplate
        nvarchar1000 MessageTemplate
        bit         IsActive
        datetime2   CreatedAt
        datetime2   UpdatedAt    "nullable"
    }

    NotificationDeliveries {
        bigint       DeliveryID      PK
        int          AccountID       FK
        int          CreatedByJobID  FK   "nullable"
        int          CampaignID      FK   "nullable"
        varchar100   TemplateCode    FK   "nullable"
        varchar15    RecipientType        "CUSTOMER|ADMIN|STAFF"
        varchar20    Channel              "WEB_BELL|EMAIL|WEB_PUSH"
        varchar20    NotificationType     "ORDER|PROMOTION|SYSTEM|BLOG|STOCK"
        nvarchar255  Title
        nvarchar500  Message
        nvarchar2000 Payload              "JSON"
        varchar10    Status               "Unread|Read|Archived"
        varchar15    EmailStatus          "Pending|Sent|Failed — nullable"
        varchar200   IdempotencyKey   UK  "nullable"
        datetime2    CreatedAt
        datetime2    UpdatedAt            "nullable"
    }

    ChatConversations {
        int      ConversationID PK
        int      AccountID      FK  "nullable — guest có SessionID"
        varchar100 SessionID        "nullable"
        varchar15 Status            "BotActive|..."
        datetime2 CreatedAt
    }

    ChatMessages {
        int         MessageID      PK
        int         ConversationID FK
        varchar10   SenderType         "User|Bot|Staff"
        nvarcharMAX Content
        datetime2   CreatedAt
    }

    %% ════════════════════════════════
    %% RELATIONSHIPS
    %% ════════════════════════════════

    Accounts             }o--||  Roles                : "có vai trò (RoleID)"
    UserBlockHistory     }o--||  Accounts             : "bị khóa (AccountID)"
    UserBlockHistory     }o--o|  Accounts             : "khóa bởi (BlockedBy)"
    UserBlockHistory     }o--o|  Accounts             : "mở khóa bởi (UnblockedBy)"
    UserBlockHistory     }o--||  BlockReasons          : "lý do (ReasonID)"
    Addresses            }o--||  Accounts             : "địa chỉ của (AccountID)"

    Categories           }o--||  SuperCategories      : "thuộc danh mục lớn"
    Products             }o--||  Categories           : "thuộc danh mục"
    Products             }o--o|  Brands               : "thương hiệu (nullable)"
    Products             }o--o|  PriceRanges          : "khoảng giá (nullable)"
    Products             }o--o|  Promotions           : "khuyến mãi (nullable)"
    ProductDetails       ||--||  Products             : "1-1 chi tiết sản phẩm"
    ProductDetails       }o--o|  Materials            : "chất liệu (nullable)"
    ProductDetails       }o--o|  Ages                 : "độ tuổi (nullable)"
    ProductDetails       }o--o|  Sexes                : "giới tính (nullable)"
    ProductDetails       }o--o|  Origins              : "xuất xứ (nullable)"
    ProductImages        }o--||  Products             : "ảnh của sản phẩm"
    Promotions           }o--||  Accounts             : "tạo bởi (CreatedBy)"

    Orders               }o--||  Accounts             : "đặt bởi (AccountID)"
    Orders               }o--||  StatusOrders         : "trạng thái (StatusID)"
    Orders               }o--o|  Accounts             : "staff phụ trách (AssignedToStaffID)"
    Orders               }o--o|  Accounts             : "huỷ bởi (CancelledBy)"
    OrderDetails         }o--||  Orders               : "thuộc đơn hàng"
    OrderDetails         }o--||  Products             : "sản phẩm trong đơn"
    OrderStatusHistory   }o--||  Orders               : "lịch sử trạng thái"
    OrderStatusHistory   }o--||  StatusOrders         : "trạng thái mới"
    OrderStatusHistory   }o--o|  Accounts             : "thay đổi bởi (ChangedBy)"
    WorkSchedules         }o--||  Accounts             : "nhân viên (AccountID)"
    WorkSchedules         }o--||  Accounts             : "tạo bởi (CreatedBy)"
    WorkSchedules         }o--||  ShiftTemplates       : "mẫu ca"
    StaffShiftCapacity    ||--||  WorkSchedules        : "1-1 capacity"
    StaffShiftCapacity    }o--||  Accounts             : "tải của nhân viên"
    OrderAssignments      }o--||  Orders               : "phân công đơn"
    OrderAssignments      }o--||  WorkSchedules        : "ca làm việc"
    OrderAssignments      }o--||  Accounts             : "nhân viên"
    OrderAssignments      }o--||  Roles                : "vai trò"
    OrderAssignments      }o--o|  Accounts             : "gán bởi (AssignedBy)"
    OrderQueue            }o--||  Orders               : "đơn vào hàng chờ"
    OrderQueue            }o--o|  Accounts             : "xử lý bởi (AssignedBy)"

    Cart                 ||--||  Accounts             : "giỏ hàng của user (1-1)"
    CartItems            }o--||  Cart                 : "item trong giỏ"
    CartItems            }o--||  Products             : "sản phẩm"
    Wishlists            }o--||  Accounts             : "yêu thích của user"
    Wishlists            }o--||  Products             : "sản phẩm yêu thích"

    Vouchers             }o--||  VoucherTypes         : "loại voucher"
    Vouchers             }o--o|  Accounts             : "tạo bởi (nullable)"
    OrderVouchers        }o--||  Orders               : "áp dụng cho đơn"
    OrderVouchers        }o--||  Vouchers             : "voucher được dùng"
    VoucherUsageLogs     }o--||  Vouchers             : "lịch sử dùng"
    VoucherUsageLogs     }o--||  Accounts             : "user dùng"
    VoucherUsageLogs     }o--||  Orders               : "đơn hàng"

    BlogPosts            }o--||  Accounts             : "viết bởi (AccountID)"
    BlogPosts            }o--o|  Accounts             : "duyệt bởi (ApprovedBy)"
    BlogPostCategories   }o--||  BlogPosts            : "bài viết"
    BlogPostCategories   }o--||  BlogCategories       : "danh mục blog"
    Banners              }o--||  Accounts             : "tạo bởi (CreatedBy)"

    ReviewProducts       }o--||  Accounts             : "review bởi user"
    ReviewProducts       }o--||  Products             : "review sản phẩm"
    ReviewProducts       }o--||  Orders               : "trong đơn hàng đã mua"
    ReviewProductImages  }o--||  ReviewProducts       : "ảnh của review"
    ReviewProductReplies }o--||  ReviewProducts       : "phản hồi review"
    ReviewProductReplies }o--||  Accounts             : "phản hồi bởi"
    ReviewProductReactions}o--|| ReviewProducts       : "reaction cho review"
    ReviewProductReactions}o--|| Accounts             : "reaction bởi user"

    Wallets              ||--||  Accounts             : "ví của user (1-1)"
    WalletTransactions   }o--||  Wallets              : "giao dịch ví"
    WalletTransactions   }o--||  Accounts             : "của user"
    WalletTransactions   }o--o|  Orders               : "liên quan đơn (nullable)"
    PaymentHistory       }o--||  Orders               : "lịch sử thanh toán"
    PaymentHistory       }o--||  Accounts             : "của user"
    PaymentHistory       }o--o|  WalletTransactions   : "giao dịch ví (nullable)"
    OrderRefunds         }o--||  Orders               : "yêu cầu cho đơn (OrderID)"
    OrderRefunds         }o--||  Accounts             : "yêu cầu bởi khách hàng (CustomerID)"
    OrderRefunds         }o--||  StatusRefunds        : "trạng thái (StatusID)"
    OrderRefunds         }o--o|  WalletTransactions   : "giao dịch hoàn tiền"
    RefundDetails        }o--||  OrderRefunds         : "thuộc yêu cầu hoàn tiền"
    RefundDetails        }o--||  Products             : "sản phẩm hoàn trả"
    RefundStatusHistory  }o--||  OrderRefunds         : "lịch sử trạng thái"
    RefundStatusHistory  }o--||  StatusRefunds        : "trạng thái mới"
    RefundStatusHistory  }o--o|  Accounts             : "thay đổi bởi (ChangedBy)"

    SavedBankAccounts    }o--||  Accounts             : "tài khoản ngân hàng của (AccountID)"
    WithdrawalRequests   }o--||  Wallets              : "rút từ ví (WalletID)"
    WithdrawalRequests   }o--||  Accounts             : "yêu cầu bởi (AccountID)"
    WithdrawalRequests   }o--o|  WalletTransactions   : "giao dịch ví liên quan (WalletTransactionID)"

    NotificationDeliveries}o--|| Accounts            : "gửi cho user"
    NotificationDeliveries}o--|| NotificationTemplates : "dùng template"
    ChatMessages         }o--||  ChatConversations    : "tin nhắn trong hội thoại"
    ChatConversations    }o--o|  Accounts             : "user (nullable — guest)"
```

---

## Tổng quan bảng theo module

| Module                 | Bảng                                                                                                                                                           |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 👤 User Management     | `Roles`, `Accounts`, `BlockReasons`, `UserBlockHistory`, `Addresses`, `SavedBankAccounts`                                                                                           |
| 🧸 Product Catalog     | `SuperCategories`, `Categories`, `Brands`, `Materials`, `Ages`, `Sexes`, `Origins`, `PriceRanges`, `Products`, `ProductDetails`, `ProductImages`, `Promotions` |
| 📦 Order Management    | `StatusOrders`, `Orders`, `OrderDetails`, `OrderStatusHistory`, `OrderAssignments`, `OrderQueue`                                                               |
| 🕒 Shift Scheduling    | `ShiftTemplates`, `WorkSchedules`, `StaffShiftCapacity`                                                                                                        |
| 🛒 Shopping            | `Cart`, `CartItems`, `Wishlists`                                                                                                                               |
| 🏷️ Vouchers            | `VoucherTypes`, `Vouchers`, `OrderVouchers`, `VoucherUsageLogs`                                                                                                |
| 📝 Blog & Content      | `BlogCategories`, `BlogPosts`, `BlogPostCategories`, `Banners`                                                                                                 |
| ⭐ Reviews             | `ReviewProducts`, `ReviewProductImages`, `ReviewProductReplies`, `ReviewProductReactions`                                                                      |
| 💳 Payment & Wallet    | `Wallets`, `WalletTransactions`, `PaymentHistory`, `StatusRefunds`, `OrderRefunds`, `RefundDetails`, `RefundStatusHistory`, `WithdrawalRequests` |
| 🔔 Notification & Chat | `Notification.Templates`, `Notification.Deliveries`, `ChatConversations`, `ChatMessages`                                                                       |
| 🤖 AI/System           | `Interaction.Events`, `Recommendation.ItemSimilarities`, `System.DomainEventOutbox`, `System.BackgroundJobs`                                                   |

---

## Các Trigger quan trọng

| Trigger                                    | Bảng               | Tác dụng                                                 |
| ------------------------------------------ | ------------------ | -------------------------------------------------------- |
| `TR_OrderDetails_SyncSubTotal`             | `OrderDetails`     | Auto-sync `Orders.SubTotal` khi thêm/sửa/xóa item        |
| `TR_OrderVouchers_SyncDiscount`            | `OrderVouchers`    | Auto-sync `Orders.VoucherDiscountAmount`                 |
| `TR_Orders_CalculateTotalAmount`           | `Orders`           | Auto-calc `TotalAmount = SubTotal - Discount + Shipping` |
| `TR_Products_SyncStatusWithQuantity`       | `Products`         | Auto Active↔OutOfStock khi Quantity thay đổi             |
| `TR_Promotions_SyncStatusWithDates`        | `Promotions`       | Auto Scheduled→Active→Expired theo ngày                  |
| `TR_Vouchers_SyncStatusWithDates`          | `Vouchers`         | Auto sync Status + Quantity                              |
| `TR_VoucherUsageLogs_DecreaseQuantity`     | `VoucherUsageLogs` | Auto giảm `Vouchers.Quantity` khi dùng                   |
| `TR_ReviewProducts_ValidateProductInOrder` | `ReviewProducts`   | Chỉ review sản phẩm đã mua & đã giao                     |
| `TR_VoucherUsageLogs_ValidateMaxUsage`     | `VoucherUsageLogs` | Giới hạn `MaxUsagePerUser`                               |
| `TR_WorkSchedules_CreateCapacity`          | `WorkSchedules`    | Auto tạo `StaffShiftCapacity` khi tạo ca                 |

---

## Ghi chú Delete Behavior (các quan hệ chính)

| FK                              | Delete Behavior | Ghi chú                             |
| ------------------------------- | --------------- | ----------------------------------- |
| `Products.CategoryID`           | NO ACTION       | Không xoá Category còn Product      |
| `Orders.AccountID`              | NO ACTION       | Không xoá Account còn Order         |
| `OrderDetails.OrderID`          | NO ACTION       | OrderDetails không cascade          |
| `OrderDetails.ProductID`        | NO ACTION       | Không xoá Product trong OrderDetail |
| `CartItems.CartID/ProductID`    | NO ACTION       |                                     |
| `Wishlists.AccountID/ProductID` | NO ACTION       |                                     |
| `ReviewProducts.OrderID`        | NO ACTION       |                                     |
