SELECT * FROM [inventory].[StockReservations]
SELECT * FROM [inventory].[StockReservationItems]
SELECT * FROM [inventory].[Products]

SELECT * FROM [orders].[OrderItems]
SELECT * FROM [orders].[Orders]

SELECT * FROM [orders].[OrderSagaState]

SELECT * FROM [orders].[OutboxMessages]
SELECT * FROM [payments].[OutboxMessages]
SELECT * FROM [notifications].[OutboxMessages]
SELECT * FROM [inventory].[OutboxMessages]

select * from [payments].[Payments]

select * from [notifications].[NotificationLogs]