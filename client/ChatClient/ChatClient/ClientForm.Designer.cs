namespace ChatClient
{
    partial class ClientForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtChat = new TextBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            listUsers = new ListBox();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtChat
            // 
            txtChat.Location = new Point(78, 0);
            txtChat.Multiline = true;
            txtChat.Name = "txtChat";
            txtChat.ReadOnly = true;
            txtChat.ScrollBars = ScrollBars.Vertical;
            txtChat.Size = new Size(406, 325);
            txtChat.TabIndex = 0;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(6, 16);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(307, 23);
            txtMessage.TabIndex = 1;
            txtMessage.KeyDown += txtMessage_KeyDown;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(319, 16);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 2;
            btnSend.Text = "Отправить";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // listUsers
            // 
            listUsers.Dock = DockStyle.Left;
            listUsers.FormattingEnabled = true;
            listUsers.ItemHeight = 15;
            listUsers.Location = new Point(0, 0);
            listUsers.Name = "listUsers";
            listUsers.Size = new Size(78, 382);
            listUsers.TabIndex = 3;
            listUsers.SelectedIndexChanged += listUsers_DoubleClick;
            // 
            // panel1
            // 
            panel1.Controls.Add(txtMessage);
            panel1.Controls.Add(btnSend);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(78, 331);
            panel1.Name = "panel1";
            panel1.Size = new Size(406, 51);
            panel1.TabIndex = 4;
            // 
            // ClientForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 382);
            Controls.Add(panel1);
            Controls.Add(listUsers);
            Controls.Add(txtChat);
            Name = "ClientForm";
            Text = "Чат";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox txtChat;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ListBox listUsers;
        private Panel panel1;
    }
}