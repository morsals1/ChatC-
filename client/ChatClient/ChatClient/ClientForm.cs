using System.Net.Sockets;
using System.Text;

namespace ChatClient
{
    public partial class ClientForm : Form
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly string _username;
        private bool _isConnected;
        private readonly Dictionary<string, PrivateChatForm> _privateChats = new();

        public ClientForm(string username)
        {
            InitializeComponent();
            _username = username;
            Text = $"Чат - {_username}";
            ConnectToServer();
        }

        private void ConnectToServer()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect("127.0.0.1", 8888);
                _stream = _client.GetStream();
                _isConnected = true;

                // Отправляем имя серверу
                byte[] nameData = Encoding.UTF8.GetBytes(_username);
                _stream.Write(nameData, 0, nameData.Length);

                new Thread(ReceiveMessages).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                this.Close();
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            while (_isConnected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessServerMessage(message);
                }
                catch
                {
                    break;
                }
            }
            Disconnect();
        }

        private void ProcessServerMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ProcessServerMessage), message);
                return;
            }

            if (message.StartsWith("USER_JOINED:"))
            {
                string username = message.Substring("USER_JOINED:".Length);
                if (!listUsers.Items.Contains(username))
                {
                    listUsers.Items.Add(username);
                    txtChat.AppendText($"[{username} подключился]\r\n");
                }
            }
            else if (message.StartsWith("USER_LEFT:"))
            {
                string username = message.Substring("USER_LEFT:".Length);
                listUsers.Items.Remove(username);
                txtChat.AppendText($"[{username} вышел]\r\n");
            }
            else if (message.StartsWith("PRIVATE:"))
            {
                ProcessPrivateMessage(message);
            }
            else if (message.Contains(':'))
            {
                var parts = message.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && parts[0] != _username) // Игнорируем свои же сообщения
                {
                    txtChat.AppendText($"{parts[0]}: {parts[1]}\r\n");
                }
            }
        }

        private void ProcessPrivateMessage(string message)
        {
            var parts = message.Split(':');
            if (parts.Length >= 3)
            {
                string sender = parts[1];
                string content = string.Join(":", parts.Skip(2));

                // Игнорируем сообщения от самого себя
                if (sender == _username) return;

                if (!_privateChats.TryGetValue(sender, out var chatForm))
                {
                    chatForm = new PrivateChatForm(_username, sender, SendPrivateMessage);
                    _privateChats[sender] = chatForm;
                    chatForm.FormClosed += (s, e) => _privateChats.Remove(sender);
                    chatForm.Show();
                }

                chatForm.ReceiveMessage(content);
                if (!chatForm.Visible) chatForm.Show();
            }
        }

        private void SendPrivateMessage(string recipient, string message)
        {
            try
            {
                string formatted = $"PRIVATE:{_username}:{recipient}:{message}";
                byte[] data = Encoding.UTF8.GetBytes(formatted);
                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                SendMessage();
                e.SuppressKeyPress = true;
            }
        }

        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                try
                {
                    // Отправляем только текст сообщения без имени пользователя
                    byte[] data = Encoding.UTF8.GetBytes(txtMessage.Text);
                    _stream.Write(data, 0, data.Length);

                    // Добавляем сообщение в чат с пометкой "(я)"
                    txtChat.AppendText($"{_username} (я): {txtMessage.Text}\r\n");
                    txtMessage.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отправки: {ex.Message}");
                }
            }
        }

        private void listUsers_DoubleClick(object sender, EventArgs e)
        {
            if (listUsers.SelectedItem is string selectedUser)
            {
                if (!_privateChats.TryGetValue(selectedUser, out var chatForm))
                {
                    chatForm = new PrivateChatForm(_username, selectedUser, SendPrivateMessage);
                    _privateChats[selectedUser] = chatForm;
                    chatForm.FormClosed += (s, ev) => _privateChats.Remove(selectedUser);
                }
                chatForm.Show();
                chatForm.BringToFront();
            }
        }

        private void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Disconnect();
            foreach (var chat in _privateChats.Values.ToList())
            {
                chat.Close();
            }
            base.OnFormClosing(e);
        }
    }
}