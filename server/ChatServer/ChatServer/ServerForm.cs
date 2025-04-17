using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    public partial class ServerForm : Form
    {
        private TcpListener _listener;
        private readonly List<ClientHandler> _clients = new();
        private bool _isRunning;
        private TextBox _txtLog;
        private Button _btnStartStop;

        public ServerForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Chat Server";
            this.Size = new Size(600, 400);
            this.FormClosing += ServerForm_FormClosing;

            _txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            _btnStartStop = new Button
            {
                Text = "Start Server",
                Dock = DockStyle.Bottom
            };
            _btnStartStop.Click += BtnStartStop_Click;

            this.Controls.Add(_txtLog);
            this.Controls.Add(_btnStartStop);
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                StopServer();
                _btnStartStop.Text = "Start Server";
            }
            else
            {
                StartServer();
                _btnStartStop.Text = "Stop Server";
            }
        }

        private void StartServer()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 8888);
                _listener.Start();
                _isRunning = true;

                new Thread(() =>
                {
                    LogMessage("Server started on port 8888");
                    while (_isRunning)
                    {
                        try
                        {
                            var client = _listener.AcceptTcpClient();
                            var handler = new ClientHandler(client, this);
                            lock (_clients)
                            {
                                _clients.Add(handler);
                            }
                            new Thread(handler.HandleClient).Start();
                        }
                        catch (SocketException) when (!_isRunning)
                        {
                            LogMessage("Server stopped");
                        }
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                LogMessage($"Server error: {ex.Message}");
            }
        }

        private void StopServer()
        {
            _isRunning = false;
            try
            {
                _listener?.Stop();
                lock (_clients)
                {
                    foreach (var client in _clients.ToList())
                    {
                        client.Disconnect();
                    }
                    _clients.Clear();
                }
                LogMessage("Server stopped gracefully");
            }
            catch (Exception ex)
            {
                LogMessage($"Stop error: {ex.Message}");
            }
        }

        public void LogMessage(string message)
        {
            if (_txtLog.InvokeRequired)
            {
                _txtLog.Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                _txtLog.AppendText($"[{DateTime.Now:T}] {message}\r\n");
                _txtLog.SelectionStart = _txtLog.Text.Length;
                _txtLog.ScrollToCaret();
            }
        }

        public void RemoveClient(ClientHandler client)
        {
            lock (_clients)
            {
                if (_clients.Remove(client))
                {
                    LogMessage($"Client disconnected: {client.Username}");
                }
            }
        }

        public List<string> GetUserList()
        {
            lock (_clients)
            {
                return _clients.Where(c => c.IsConnected).Select(c => c.Username).ToList();
            }
        }

        public void Broadcast(string message, ClientHandler sender = null)
        {
            // Убедимся, что это не приватное сообщение
            if (message.StartsWith("PRIVATE:")) return;

            var cleanMessage = CleanMessage(message);
            LogMessage($"Broadcast: {cleanMessage}");

            lock (_clients)
            {
                foreach (var client in _clients.Where(c => c != sender && c.IsConnected))
                {
                    client.SendMessage(cleanMessage);
                }
            }
        }

        public void SendPrivate(ClientHandler sender, string recipient, string message)
        {
            lock (_clients)
            {
                // Запрещаем отправку самому себе
                if (sender.Username == recipient)
                {
                    sender.SendMessage($"ERROR:Нельзя отправлять сообщения самому себе");
                    return;
                }

                var recipientClient = _clients.FirstOrDefault(c => c.Username == recipient && c.IsConnected);
                if (recipientClient != null)
                {
                    // Отправляем получателю
                    recipientClient.SendMessage($"PRIVATE:{sender.Username}:{message}");
                    // Отправляем копию отправителю с пометкой (я)
                    //sender.SendMessage($"PRIVATE:{recipient}:(я) {message}");
                    LogMessage($"Private: {sender.Username} -> {recipient}: {message}");
                }
                else
                {
                    sender.SendMessage($"ERROR:Пользователь {recipient} не найден");
                }
            }
        }

        private string CleanMessage(string message)
        {
            var parts = message.Split(':');
            if (parts.Length > 2 && parts[0] == parts[1])
            {
                return $"{parts[0]}:{string.Join(":", parts.Skip(2))}";
            }
            return message;
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }
    }

    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly ServerForm _server;
        private NetworkStream _stream;
        private bool _isConnected;

        public string Username { get; private set; }
        public bool IsConnected => _isConnected && _client.Connected;

        public ClientHandler(TcpClient client, ServerForm server)
        {
            _client = client;
            _server = server;
            _stream = client.GetStream();
            _isConnected = true;
        }

        public void HandleClient()
        {
            try
            {
                // Get username
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                Username = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                _server.LogMessage($"Client connected: {Username}");

                // Send current user list (excluding self)
                foreach (var user in _server.GetUserList().Where(u => u != Username))
                {
                    SendMessage($"USER_JOINED:{user}");
                }

                // Notify others about new user
                _server.Broadcast($"USER_JOINED:{Username}", this);

                // Main message loop
                while (_isConnected)
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (message.StartsWith("PRIVATE:"))
                    {
                        var parts = message.Split(':');
                        if (parts.Length >= 4)
                        {
                            string recipient = parts[2];
                            string content = string.Join(":", parts.Skip(3));
                            _server.SendPrivate(this, recipient, content);
                        }
                    }
                    else
                    {
                        _server.Broadcast($"{Username}:{message}", this);
                    }
                }
            }
            catch (Exception ex)
            {
                _server.LogMessage($"Client error ({Username}): {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            try
            {
                _stream?.Close();
                _client?.Close();
                _server.RemoveClient(this);
                _server.Broadcast($"USER_LEFT:{Username}");
            }
            catch { }
        }
    }
}