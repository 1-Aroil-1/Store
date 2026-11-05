-- DROP SCHEMA dbo;

CREATE SCHEMA dbo;
-- ShopDB.dbo.Clients определение
-- Drop table
-- DROP TABLE ShopDB.dbo.Clients;

CREATE TABLE ShopDB.dbo.Clients (
	Id int IDENTITY(1, 1) NOT NULL,
	FullName nvarchar(150) COLLATE Cyrillic_General_CI_AS NOT NULL,
	Email nvarchar(100) COLLATE Cyrillic_General_CI_AS NOT NULL,
	Password nvarchar(255) COLLATE Cyrillic_General_CI_AS NOT NULL,
	PhoneNumber nvarchar(20) COLLATE Cyrillic_General_CI_AS NULL,
	[Role] int DEFAULT 2 NOT NULL,
	CONSTRAINT PK_Clients PRIMARY KEY (Id),
	CONSTRAINT UQ_Clients_Email UNIQUE (Email)
);

ALTER TABLE ShopDB.dbo.Clients WITH NOCHECK ADD CONSTRAINT CHK_Clients_Role CHECK (([Role] =(2)
OR [Role] =(1)));
-- ShopDB.dbo.Products определение
-- Drop table
-- DROP TABLE ShopDB.dbo.Products;

CREATE TABLE ShopDB.dbo.Products (
	Id int IDENTITY(1, 1) NOT NULL,
	Name nvarchar(150) COLLATE Cyrillic_General_CI_AS NOT NULL,
	Category nvarchar(100) COLLATE Cyrillic_General_CI_AS NULL,
	Price decimal(18, 2) NOT NULL,
	StockQuantity int DEFAULT 0 NOT NULL,
	CONSTRAINT PK_Products PRIMARY KEY (Id)
);

ALTER TABLE ShopDB.dbo.Products WITH NOCHECK ADD CONSTRAINT CHK_Products_Price CHECK (([Price] >=(0)));

ALTER TABLE ShopDB.dbo.Products WITH NOCHECK ADD CONSTRAINT CHK_Products_Stock CHECK (([StockQuantity] >=(0)));
-- ShopDB.dbo.Orders определение
-- Drop table
-- DROP TABLE ShopDB.dbo.Orders;

CREATE TABLE ShopDB.dbo.Orders (
	Id int IDENTITY(1, 1) NOT NULL,
	ClientId int NOT NULL,
	OrderDate datetime DEFAULT getdate() NOT NULL,
	TotalAmount decimal(18, 2) DEFAULT 0.00 NOT NULL,
	CONSTRAINT PK_Orders PRIMARY KEY (Id),
	CONSTRAINT FK_Orders_Clients FOREIGN KEY (ClientId) REFERENCES ShopDB.dbo.Clients(Id) ON
DELETE
    CASCADE
);

ALTER TABLE ShopDB.dbo.Orders WITH NOCHECK ADD CONSTRAINT CHK_Orders_TotalAmount CHECK (([TotalAmount] >=(0)));
-- ShopDB.dbo.OrderDetails определение
-- Drop table
-- DROP TABLE ShopDB.dbo.OrderDetails;

CREATE TABLE ShopDB.dbo.OrderDetails (
	Id int IDENTITY(1, 1) NOT NULL,
	OrderId int NOT NULL,
	ProductId int NOT NULL,
	Quantity int NOT NULL,
	PricePerUnit decimal(18, 2) NOT NULL,
	CONSTRAINT PK_OrderDetails PRIMARY KEY (Id),
	CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES ShopDB.dbo.Orders(Id) ON
DELETE
    CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES ShopDB.dbo.Products(Id)
);

ALTER TABLE ShopDB.dbo.OrderDetails WITH NOCHECK ADD CONSTRAINT CHK_OrderDetails_Quantity CHECK (([Quantity]>(0)));

ALTER TABLE ShopDB.dbo.OrderDetails WITH NOCHECK ADD CONSTRAINT CHK_OrderDetails_Price CHECK (([PricePerUnit] >=(0)));
