using System;
using System.Windows.Forms;

namespace TimerSys
{
    public partial class Form2 : Form
    {
        private const int MinPasswordLength = 4;

        public Form2()
        {
            InitializeComponent();

            AcceptButton = button1;
            Text        = Lang.T("pwd.setTitle");
            label1.Text = Lang.T("pwd.first");
            label2.Text = Lang.T("pwd.repeat");
            button1.Text = Lang.T("dlg.ok");
            textBox1.MaxLength = 64;
            textBox2.MaxLength = 64;
            label3.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string password = textBox1.Text;

            if (password.Length < MinPasswordLength)
            {
                label3.Text = Lang.T("pwd.tooShort", MinPasswordLength);
                return;
            }
            if (password != textBox2.Text)
            {
                label3.Text = Lang.T("pwd.mismatch");
                return;
            }

            if (!PasswordStore.Save(password))
            {
                MessageBox.Show(Lang.T("pwd.saveFailed"),
                    Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e) => label3.Text = "";

        private void textBox2_TextChanged(object sender, EventArgs e) => label3.Text = "";

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void Form2_Load(object sender, EventArgs e)
        {
        }
    }
}
