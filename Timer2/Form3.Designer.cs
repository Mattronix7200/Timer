namespace TimerSys
{
    partial class Form3
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            label1 = new Label();
            textBox1 = new TextBox();
            button1 = new Button();
            label2 = new Label();
            SuspendLayout();

            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(208, 15);
            label1.TabIndex = 0;
            label1.Text = "Proszę podać hasło aby kontynuować.";

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(12, 36);
            textBox1.Name = "textBox1";
            textBox1.PasswordChar = '*';
            textBox1.Size = new Size(340, 23);
            textBox1.TabIndex = 1;
            textBox1.UseSystemPasswordChar = true;
            textBox1.TextChanged += textBox1_TextChanged;

            button1.Location = new Point(277, 76);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 2;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;

            label2.AutoSize = true;
            label2.Enabled = false;
            label2.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label2.ForeColor = Color.Red;
            label2.Location = new Point(11, 78);
            label2.Name = "label2";
            label2.Size = new Size(248, 17);
            label2.TabIndex = 3;
            label2.Text = "Niepoprawne hasło! Spróbuj ponownie";
            label2.Visible = false;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(364, 111);
            Controls.Add(label2);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Controls.Add(label1);
            MaximizeBox = false;
            MaximumSize = new Size(380, 150);
            MinimizeBox = false;
            Name = "Form3";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Zabezpieczenie hasłem";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Button button1;
        private Label label2;
    }
}
