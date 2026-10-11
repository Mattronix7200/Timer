using System;
using System.Windows.Forms;

namespace TimerSys
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();

            Text        = Lang.T("welcome.title");
            label1.Text = Lang.T("welcome.intro");
            label3.Text = Lang.T("welcome.note");
            button1.Text = Lang.T("dlg.ok");
            AcceptButton = button1;

            if (!PasswordStore.Exists() && !PasswordStore.Save("123456"))
            {
                MessageBox.Show(Lang.T("pwd.saveFailed"),
                    Lang.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void label1_Click(object? sender, EventArgs e)
        {
        }
    }
}
