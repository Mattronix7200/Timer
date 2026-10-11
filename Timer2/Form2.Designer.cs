namespace TimerSys
{
    partial class Form2
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
            button1 = new Button();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();

            button1.Location = new Point(313, 142);
            button1.Name = "button1";
            button1.Size = new Size(89, 23);
            button1.TabIndex = 0;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(12, 39);
            textBox1.Name = "textBox1";
            textBox1.PasswordChar = '*';
            textBox1.Size = new Size(390, 23);
            textBox1.TabIndex = 1;
            textBox1.UseSystemPasswordChar = true;
            textBox1.TextChanged += textBox1_TextChanged;

            textBox2.BorderStyle = BorderStyle.FixedSingle;
            textBox2.Location = new Point(12, 103);
            textBox2.Name = "textBox2";
            textBox2.PasswordChar = '*';
            textBox2.Size = new Size(390, 23);
            textBox2.TabIndex = 2;
            textBox2.UseSystemPasswordChar = true;
            textBox2.TextChanged += textBox2_TextChanged;

            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(68, 15);
            label1.TabIndex = 3;
            label1.Text = "Podaj hasło";
            label1.Click += label1_Click;

            label2.AutoSize = true;
            label2.Location = new Point(12, 74);
            label2.Name = "label2";
            label2.Size = new Size(81, 15);
            label2.TabIndex = 4;
            label2.Text = "Powtórz hasło";

            label3.AutoSize = true;
            label3.Location = new Point(12, 144);
            label3.Name = "label3";
            label3.Size = new Size(0, 15);
            label3.TabIndex = 5;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(414, 181);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(button1);
            MaximizeBox = false;
            MaximumSize = new Size(430, 220);
            MinimizeBox = false;
            Name = "Form2";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Ustawienia hasła";
            Load += Form2_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}
