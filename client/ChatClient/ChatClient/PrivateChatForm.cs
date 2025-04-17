namespace ChatClient
{
    public partial class PrivateChatForm : Form
    {
        private readonly string _sender;
        private readonly string _recipient;
        private readonly Action<string, string> _sendHandler;

        public PrivateChatForm(string sender, string recipient, Action<string, string> sendHandler)
        {
            _sender = sender;
            _recipient = recipient;
            _sendHandler = sendHandler;
            InitializeComponent();
            Text = $"Приватный чат: {_sender} → {_recipient}";
        }

        public void ReceiveMessage(string message)
        {
            if (txtChat.InvokeRequired)
            {
                txtChat.Invoke(new Action<string>(ReceiveMessage), message);
            }
            else
            {
                txtChat.AppendText($"{message}\r\n");
                txtChat.SelectionStart = txtChat.Text.Length;
                txtChat.ScrollToCaret();
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
                // Проверяем, что не отправляем себе
                if (_recipient == _sender)
                {
                    MessageBox.Show("Нельзя отправлять сообщения самому себе");
                    return;
                }

                _sendHandler(_recipient, txtMessage.Text);
                txtChat.AppendText($"{_sender} (я): {txtMessage.Text}\r\n");
                txtMessage.Clear();
            }
        }
    }
}