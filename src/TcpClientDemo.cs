using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MesTcpClientDemo
{
    /// <summary>
    /// A robust TCP/IP client designed for MES-equipment integration.
    /// Includes auto-reconnect, background read monitor, socket-level
    /// disconnect detection, and thread-safe send operations.
    /// 
    /// This is a clean, generic version suitable for open-source use,
    /// without any vendor-specific or sensitive logic.
    /// </summary>
    public class MesTcpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        private readonly string _ip;
        private readonly int _port;

        private readonly object _sendLock = new object();
        private readonly object _reconnectLock = new object();

        private Thread _readThread;
        private Thread _reconnectThread;

        private bool _readRunning = false;
        private bool _reconnectRunning = false;

        private readonly int _reconnectIntervalMs = 3000;

        // ================================
        // Public Events (for user callbacks)
        // ================================
        public Action OnConnected;
        public Action OnDisconnected;
        public Action<string> OnMessageReceived;
        public Action OnReconnected;

        public MesTcpClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        // ============================================
        // Main API
        // ============================================
        public void Start()
        {
            Connect();

            StartReadMonitor();
            StartReconnectMonitor();
        }

        public void Stop()
        {
            _readRunning = false;
            _reconnectRunning = false;

            ForceClose();
        }

        // ============================================
        // Connection Logic
        // ============================================
        private void Connect()
        {
            try
            {
                Console.WriteLine($"[INFO] Connecting to {_ip}:{_port} ...");

                _client = new TcpClient();
                _client.Connect(_ip, _port);
                _stream = _client.GetStream();

                Console.WriteLine("[INFO] Connected.");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Connection failed: {ex.Message}");
            }
        }

        // ============================================
        // Read Monitor Thread
        // ============================================
        private void StartReadMonitor()
        {
            if (_readRunning)
                return;

            _readRunning = true;

            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true
            };
            _readThread.Start();

            Console.WriteLine("[INFO] Read monitor started.");
        }

        private void ReadLoop()
        {
            byte[] buffer = new byte[4096];

            while (_readRunning)
            {
                try
                {
                    if (_client == null || _stream == null)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    Socket s = _client.Client;

                    // FIN or RST detection
                    bool readReady = s.Poll(0, SelectMode.SelectRead);
                    bool noData = (s.Available == 0);

                    if (readReady && noData)
                    {
                        byte[] test = new byte[1];
                        int read = 0;
                        try
                        {
                            // non-blocking
                            read = s.Receive(test, 0, 0, SocketFlags.None);
                        }
                        catch
                        {
                            Console.WriteLine("[WARN] Server reset / closed (RST).");
                            HandleDisconnect();
                            continue;
                        }

                        if (read == 0)
                        {
                            Console.WriteLine("[WARN] Server closed the connection (FIN).");
                            HandleDisconnect();
                            continue;
                        }
                    }

                    if (_stream.DataAvailable)
                    {
                        int len = _stream.Read(buffer, 0, buffer.Length);
                        if (len > 0)
                        {
                            string msg = Encoding.UTF8.GetString(buffer, 0, len);
                            OnMessageReceived?.Invoke(msg);
                        }
                    }

                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERR] ReadLoop error: {ex.Message}");
                    HandleDisconnect();
                }
            }
        }

        private void HandleDisconnect()
        {
            ForceClose();
            OnDisconnected?.Invoke();
        }

        // ============================================
        // Auto Reconnect Thread
        // ============================================
        private void StartReconnectMonitor()
        {
            if (_reconnectRunning)
                return;

            _reconnectRunning = true;

            _reconnectThread = new Thread(ReconnectLoop)
            {
                IsBackground = true
            };
            _reconnectThread.Start();

            Console.WriteLine("[INFO] Reconnect monitor started.");
        }

        private void ReconnectLoop()
        {
            while (_reconnectRunning)
            {
                try
                {
                    if (!IsConnected())
                    {
                        Console.WriteLine("[Reconnect] Disconnected... attempting to reconnect.");

                        lock (_reconnectLock)
                        {
                            ForceClose();
                            Connect();

                            if (IsConnected())
                            {
                                Console.WriteLine("[Reconnect] Reconnected!");
                                OnReconnected?.Invoke();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Reconnect] Error: {ex.Message}");
                }

                Thread.Sleep(_reconnectIntervalMs);
            }
        }

        // ============================================
        // Send
        // ============================================
        public bool Send(string text)
        {
            if (_client == null || !_client.Connected)
                return false;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(text);

                lock (_sendLock)
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] Send error: {ex.Message}");
                HandleDisconnect();
                return false;
            }
        }

        // ============================================
        // Connection Status
        // ============================================
        public bool IsConnected()
        {
            try
            {
                if (_client == null || _client.Client == null)
                    return false;

                Socket s = _client.Client;

                if (!s.Connected)
                    return false;

                bool readReady = s.Poll(0, SelectMode.SelectRead);
                bool noData = (s.Available == 0);

                if (readReady && noData)
                {
                    byte[] buf = new byte[1];
                    int read = s.Receive(buf, 0, 0, SocketFlags.None);

                    if (read == 0)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // Force Close
        // ============================================
        private void ForceClose()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            finally
            {
                _stream = null;
                _client = null;
            }
        }
    }
}