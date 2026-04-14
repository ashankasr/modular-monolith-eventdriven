SELECT * FROM [inventory].[StockReservations]
SELECT * FROM [inventory].[StockReservationItems]
SELECT * FROM [inventory].[Products]

SELECT * FROM [orders].[OrderItems]
SELECT * FROM [orders].[Orders] order by [CreatedAt]

SELECT * FROM [orders].[OrderSagaState]

SELECT * FROM [orders].[OutboxMessages]
SELECT * FROM [payments].[OutboxMessages]
SELECT * FROM [inventory].[OutboxMessages]

select * from [payments].[Payments]

select * from [notifications].[NotificationLogs]


/*
delete [notifications].[NotificationLogs]
delete [orders].[OutboxMessages]
delete [payments].[OutboxMessages]
delete [notifications].[OutboxMessages]
delete [inventory].[OutboxMessages]

delete [orders].[OrderSagaState]
delete [orders].[OrderItems]
delete [orders].[Orders]

delete [payments].[Payments]

delete [inventory].[StockReservationItems]
delete [inventory].[StockReservations]

*/