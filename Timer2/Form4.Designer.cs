namespace TimerSys
{
    partial class Form4
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            button1 = new Button();
            SuspendLayout();

            label1.Location = new Point(16, 14);
            label1.Name = "label1";
            label1.Size = new Size(436, 40);
            label1.TabIndex = 0;
            label1.Click += label1_Click;

            label2.BackColor = Color.AntiqueWhite;
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label2.ForeColor = Color.Coral;
            label2.Location = new Point(16, 60);
            label2.Name = "label2";
            label2.Size = new Size(436, 50);
            label2.TabIndex = 1;
            label2.Text = "123456";
            label2.TextAlign = ContentAlignment.MiddleCenter;

            label3.Location = new Point(16, 120);
            label3.Name = "label3";
            label3.Size = new Size(436, 36);
            label3.TabIndex = 2;

            button1.DialogResult = DialogResult.OK;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(184, 164);
            button1.Name = "button1";
            button1.Size = new Size(100, 30);
            button1.TabIndex = 3;
            button1.Text = "OK";

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(468, 208);
            Controls.Add(button1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form4";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Komunikat";
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Button button1;
    }
}
