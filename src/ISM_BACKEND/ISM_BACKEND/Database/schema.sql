IF OBJECT_ID('[ISM].[Stock]', 'U') IS NOT NULL DROP TABLE [ISM].[Stock];
IF OBJECT_ID('[ISM].[OutboundOrderItem]', 'U') IS NOT NULL DROP TABLE [ISM].[OutboundOrderItem];
IF OBJECT_ID('[ISM].[OutboundOrder]', 'U') IS NOT NULL DROP TABLE [ISM].[OutboundOrder];
IF OBJECT_ID('[ISM].[InboundOrderItem]', 'U') IS NOT NULL DROP TABLE [ISM].[InboundOrderItem];
IF OBJECT_ID('[ISM].[InboundOrder]', 'U') IS NOT NULL DROP TABLE [ISM].[InboundOrder];
IF OBJECT_ID('[ISM].[Product]', 'U') IS NOT NULL DROP TABLE [ISM].[Product];
IF OBJECT_ID('[ISM].[ProductUnit]', 'U') IS NOT NULL DROP TABLE [ISM].[ProductUnit];
IF OBJECT_ID('[ISM].[RefreshToken]', 'U') IS NOT NULL DROP TABLE [ISM].[RefreshToken];
IF OBJECT_ID('[ISM].[Users]', 'U') IS NOT NULL DROP TABLE [ISM].[Users];
IF OBJECT_ID('[ISM].[Warehouse]', 'U') IS NOT NULL DROP TABLE [ISM].[Warehouse];

IF SCHEMA_ID('ISM') IS NOT NULL DROP SCHEMA [ISM];
GO

CREATE SCHEMA [ISM];
GO

-- ============================================================
-- 一、Organization 對應：倉庫 / 使用者 / RefreshToken
-- ============================================================

CREATE TABLE [ISM].[Warehouse]
(
    WarehouseId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL UNIQUE,
    CreateTime  DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);

-- Role: 1=Admin, 2=WarehouseAdmin, 3=WarehouseUser
CREATE TABLE [ISM].[Users]
(
    UserId       BIGINT IDENTITY(1,1) PRIMARY KEY,
    WarehouseId  BIGINT        NULL, -- FK -> Warehouse.WarehouseId(Admin 為 NULL)
    Name         NVARCHAR(100) NOT NULL,
    Username     NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role         INT           NOT NULL,
    CreateTime   DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Users_WarehouseId ON [ISM].[Users](WarehouseId);

CREATE TABLE [ISM].[RefreshToken]
(
    RefreshTokenId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    Token            NVARCHAR(200) NOT NULL UNIQUE,
    ReplacedByToken  NVARCHAR(200) NULL,
    UserId           BIGINT        NOT NULL, -- FK -> Users.UserId
    CreateTime       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    ExpireTime       DATETIME2     NOT NULL,
    RevokeTime       DATETIME2     NULL
);
CREATE INDEX IX_RefreshToken_UserId ON [ISM].[RefreshToken](UserId);

-- ============================================================
-- 二、Ordering 對應：商品 / 入庫單 / 出庫單
-- ============================================================

CREATE TABLE [ISM].[ProductUnit]
(
    Name NVARCHAR(50) PRIMARY KEY
);

CREATE TABLE [ISM].[Product]
(
    ProductId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductNo  NVARCHAR(50)  NOT NULL UNIQUE,
    Name       NVARCHAR(200) NOT NULL,
    Unit       NVARCHAR(50)  NOT NULL, -- FK -> ProductUnit.Name
    Price      DECIMAL(18,2) NOT NULL
);

-- Status: 1=Pending, 2=Confirmed, 3=Rejected
CREATE TABLE [ISM].[InboundOrder]
(
    InboundOrderId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderNo          NVARCHAR(50)  NOT NULL UNIQUE,
    WarehouseId      BIGINT        NOT NULL, -- FK -> Warehouse.WarehouseId
    Status           INT           NOT NULL,
    RejectReason     NVARCHAR(500) NULL,
    RequestedBy      BIGINT        NOT NULL, -- FK -> Users.UserId
    RequestedByName  NVARCHAR(100) NOT NULL,
    RequestedAt      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    ConfirmedBy      BIGINT        NULL,
    ConfirmedByName  NVARCHAR(100) NULL,
    ConfirmedAt      DATETIME2     NULL
);
CREATE INDEX IX_InboundOrder_WarehouseId ON [ISM].[InboundOrder](WarehouseId);

CREATE TABLE [ISM].[InboundOrderItem]
(
    InboundOrderItemId BIGINT IDENTITY(1,1) PRIMARY KEY,
    InboundOrderId      BIGINT        NOT NULL, -- FK -> InboundOrder.InboundOrderId
    ProductId           BIGINT        NOT NULL, -- FK -> Product.ProductId
    Quantity             INT          NOT NULL,
    UnitPrice           DECIMAL(18,2) NOT NULL
);
CREATE INDEX IX_InboundOrderItem_InboundOrderId ON [ISM].[InboundOrderItem](InboundOrderId);

CREATE TABLE [ISM].[OutboundOrder]
(
    OutboundOrderId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrderNo          NVARCHAR(50)  NOT NULL UNIQUE,
    WarehouseId      BIGINT        NOT NULL, -- FK -> Warehouse.WarehouseId
    Status           INT           NOT NULL,
    RejectReason     NVARCHAR(500) NULL,
    RequestedBy      BIGINT        NOT NULL, -- FK -> Users.UserId
    RequestedByName  NVARCHAR(100) NOT NULL,
    RequestedAt      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    ConfirmedBy      BIGINT        NULL,
    ConfirmedByName  NVARCHAR(100) NULL,
    ConfirmedAt      DATETIME2     NULL
);
CREATE INDEX IX_OutboundOrder_WarehouseId ON [ISM].[OutboundOrder](WarehouseId);

CREATE TABLE [ISM].[OutboundOrderItem]
(
    OutboundOrderItemId BIGINT IDENTITY(1,1) PRIMARY KEY,
    OutboundOrderId      BIGINT NOT NULL, -- FK -> OutboundOrder.OutboundOrderId
    ProductId            BIGINT NOT NULL, -- FK -> Product.ProductId
    Quantity             INT    NOT NULL
);
CREATE INDEX IX_OutboundOrderItem_OutboundOrderId ON [ISM].[OutboundOrderItem](OutboundOrderId);

-- ============================================================
-- 三、Inventory 對應：庫存
-- ============================================================

CREATE TABLE [ISM].[Stock]
(
    StockId            BIGINT IDENTITY(1,1) PRIMARY KEY,
    ProductId          BIGINT NOT NULL, -- FK -> Product.ProductId
    WarehouseId        BIGINT NOT NULL, -- FK -> Warehouse.WarehouseId
    Quantity           INT    NOT NULL DEFAULT 0,
    CumulativeShipped  INT    NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Stock_Product_Warehouse UNIQUE (ProductId, WarehouseId)
);

-- ============================================================
-- 附錄：狀態碼 INT <-> 字串對應
-- ============================================================
-- InboundOrderStatus / OutboundOrderStatus
--   1 = Pending
--   2 = Confirmed
--   3 = Rejected
--
-- Users.Role
--   1 = Admin
--   2 = WarehouseAdmin
--   3 = WarehouseUser
