# MES TCP/IP Client Demo

這是一個用於 **MES ↔ 設備通信** 的 TCP/IP 客戶端示範專案。  
具備自動重連、斷線偵測、多執行緒架構、Thread-safe 發送等功能。  

---

## 🔧 專案功能

- ✔ 自動重連（Auto Reconnect）
- ✔ 背景讀取 Thread（Read Loop）
- ✔ Socket 級斷線偵測（FIN / RST）
- ✔ Thread-safe 發送（避免資料衝突）
- ✔ 事件回呼（Connected / Disconnected / MessageReceived）
- ✔ 不依賴任何 DLL、MES、機台特殊格式
- ✔ 可直接用於 MES 通訊 Demo 或作品集展示

---

## 📂 專案結構

```
mes-tcpip-bridge-demo/
 ├── src/
 │    ├── MesTcpClient.cs     # TCP Client 通訊核心
 │    └── Program.cs          # Demo 主程式
 ├── README.md
 ├── LICENSE
 └── .gitignore
```

---

## 🚀 使用方式

以下示範如何使用本專案提供的 `MesTcpClient`：

```csharp
var client = new MesTcpClient("127.0.0.1", 9000);

// 事件回呼
client.OnConnected = () => Console.WriteLine("Connected");
client.OnMessageReceived = msg => Console.WriteLine(msg);
client.OnDisconnected = () => Console.WriteLine("Disconnected");
client.OnReconnected = () => Console.WriteLine("Reconnected");

// 啟動
client.Start();

// 傳送資料
client.Send("HELLO MES");

// 結束
client.Stop();
```

---

## 🧠 技術亮點

- 使用 `Socket.Poll()` + `Available == 0` 檢測真正斷線（FIN / RST）
- 多執行緒架構（ReadLoop + ReconnectLoop）
- Thread-safe 發送（lock 保護 NetworkStream）
- 自動重連機制（自動檢查 → 掉線 → 重連）
- 可回呼事件架構，方便整合到其他設備或 MES 模組
- 通用實作，不依賴任何私有格式或 DLL

---

## 🏭 適用場景

- MES ↔ 設備 TCP 通訊橋接
- 心跳包（Heartbeat）通信
- 設備狀態上報
- 自動重連 TCP 模組
- 工廠自動化、製造業 IPC 通訊

---

## 👤 作者

HungHsiang,Lin (林弘翔)  
Software Engineer — MES / Equipment Communication / Automation