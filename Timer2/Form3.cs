using System;
using System.Windows.Forms;
using Timer2;

namespace TimerSys
{
    public partial class Form3 : Form
    {
        public Form1? MainForm { get; set; }

        private int _failedAttempts;

        public Form3()
        {
            InitializeComponent();
            AcceptButton = button1;
            textBox1.MaxLength = 64;
            Text         = Lang.T("pwd.dlgTitle");
            label1.Text  = Lang.T("pwd.enter");
            button1.Text = Lang.T("dlg.ok");
            label2.Visible = false;
            label2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!PasswordStore.Exists())
            {

                MessageBox.Show(Lang.T("pwd.missing"),
                    Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MainForm?.UnlockScreen();
                Close();
                return;
            }

            if (PasswordStore.Verify(textBox1.Text))
            {
                _failedAttempts = 0;
                MainForm?.UnlockScreen();
                Close();
                return;
            }

            _failedAttempts++;
            label2.Text = Lang.T(_failedAttempts >= 5 ? "pwd.wrongWait" : "pwd.wrong");
            label2.Visible = true;
            label2.Enabled = true;
            textBox1.SelectAll();
            textBox1.Focus();

            if (_failedAttempts >= 5)
            {

                button1.Enabled = false;
                var delay = new System.Windows.Forms.Timer { Interval = 3000 };
                delay.Tick += (s, ev) =>
                {
                    delay.Stop();
                    delay.Dispose();
                    if (!IsDisposed) button1.Enabled = true;
                };
                delay.Start();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            label2.Visible = false;
            label2.Enabled = false;
        }
    }
}
