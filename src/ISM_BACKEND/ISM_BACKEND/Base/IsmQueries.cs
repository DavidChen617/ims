namespace ISM_BACKEND.Base;

// 全部 SQL 字串集中在這個靜態類，Service 端不自己拼 SQL。
public static class IsmQueries
{
    // ============================================================
    // Users / Auth
    // ============================================================

    public const string FindUserByUsername = @"
SELECT UserId, WarehouseId, Name, Username, PasswordHash, Role, CreateTime
FROM [ISM].Users
WHERE Username = @Username;";

    public const string FindUserById = @"
SELECT UserId, WarehouseId, Name, Username, PasswordHash, Role, CreateTime
FROM [ISM].Users
WHERE UserId = @UserId;";

    public const string CountUserByUsername = @"
SELECT COUNT(1) FROM [ISM].Users WHERE Username = @Username;";

    public const string InsertUser = @"
INSERT INTO [ISM].Users (WarehouseId, Name, Username, PasswordHash, Role, CreateTime)
VALUES (@WarehouseId, @Name, @Username, @PasswordHash, @Role, SYSDATETIME());";

    public const string InsertRefreshToken = @"
INSERT INTO [ISM].RefreshToken (Token, UserId, CreateTime, ExpireTime)
VALUES (@Token, @UserId, SYSDATETIME(), @ExpireTime);";

    public const string FindRefreshTokenByToken = @"
SELECT RefreshTokenId, Token, ReplacedByToken, UserId, CreateTime, ExpireTime, RevokeTime
FROM [ISM].RefreshToken
WHERE Token = @Token;";

    public const string RevokeRefreshToken = @"
UPDATE [ISM].RefreshToken SET RevokeTime = SYSDATETIME() WHERE RefreshTokenId = @RefreshTokenId;";

    public const string ReplaceRefreshToken = @"
UPDATE [ISM].RefreshToken SET RevokeTime = SYSDATETIME(), ReplacedByToken = @NewToken WHERE RefreshTokenId = @RefreshTokenId;";

    // ============================================================
    // Warehouse
    // ============================================================

    public const string CountWarehouseByName = @"
SELECT COUNT(1) FROM [ISM].Warehouse WHERE Name = @Name;";

    public const string InsertWarehouse = @"
INSERT INTO [ISM].Warehouse (Name, CreateTime) VALUES (@Name, SYSDATETIME());";

    public const string ListWarehouses = @"
SELECT w.WarehouseId, w.Name,
       (SELECT TOP 1 u.Name FROM [ISM].Users u WHERE u.WarehouseId = w.WarehouseId AND u.Role = 2) AS WarehouseAdminName,
       (SELECT COUNT(1) FROM [ISM].Users u WHERE u.WarehouseId = w.WarehouseId) AS StaffCount
FROM [ISM].Warehouse w
WHERE (@Name IS NULL OR w.Name LIKE '%' + @Name + '%')
ORDER BY w.WarehouseId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountWarehouses = @"
SELECT COUNT(1) FROM [ISM].Warehouse w WHERE (@Name IS NULL OR w.Name LIKE '%' + @Name + '%');";

    public const string FindWarehouseById = @"
SELECT WarehouseId, Name FROM [ISM].Warehouse WHERE WarehouseId = @WarehouseId;";

    public const string ListWarehouseStaff = @"
SELECT UserId, Name, Role FROM [ISM].Users WHERE WarehouseId = @WarehouseId ORDER BY UserId;";

    // ============================================================
    // Users
    // ============================================================

    public const string ListUsers = @"
SELECT u.UserId, u.Name, u.Username, u.Role, u.WarehouseId, w.Name AS WarehouseName, u.CreateTime
FROM [ISM].Users u
LEFT JOIN [ISM].Warehouse w ON u.WarehouseId = w.WarehouseId
WHERE (@Name IS NULL OR u.Name LIKE '%' + @Name + '%')
  AND (@Username IS NULL OR u.Username LIKE '%' + @Username + '%')
  AND (@Role IS NULL OR u.Role = @Role)
  AND (@WarehouseId IS NULL OR u.WarehouseId = @WarehouseId)
ORDER BY u.UserId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountUsers = @"
SELECT COUNT(1)
FROM [ISM].Users u
WHERE (@Name IS NULL OR u.Name LIKE '%' + @Name + '%')
  AND (@Username IS NULL OR u.Username LIKE '%' + @Username + '%')
  AND (@Role IS NULL OR u.Role = @Role)
  AND (@WarehouseId IS NULL OR u.WarehouseId = @WarehouseId);";

    // ============================================================
    // Products
    // ============================================================

    public const string CountProductUnitByName = @"
SELECT COUNT(1) FROM [ISM].ProductUnit WHERE Name = @Name;";

    public const string InsertProductUnit = @"
INSERT INTO [ISM].ProductUnit (Name) VALUES (@Name);";

    public const string CountProductByUnit = @"
SELECT COUNT(1) FROM [ISM].Product WHERE Unit = @Name;";

    public const string DeleteProductUnit = @"
DELETE FROM [ISM].ProductUnit WHERE Name = @Name;";

    public const string ListProductUnits = @"
SELECT Name FROM [ISM].ProductUnit ORDER BY Name;";

    public const string CountProductByNo = @"
SELECT COUNT(1) FROM [ISM].Product WHERE ProductNo = @ProductNo;";

    public const string FindProductUnitByName = @"
SELECT Name FROM [ISM].ProductUnit WHERE Name = @Name;";

    public const string InsertProduct = @"
INSERT INTO [ISM].Product (ProductNo, Name, Unit, Price) VALUES (@ProductNo, @Name, @Unit, @Price);";

    public const string ListProducts = @"
SELECT ProductId, ProductNo, Name, Unit, Price
FROM [ISM].Product
WHERE (@ProductNo IS NULL OR ProductNo LIKE '%' + @ProductNo + '%')
  AND (@Name IS NULL OR Name LIKE '%' + @Name + '%')
  AND (@Unit IS NULL OR Unit = @Unit)
ORDER BY ProductId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountProducts = @"
SELECT COUNT(1)
FROM [ISM].Product
WHERE (@ProductNo IS NULL OR ProductNo LIKE '%' + @ProductNo + '%')
  AND (@Name IS NULL OR Name LIKE '%' + @Name + '%')
  AND (@Unit IS NULL OR Unit = @Unit);";

    public const string FindProductById = @"
SELECT ProductId, ProductNo, Name, Unit, Price FROM [ISM].Product WHERE ProductId = @ProductId;";

    public const string FindProductsByIds = @"
SELECT ProductId, ProductNo, Name, Unit, Price FROM [ISM].Product WHERE ProductId IN @ProductIds;";

    // ============================================================
    // Stock(InboundOrderService / OutboundOrderService / StockService 共用 SQL 文字,
    //       但各自複製一份存取邏輯,Service 彼此不互相注入)
    // ============================================================

    public const string FindStockByProductWarehouse = @"
SELECT StockId, ProductId, WarehouseId, Quantity, CumulativeShipped
FROM [ISM].Stock WHERE ProductId = @ProductId AND WarehouseId = @WarehouseId;";

    public const string InsertStock = @"
INSERT INTO [ISM].Stock (ProductId, WarehouseId, Quantity, CumulativeShipped) VALUES (@ProductId, @WarehouseId, 0, 0);";

    public const string UpdateStockQuantity = @"
UPDATE [ISM].Stock SET Quantity = @Quantity, CumulativeShipped = @CumulativeShipped WHERE StockId = @StockId;";

    public const string ListStocks = @"
SELECT s.ProductId, p.ProductNo, p.Name AS ProductName, p.Unit, s.WarehouseId, w.Name AS WarehouseName, s.Quantity, s.CumulativeShipped
FROM [ISM].Stock s
JOIN [ISM].Product p ON s.ProductId = p.ProductId
JOIN [ISM].Warehouse w ON s.WarehouseId = w.WarehouseId
WHERE (@WarehouseId IS NULL OR s.WarehouseId = @WarehouseId)
  AND (@ProductId IS NULL OR s.ProductId = @ProductId)
ORDER BY s.StockId
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountStocks = @"
SELECT COUNT(1)
FROM [ISM].Stock s
WHERE (@WarehouseId IS NULL OR s.WarehouseId = @WarehouseId)
  AND (@ProductId IS NULL OR s.ProductId = @ProductId);";

    // ============================================================
    // InboundOrder
    // ============================================================

    public const string CountInboundOrderByOrderNo = @"
SELECT COUNT(1) FROM [ISM].InboundOrder WHERE OrderNo = @OrderNo;";

    public const string InsertInboundOrder = @"
INSERT INTO [ISM].InboundOrder (OrderNo, WarehouseId, Status, RequestedBy, RequestedByName, RequestedAt)
VALUES (@OrderNo, @WarehouseId, @Status, @RequestedBy, @RequestedByName, SYSDATETIME());";

    public const string InsertInboundOrderItem = @"
INSERT INTO [ISM].InboundOrderItem (InboundOrderId, ProductId, Quantity, UnitPrice)
VALUES (@InboundOrderId, @ProductId, @Quantity, @UnitPrice);";

    public const string FindInboundOrderById = @"
SELECT InboundOrderId, OrderNo, WarehouseId, Status, RejectReason, RequestedBy, RequestedByName, RequestedAt, ConfirmedBy, ConfirmedByName, ConfirmedAt
FROM [ISM].InboundOrder WHERE InboundOrderId = @InboundOrderId;";

    public const string ListInboundOrderItemsByOrderId = @"
SELECT i.ProductId, p.ProductNo, p.Name AS ProductName, p.Unit, i.Quantity, i.UnitPrice
FROM [ISM].InboundOrderItem i
JOIN [ISM].Product p ON i.ProductId = p.ProductId
WHERE i.InboundOrderId = @InboundOrderId;";

    public const string UpdateInboundOrderConfirm = @"
UPDATE [ISM].InboundOrder SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedByName = @ConfirmedByName, ConfirmedAt = SYSDATETIME()
WHERE InboundOrderId = @InboundOrderId AND Status = @PendingStatus;";

    public const string UpdateInboundOrderReject = @"
UPDATE [ISM].InboundOrder SET Status = @Status, RejectReason = @RejectReason, ConfirmedBy = @ConfirmedBy, ConfirmedByName = @ConfirmedByName, ConfirmedAt = SYSDATETIME()
WHERE InboundOrderId = @InboundOrderId AND Status = @PendingStatus;";

    public const string ListInboundOrders = @"
SELECT InboundOrderId, OrderNo, WarehouseId, Status, RequestedAt, ConfirmedAt
FROM [ISM].InboundOrder
WHERE (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%')
ORDER BY InboundOrderId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountInboundOrders = @"
SELECT COUNT(1) FROM [ISM].InboundOrder
WHERE (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%');";

    // ============================================================
    // OutboundOrder
    // ============================================================

    public const string CountOutboundOrderByOrderNo = @"
SELECT COUNT(1) FROM [ISM].OutboundOrder WHERE OrderNo = @OrderNo;";

    public const string InsertOutboundOrder = @"
INSERT INTO [ISM].OutboundOrder (OrderNo, WarehouseId, Status, RejectReason, RequestedBy, RequestedByName, RequestedAt)
VALUES (@OrderNo, @WarehouseId, @Status, @RejectReason, @RequestedBy, @RequestedByName, SYSDATETIME());";

    public const string InsertOutboundOrderItem = @"
INSERT INTO [ISM].OutboundOrderItem (OutboundOrderId, ProductId, Quantity)
VALUES (@OutboundOrderId, @ProductId, @Quantity);";

    public const string FindOutboundOrderById = @"
SELECT OutboundOrderId, OrderNo, WarehouseId, Status, RejectReason, RequestedBy, RequestedByName, RequestedAt, ConfirmedBy, ConfirmedByName, ConfirmedAt
FROM [ISM].OutboundOrder WHERE OutboundOrderId = @OutboundOrderId;";

    public const string ListOutboundOrderItemsByOrderId = @"
SELECT i.ProductId, p.ProductNo, p.Name AS ProductName, p.Unit, i.Quantity
FROM [ISM].OutboundOrderItem i
JOIN [ISM].Product p ON i.ProductId = p.ProductId
WHERE i.OutboundOrderId = @OutboundOrderId;";

    public const string UpdateOutboundOrderConfirm = @"
UPDATE [ISM].OutboundOrder SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedByName = @ConfirmedByName, ConfirmedAt = SYSDATETIME()
WHERE OutboundOrderId = @OutboundOrderId AND Status = @PendingStatus;";

    public const string UpdateOutboundOrderReject = @"
UPDATE [ISM].OutboundOrder SET Status = @Status, RejectReason = @RejectReason, ConfirmedBy = @ConfirmedBy, ConfirmedByName = @ConfirmedByName, ConfirmedAt = SYSDATETIME()
WHERE OutboundOrderId = @OutboundOrderId AND Status = @PendingStatus;";

    public const string ListOutboundOrders = @"
SELECT OutboundOrderId, OrderNo, WarehouseId, Status, RequestedAt, ConfirmedAt
FROM [ISM].OutboundOrder
WHERE (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%')
ORDER BY OutboundOrderId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public const string CountOutboundOrders = @"
SELECT COUNT(1) FROM [ISM].OutboundOrder
WHERE (@WarehouseId IS NULL OR WarehouseId = @WarehouseId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%');";
}
