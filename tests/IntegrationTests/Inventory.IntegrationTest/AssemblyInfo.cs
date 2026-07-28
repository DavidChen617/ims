// 每個 CustomWebApplicationFactory 在 InitializeAsync 裡把 ConnectionStrings__DefaultConnection
// (Kafka 版的還多一個 Kafka__BootstrapServers)設成 process 全域的環境變數 —— Program.cs 必須在
// host 建置前讀到它,而目前沒有能即時生效的 per-instance 覆寫機制(見各 factory 自己的註解)。
// 如果 xUnit 平行執行多個測試類別的 factory,它們的 InitializeAsync 會搶同一組共用環境變數,
// 導致某個 host 最後連到*另一個*類別的容器。關掉 collection 平行化,讓整個 assembly 的 factory
// 初始化依序執行,是這個限制下唯一可靠的解法。(這個問題在 Organization 對應的專案裡真的爆過一次
// ——因為那邊的 DataSeeder 會插入一筆固定 key 的資料,跨容器混用時直接撞 unique constraint 拋
// 例外,而不是默默地破壞隔離性。)
[assembly: CollectionBehavior(DisableTestParallelization = true)]
