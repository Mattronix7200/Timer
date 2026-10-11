using System.Diagnostics;

namespace Timer2
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tabPage2 = new TabPage();
            tip02 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            textBox9 = new TextBox();
            textBox7 = new TextBox();
            button4 = new Button();
            dateTimePicker1 = new DateTimePicker();
            comboBox2 = new ComboBox();
            label12 = new Label();
            tabPage1 = new TabPage();
            tip01 = new Label();
            label11 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            textBox5 = new TextBox();
            textBox4 = new TextBox();
            button2 = new Button();
            comboBox1 = new ComboBox();
            tabControl1 = new TabControl();
            tabPage6 = new TabPage();
            stopStartHarmonogram = new Button();
            tip03 = new Label();
            checkedListBox1 = new CheckedListBox();
            label26 = new Label();
            label24 = new Label();
            label25 = new Label();
            textBox1 = new TextBox();
            button8 = new Button();
            label22 = new Label();
            comboBox3 = new ComboBox();
            label21 = new Label();
            tabPage5 = new TabPage();
            button12 = new Button();
            button11 = new Button();
            button10 = new Button();
            dataGridView1 = new DataGridView();
            Column1 = new DataGridViewTextBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            Column3 = new DataGridViewTextBoxColumn();
            Column4 = new DataGridViewTextBoxColumn();
            label15 = new Label();
            label14 = new Label();
            label13 = new Label();
            label9 = new Label();
            tabPage3 = new TabPage();
            groupBox3 = new GroupBox();
            button13 = new Button();
            button7 = new Button();
            label23 = new Label();
            label20 = new Label();
            button5 = new Button();
            groupBox1 = new GroupBox();
            checkBox16 = new CheckBox();
            checkBox15 = new CheckBox();
            checkBox12 = new CheckBox();
            checkBox7 = new CheckBox();
            checkBox6 = new CheckBox();
            button6 = new Button();
            label1 = new Label();
            textBoxUser = new TextBox();
            checkBox11 = new CheckBox();
            checkBox8 = new CheckBox();
            label27 = new Label();
            label29 = new Label();
            checkBox10 = new CheckBox();
            checkBox5 = new CheckBox();
            checkBox1 = new CheckBox();
            checkBox9 = new CheckBox();
            checkBox3 = new CheckBox();
            checkBox2 = new CheckBox();
            label28 = new Label();
            checkBox4 = new CheckBox();
            tabPage4 = new TabPage();
            textBoxInfo = new TextBox();
            label10 = new Label();
            pictureBox4 = new PictureBox();
            pictureBox1 = new PictureBox();
            label8 = new Label();
            pictureBox5 = new PictureBox();
            contextMenuStrip1 = new ContextMenuStrip(components);
            label19 = new PictureBox();
            helpLink = new LinkLabel();
            tabPage2.SuspendLayout();
            tabPage1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage6.SuspendLayout();
            tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabPage3.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox1.SuspendLayout();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).BeginInit();
            ((System.ComponentModel.ISupportInitialize)label19).BeginInit();
            SuspendLayout();

            tabPage2.BackColor = SystemColors.ButtonHighlight;
            tabPage2.Controls.Add(tip02);
            tabPage2.Controls.Add(label7);
            tabPage2.Controls.Add(label6);
            tabPage2.Controls.Add(label5);
            tabPage2.Controls.Add(textBox9);
            tabPage2.Controls.Add(textBox7);
            tabPage2.Controls.Add(button4);
            tabPage2.Controls.Add(dateTimePicker1);
            tabPage2.Controls.Add(comboBox2);
            tabPage2.Controls.Add(label12);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(666, 410);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Stoper";

            tip02.BackColor = SystemColors.Info;
            tip02.Cursor = Cursors.Help;
            tip02.ForeColor = Color.FromArgb(192, 64, 0);
            tip02.Location = new Point(418, 99);
            tip02.Name = "tip02";
            tip02.Padding = new Padding(10);
            tip02.Size = new Size(230, 170);
            tip02.TabIndex = 23;
            tip02.Text = "WSKAZÓWKA\r\n\r\n%selectedIndexoption-info%\r\n";

            label7.AutoSize = true;
            label7.ForeColor = Color.DimGray;
            label7.Location = new Point(106, 202);
            label7.Name = "label7";
            label7.Size = new Size(209, 17);
            label7.TabIndex = 20;
            label7.Text = "Podaj godzinę w formacie HH:MM";

            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label6.Location = new Point(20, 146);
            label6.Name = "label6";
            label6.Size = new Size(180, 17);
            label6.TabIndex = 19;
            label6.Text = "Moment aktywacji działania";

            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label5.Location = new Point(20, 78);
            label5.Name = "label5";
            label5.Size = new Size(155, 17);
            label5.TabIndex = 18;
            label5.Text = "Czynność do wykonania";

            textBox9.BackColor = SystemColors.ButtonHighlight;
            textBox9.BorderStyle = BorderStyle.None;
            textBox9.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 238);
            textBox9.ForeColor = SystemColors.MenuHighlight;
            textBox9.Location = new Point(19, 278);
            textBox9.Multiline = true;
            textBox9.Name = "textBox9";
            textBox9.ReadOnly = true;
            textBox9.Size = new Size(629, 63);
            textBox9.TabIndex = 15;
            textBox9.TextAlign = HorizontalAlignment.Center;

            textBox7.BorderStyle = BorderStyle.FixedSingle;
            textBox7.Location = new Point(20, 200);
            textBox7.Name = "textBox7";
            textBox7.Size = new Size(79, 25);
            textBox7.TabIndex = 14;
            textBox7.TextChanged += textBox7_TextChanged;

            button4.Location = new Point(20, 243);
            button4.Name = "button4";
            button4.Size = new Size(343, 26);
            button4.TabIndex = 15;
            button4.Text = "Dodaj do listy zadań i uruchom";
            button4.UseVisualStyleBackColor = true;

            dateTimePicker1.Location = new Point(20, 169);
            dateTimePicker1.Name = "dateTimePicker1";
            dateTimePicker1.Size = new Size(295, 25);
            dateTimePicker1.TabIndex = 15;
            dateTimePicker1.ValueChanged += dateTimePicker1_ValueChanged;

            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(20, 102);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(343, 25);
            comboBox2.TabIndex = 11;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            label12.BackColor = SystemColors.Control;
            label12.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 238);
            label12.Location = new Point(21, 12);
            label12.Name = "label12";
            label12.Padding = new Padding(10);
            label12.Size = new Size(627, 51);
            label12.TabIndex = 19;
            label12.Text = "Tutaj możesz zdefiniować oraz aktywować określone działanie w konkretnym czasie. Aby to zadziałało, należy wybrać datę oraz podać godzinę. Bezczynność nie będzie monitorowana.";
            label12.TextAlign = ContentAlignment.MiddleLeft;

            tabPage1.BackColor = SystemColors.ButtonHighlight;
            tabPage1.Controls.Add(tip01);
            tabPage1.Controls.Add(label11);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(textBox5);
            tabPage1.Controls.Add(textBox4);
            tabPage1.Controls.Add(button2);
            tabPage1.Controls.Add(comboBox1);
            tabPage1.Location = new Point(4, 26);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(666, 410);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Bezczynność";

            tip01.BackColor = SystemColors.Info;
            tip01.Cursor = Cursors.Help;
            tip01.ForeColor = Color.FromArgb(192, 64, 0);
            tip01.Location = new Point(418, 81);
            tip01.Name = "tip01";
            tip01.Padding = new Padding(10);
            tip01.Size = new Size(230, 170);
            tip01.TabIndex = 20;
            tip01.Text = "WSKAZÓWKA\r\n\r\n%selectedIndexoption-info%";

            label11.BackColor = SystemColors.Control;
            label11.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label11.Location = new Point(21, 12);
            label11.Name = "label11";
            label11.Padding = new Padding(10);
            label11.Size = new Size(627, 54);
            label11.TabIndex = 18;
            label11.Text = "Za pomocą powyższych opcji możesz zdefiniować wykonanie akcji po upływie określonej ilości czasu bezczynności systemu (braku aktywności użytkownika)";
            label11.TextAlign = ContentAlignment.MiddleLeft;

            label4.AutoSize = true;
            label4.ForeColor = Color.DimGray;
            label4.Location = new Point(20, 199);
            label4.Name = "label4";
            label4.Size = new Size(204, 17);
            label4.TabIndex = 17;
            label4.Text = "Podaj czas w formacie HH:MM:SS";

            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label3.Location = new Point(20, 142);
            label3.Name = "label3";
            label3.Size = new Size(175, 17);
            label3.TabIndex = 16;
            label3.Text = "Czas do aktywacji działania";

            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label2.Location = new Point(20, 74);
            label2.Name = "label2";
            label2.Size = new Size(155, 17);
            label2.TabIndex = 15;
            label2.Text = "Czynność do wykonania";

            textBox5.BackColor = SystemColors.ButtonHighlight;
            textBox5.BorderStyle = BorderStyle.None;
            textBox5.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 238);
            textBox5.ForeColor = SystemColors.MenuHighlight;
            textBox5.Location = new Point(20, 277);
            textBox5.Multiline = true;
            textBox5.Name = "textBox5";
            textBox5.ReadOnly = true;
            textBox5.Size = new Size(627, 56);
            textBox5.TabIndex = 14;
            textBox5.TextAlign = HorizontalAlignment.Center;

            textBox4.BorderStyle = BorderStyle.FixedSingle;
            textBox4.Location = new Point(20, 166);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(343, 25);
            textBox4.TabIndex = 12;
            textBox4.TextChanged += textBox4_TextChanged;

            button2.Location = new Point(21, 225);
            button2.Name = "button2";
            button2.Size = new Size(342, 26);
            button2.TabIndex = 13;
            button2.Text = "Dodaj do listy zadań i uruchom";
            button2.UseVisualStyleBackColor = true;

            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(20, 98);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(343, 25);
            comboBox1.TabIndex = 0;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage6);
            tabControl1.Controls.Add(tabPage5);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(28, 104);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(674, 438);
            tabControl1.TabIndex = 8;

            tabPage6.AutoScroll = true;
            tabPage6.AutoScrollMargin = new Size(0, 20);
            tabPage6.BackColor = SystemColors.ButtonHighlight;
            tabPage6.Controls.Add(stopStartHarmonogram);
            tabPage6.Controls.Add(tip03);
            tabPage6.Controls.Add(checkedListBox1);
            tabPage6.Controls.Add(label26);
            tabPage6.Controls.Add(label24);
            tabPage6.Controls.Add(label25);
            tabPage6.Controls.Add(textBox1);
            tabPage6.Controls.Add(button8);
            tabPage6.Controls.Add(label22);
            tabPage6.Controls.Add(comboBox3);
            tabPage6.Controls.Add(label21);
            tabPage6.Location = new Point(4, 24);
            tabPage6.Name = "tabPage6";
            tabPage6.Size = new Size(666, 410);
            tabPage6.TabIndex = 5;
            tabPage6.Text = "Harmonogram";

            stopStartHarmonogram.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 238);
            stopStartHarmonogram.Location = new Point(416, 303);
            stopStartHarmonogram.Name = "stopStartHarmonogram";
            stopStartHarmonogram.Size = new Size(217, 26);
            stopStartHarmonogram.TabIndex = 34;
            stopStartHarmonogram.Text = "Uruchom / Zatrzymaj harmonogram";
            stopStartHarmonogram.UseVisualStyleBackColor = true;

            tip03.BackColor = SystemColors.Info;
            tip03.Cursor = Cursors.Help;
            tip03.ForeColor = Color.FromArgb(192, 64, 0);
            tip03.Location = new Point(416, 90);
            tip03.Name = "tip03";
            tip03.Padding = new Padding(10);
            tip03.Size = new Size(217, 201);
            tip03.TabIndex = 33;
            tip03.Text = "WSKAZÓWKA\r\n\r\n%selectedIndexoption-info%\r\n";

            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Items.AddRange(new object[] { "Codziennie", "Co dwa dni", "Co trzy dni", "Co cztery dni", "Co pięć dni", "Co sześć dni", "Co tydzień", "W każdy weekend (Pt.-Nd.)", "W każdy poniedziałek", "W każdy wtorek", "W każdą środę", "W każdy czwartek", "W każdy piątek", "W każdą sobotę", "W każdą niedzielę", "Od poniedziałku do piątku", "Powtarzaj co..." });
            checkedListBox1.Location = new Point(20, 227);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(343, 64);
            checkedListBox1.TabIndex = 30;

            label26.AutoSize = true;
            label26.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label26.Location = new Point(20, 200);
            label26.Name = "label26";
            label26.Size = new Size(212, 17);
            label26.TabIndex = 28;
            label26.Text = "Moment powtórzenia aktywatora";

            label24.AutoSize = true;
            label24.ForeColor = Color.DimGray;
            label24.Location = new Point(106, 167);
            label24.Name = "label24";
            label24.Size = new Size(209, 17);
            label24.TabIndex = 27;
            label24.Text = "Podaj godzinę w formacie HH:MM";

            label25.AutoSize = true;
            label25.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label25.Location = new Point(20, 138);
            label25.Name = "label25";
            label25.Size = new Size(180, 17);
            label25.TabIndex = 26;
            label25.Text = "Moment aktywacji działania";

            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(20, 164);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(79, 25);
            textBox1.TabIndex = 23;

            button8.Location = new Point(20, 303);
            button8.Name = "button8";
            button8.Size = new Size(343, 26);
            button8.TabIndex = 25;
            button8.Text = "Dodaj do listy zadań i uruchom";
            button8.UseVisualStyleBackColor = true;

            label22.AutoSize = true;
            label22.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            label22.Location = new Point(20, 79);
            label22.Name = "label22";
            label22.Size = new Size(155, 17);
            label22.TabIndex = 22;
            label22.Text = "Czynność do wykonania";

            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(20, 103);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(343, 25);
            comboBox3.TabIndex = 21;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;

            label21.BackColor = SystemColors.Control;
            label21.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 238);
            label21.Location = new Point(20, 11);
            label21.Name = "label21";
            label21.Padding = new Padding(10);
            label21.Size = new Size(613, 53);
            label21.TabIndex = 20;
            label21.Text = "Jeżeli potrzebujesz uaktywnienia licznika czasu powtarzanego ciągiem według określonego harmonogramu, skorzystaj z powyższych opcji.";
            label21.TextAlign = ContentAlignment.MiddleLeft;

            tabPage5.Controls.Add(button12);
            tabPage5.Controls.Add(button11);
            tabPage5.Controls.Add(button10);
            tabPage5.Controls.Add(dataGridView1);
            tabPage5.Controls.Add(label15);
            tabPage5.Controls.Add(label14);
            tabPage5.Controls.Add(label13);
            tabPage5.Controls.Add(label9);
            tabPage5.Location = new Point(4, 24);
            tabPage5.Name = "tabPage5";
            tabPage5.Padding = new Padding(3);
            tabPage5.Size = new Size(666, 410);
            tabPage5.TabIndex = 6;
            tabPage5.Text = "Lista zadań";
            tabPage5.UseVisualStyleBackColor = true;

            button12.Location = new Point(16, 292);
            button12.Name = "button12";
            button12.Size = new Size(140, 30);
            button12.TabIndex = 8;
            button12.Text = "Włącz/wyłącz";
            button12.UseVisualStyleBackColor = true;

            button11.Location = new Point(16, 328);
            button11.Name = "button11";
            button11.Size = new Size(140, 30);
            button11.TabIndex = 7;
            button11.Text = "Edytuj zadanie";
            button11.UseVisualStyleBackColor = true;

            button10.Location = new Point(16, 364);
            button10.Name = "button10";
            button10.Size = new Size(140, 30);
            button10.TabIndex = 6;
            button10.Text = "Usuń zadanie";
            button10.UseVisualStyleBackColor = true;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2, Column3, Column4 });
            dataGridView1.Location = new Point(169, 46);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.ShowEditingIcon = false;
            dataGridView1.Size = new Size(478, 345);
            dataGridView1.TabIndex = 5;

            Column1.HeaderText = "Zadanie";
            Column1.Name = "Column1";
            Column1.ReadOnly = true;

            Column2.HeaderText = "Pozostały czas";
            Column2.Name = "Column2";
            Column2.ReadOnly = true;

            Column3.HeaderText = "Stan działania";
            Column3.Name = "Column3";
            Column3.ReadOnly = true;

            Column4.HeaderText = "Kolejka";
            Column4.Name = "Column4";
            Column4.ReadOnly = true;

            label15.AutoSize = true;
            label15.Location = new Point(26, 82);
            label15.Name = "label15";
            label15.Size = new Size(116, 17);
            label15.TabIndex = 3;
            label15.Text = "Harmonogram";

            label14.AutoSize = true;
            label14.Location = new Point(26, 113);
            label14.Name = "label14";
            label14.Size = new Size(69, 17);
            label14.TabIndex = 2;
            label14.Text = "Stoper";

            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI", 9.75F);
            label13.ForeColor = SystemColors.ControlText;
            label13.Location = new Point(26, 53);
            label13.Name = "label13";
            label13.Size = new Size(102, 17);
            label13.TabIndex = 1;
            label13.Text = "Bezczynność";

            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label9.Location = new Point(16, 13);
            label9.Name = "label9";
            label9.Size = new Size(338, 17);
            label9.TabIndex = 0;
            label9.Text = "Kolejka aktywatorów zadań dla określonych liczników";

            tabPage3.AutoScroll = true;
            tabPage3.AutoScrollMargin = new Size(0, 20);
            tabPage3.BackColor = SystemColors.ButtonHighlight;
            tabPage3.Controls.Add(groupBox3);
            tabPage3.Controls.Add(groupBox1);
            tabPage3.Location = new Point(4, 26);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(666, 410);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Ustawienia";

            groupBox3.Controls.Add(button13);
            groupBox3.Controls.Add(button7);
            groupBox3.Controls.Add(label23);
            groupBox3.Controls.Add(label20);
            groupBox3.Controls.Add(button5);
            groupBox3.Location = new Point(20, 583);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(610, 142);
            groupBox3.TabIndex = 20;
            groupBox3.TabStop = false;

            button13.Location = new Point(16, 94);
            button13.Name = "button13";
            button13.Size = new Size(285, 30);
            button13.TabIndex = 22;
            button13.Text = "Import/kopia ustawień...";
            button13.UseVisualStyleBackColor = true;

            button7.Location = new Point(309, 56);
            button7.Name = "button7";
            button7.Size = new Size(285, 30);
            button7.TabIndex = 21;
            button7.Text = "Resetuj ustawienia aplikacji";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;

            label23.AutoSize = true;
            label23.Enabled = false;
            label23.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label23.ForeColor = Color.SteelBlue;
            label23.Location = new Point(17, 28);
            label23.Name = "label23";
            label23.Size = new Size(244, 17);
            label23.TabIndex = 21;
            label23.Text = "Posiadasz najnowszą wersję programu.";
            label23.Visible = false;

            label20.AutoSize = true;
            label20.Enabled = false;
            label20.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label20.ForeColor = Color.ForestGreen;
            label20.Location = new Point(16, 28);
            label20.Name = "label20";
            label20.Size = new Size(411, 17);
            label20.TabIndex = 18;
            label20.Text = "Dostępna jest nowa wersja aplikacji. Kliknij tutaj aby zaktualizować.";
            label20.Visible = false;
            label20.Click += label20_Click;

            button5.Location = new Point(16, 56);
            button5.Name = "button5";
            button5.Size = new Size(285, 30);
            button5.TabIndex = 5;
            button5.Text = "Edytuj plik konfiguracyjny";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;

            groupBox1.Controls.Add(checkBox16);
            groupBox1.Controls.Add(checkBox15);
            groupBox1.Controls.Add(checkBox12);
            groupBox1.Controls.Add(checkBox7);
            groupBox1.Controls.Add(checkBox6);
            groupBox1.Controls.Add(button6);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(textBoxUser);
            groupBox1.Controls.Add(checkBox11);
            groupBox1.Controls.Add(checkBox8);
            groupBox1.Controls.Add(label27);
            groupBox1.Controls.Add(label29);
            groupBox1.Controls.Add(checkBox10);
            groupBox1.Controls.Add(checkBox5);
            groupBox1.Controls.Add(checkBox1);
            groupBox1.Controls.Add(checkBox9);
            groupBox1.Controls.Add(checkBox3);
            groupBox1.Controls.Add(checkBox2);
            groupBox1.Controls.Add(label28);
            groupBox1.Controls.Add(checkBox4);
            groupBox1.Location = new Point(20, 14);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(610, 563);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;

            checkBox16.AutoSize = true;
            checkBox16.Location = new Point(24, 544);
            checkBox16.Name = "checkBox16";
            checkBox16.Size = new Size(397, 21);
            checkBox16.TabIndex = 39;
            checkBox16.Text = "Nie pokazuj powiadomień o wykonaniu akcji przez daną kolejkę";
            checkBox16.UseVisualStyleBackColor = true;

            checkBox15.AutoSize = true;
            checkBox15.Location = new Point(24, 634);
            checkBox15.Name = "checkBox15";
            checkBox15.Size = new Size(346, 21);
            checkBox15.TabIndex = 38;
            checkBox15.Text = "Uruchamiaj program z uprawnieniami administratora";
            checkBox15.UseVisualStyleBackColor = true;

            checkBox12.AutoSize = true;
            checkBox12.Location = new Point(24, 98);
            checkBox12.Name = "checkBox12";
            checkBox12.Size = new Size(434, 21);
            checkBox12.TabIndex = 37;
            checkBox12.Text = "Automatycznie uruchamiaj zadania zapisane w kolejce harmonogramu";
            checkBox12.UseVisualStyleBackColor = true;

            checkBox7.AutoSize = true;
            checkBox7.Location = new Point(24, 312);
            checkBox7.Name = "checkBox7";
            checkBox7.Size = new Size(545, 21);
            checkBox7.TabIndex = 9;
            checkBox7.Text = "Zaczekaj na zakończenie uruchomionego procesu, zanim program przejdzie dalej";
            checkBox7.UseVisualStyleBackColor = true;
            checkBox7.CheckedChanged += checkBox7_CheckedChanged;

            checkBox6.AutoSize = true;
            checkBox6.Location = new Point(24, 608);
            checkBox6.Name = "checkBox6";
            checkBox6.Size = new Size(220, 21);
            checkBox6.TabIndex = 5;
            checkBox6.Text = "Aktywuj blokadę ustawień hasłem";
            checkBox6.UseVisualStyleBackColor = true;
            checkBox6.CheckedChanged += checkBox6_CheckedChanged;

            button6.Enabled = false;
            button6.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 238);
            button6.Location = new Point(378, 606);
            button6.Name = "button6";
            button6.Size = new Size(217, 26);
            button6.TabIndex = 6;
            button6.Text = "Ustaw hasło blokady programu";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;

            label1.AutoSize = true;
            label1.Location = new Point(24, 258);
            label1.Name = "label1";
            label1.Size = new Size(519, 17);
            label1.TabIndex = 8;
            label1.Text = "Polecenie lub ścieżka do programu/skryptu, który ma wykonać aktywator.";

            textBoxUser.BorderStyle = BorderStyle.FixedSingle;
            textBoxUser.Location = new Point(24, 280);
            textBoxUser.Name = "textBoxUser";
            textBoxUser.Size = new Size(575, 25);
            textBoxUser.TabIndex = 7;
            textBoxUser.TextChanged += textBoxUser_TextChanged;

            checkBox11.AutoSize = true;
            checkBox11.Location = new Point(24, 414);
            checkBox11.Name = "checkBox11";
            checkBox11.Size = new Size(314, 21);
            checkBox11.TabIndex = 36;
            checkBox11.Text = "Włącz tryb ciemny (czarna kolorystyka programu)";
            checkBox11.UseVisualStyleBackColor = true;

            checkBox8.AutoSize = true;
            checkBox8.Location = new Point(24, 150);
            checkBox8.Name = "checkBox8";
            checkBox8.Size = new Size(466, 21);
            checkBox8.TabIndex = 31;
            checkBox8.Text = "Podczas aktywacji licznika czasu sprawdź, czy strefa czasowa uległa zmianie";
            checkBox8.UseVisualStyleBackColor = true;

            label27.AutoSize = true;
            label27.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label27.Location = new Point(16, 16);
            label27.Name = "label27";
            label27.Size = new Size(130, 17);
            label27.TabIndex = 34;
            label27.Text = "Działanie programu";

            label29.AutoSize = true;
            label29.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label29.Location = new Point(16, 576);
            label29.Name = "label29";
            label29.Size = new Size(101, 17);
            label29.TabIndex = 36;
            label29.Text = "Zabezpieczenia";

            checkBox10.AutoSize = true;
            checkBox10.Location = new Point(24, 518);
            checkBox10.Name = "checkBox10";
            checkBox10.Size = new Size(350, 21);
            checkBox10.TabIndex = 33;
            checkBox10.Text = "Nie pokazuj licznika czasowego po aktywacji odliczania.";
            checkBox10.UseVisualStyleBackColor = true;

            checkBox5.AutoSize = true;
            checkBox5.Location = new Point(24, 176);
            checkBox5.Name = "checkBox5";
            checkBox5.Size = new Size(501, 21);
            checkBox5.TabIndex = 4;
            checkBox5.Text = "Nie pozwalaj Windowsowi automatycznie usypiać komputera (Nie usypiaj)";
            checkBox5.UseVisualStyleBackColor = true;
            checkBox5.CheckedChanged += checkBox5_CheckedChanged;

            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(24, 46);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(394, 21);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Automatycznie uruchamiaj program wraz z systemem Windows";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;

            checkBox9.AutoSize = true;
            checkBox9.Location = new Point(24, 492);
            checkBox9.Name = "checkBox9";
            checkBox9.Size = new Size(211, 21);
            checkBox9.TabIndex = 32;
            checkBox9.Text = "Nie pokazuj ikonki w zasobniku.";
            checkBox9.UseVisualStyleBackColor = true;

            checkBox3.AutoSize = true;
            checkBox3.Location = new Point(24, 466);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new Size(349, 21);
            checkBox3.TabIndex = 3;
            checkBox3.Text = "Minimalizuj do paska zadań, zamiast zamykać program";
            checkBox3.UseVisualStyleBackColor = true;
            checkBox3.CheckedChanged += checkBox3_CheckedChanged;

            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(24, 72);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(584, 21);
            checkBox2.TabIndex = 1;
            checkBox2.Text = "Automatycznie uruchamiaj odliczanie po uruchomieniu programu (tylko aktywator bezczynności)";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.CheckedChanged += checkBox2_CheckedChanged;

            label28.AutoSize = true;
            label28.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 238);
            label28.Location = new Point(16, 344);
            label28.Name = "label28";
            label28.Size = new Size(55, 17);
            label28.TabIndex = 35;
            label28.Text = "Wygląd";

            checkBox4.AutoSize = true;
            checkBox4.Location = new Point(24, 124);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(439, 21);
            checkBox4.TabIndex = 2;
            checkBox4.Text = "Włącz automatyczne resetowanie licznika (tylko aktywator bezczynności)";
            checkBox4.UseVisualStyleBackColor = true;
            checkBox4.CheckedChanged += checkBox4_CheckedChanged;

            tabPage4.AutoScroll = true;
            tabPage4.BackColor = SystemColors.ButtonHighlight;
            tabPage4.Controls.Add(textBoxInfo);
            tabPage4.Controls.Add(label10);
            tabPage4.Controls.Add(pictureBox4);
            tabPage4.Controls.Add(pictureBox1);
            tabPage4.Controls.Add(label8);
            tabPage4.Location = new Point(4, 26);
            tabPage4.Name = "tabPage4";
            tabPage4.Size = new Size(666, 410);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Informacje";

            textBoxInfo.BackColor = Color.WhiteSmoke;
            textBoxInfo.BorderStyle = BorderStyle.FixedSingle;
            textBoxInfo.Cursor = Cursors.Help;
            textBoxInfo.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 238);
            textBoxInfo.Location = new Point(27, 82);
            textBoxInfo.Multiline = true;
            textBoxInfo.Name = "textBoxInfo";
            textBoxInfo.ReadOnly = true;
            textBoxInfo.ScrollBars = ScrollBars.Vertical;
            textBoxInfo.Size = new Size(624, 252);
            textBoxInfo.TabIndex = 18;
            textBoxInfo.Text = resources.GetString("textBoxInfo.Text");

            label10.BackColor = Color.Transparent;
            label10.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 238);
            label10.Location = new Point(112, 356);
            label10.Name = "label10";
            label10.Size = new Size(522, 46);
            label10.TabIndex = 16;
            label10.Text = resources.GetString("label10.Text");

            pictureBox4.Image = (Image)resources.GetObject("pictureBox4.Image");
            pictureBox4.InitialImage = (Image)resources.GetObject("pictureBox4.InitialImage");
            pictureBox4.Location = new Point(27, 346);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(606, 1);
            pictureBox4.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox4.TabIndex = 13;
            pictureBox4.TabStop = false;

            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = (Image)resources.GetObject("pictureBox1.InitialImage");
            pictureBox1.Location = new Point(14, 356);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(88, 31);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;

            label8.BackColor = Color.Transparent;
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 238);
            label8.Location = new Point(14, 13);
            label8.Name = "label8";
            label8.Size = new Size(620, 62);
            label8.TabIndex = 14;
            label8.Text = resources.GetString("label8.Text");

            pictureBox5.Image = (Image)resources.GetObject("pictureBox5.Image");
            pictureBox5.InitialImage = (Image)resources.GetObject("pictureBox5.InitialImage");
            pictureBox5.Location = new Point(28, 14);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(320, 60);
            pictureBox5.SizeMode = PictureBoxSizeMode.Normal;
            pictureBox5.TabIndex = 16;
            pictureBox5.TabStop = false;

            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);

            label19.Image = TimerSys.Properties.Resources.block;
            label19.Location = new Point(0, 0);
            label19.Name = "label19";
            label19.Size = new Size(732, 564);
            label19.SizeMode = PictureBoxSizeMode.StretchImage;
            label19.TabIndex = 19;
            label19.TabStop = false;

            helpLink.AutoSize = true;
            helpLink.LinkColor = Color.FromArgb(255, 128, 128);
            helpLink.Location = new Point(609, 9);
            helpLink.Name = "helpLink";
            helpLink.Size = new Size(111, 17);
            helpLink.TabIndex = 21;
            helpLink.TabStop = true;
            helpLink.Text = "Pomoc programu";

            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(732, 564);
            Controls.Add(label19);
            Controls.Add(helpLink);
            Controls.Add(tabControl1);
            Controls.Add(pictureBox5);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 238);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            Text = "Wyłącznik czasowy";
            Load += Form1_Load;
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage6.ResumeLayout(false);
            tabPage6.PerformLayout();
            tabPage5.ResumeLayout(false);
            tabPage5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabPage3.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).EndInit();
            ((System.ComponentModel.ISupportInitialize)label19).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TabPage tabPage2;
        private TextBox textBox9;
        private TextBox textBox7;
        private Button button4;
        private DateTimePicker dateTimePicker1;
        private ComboBox comboBox2;
        private TabPage tabPage1;
        private Label tip01;
        private TextBox textBox5;
        private TextBox textBox4;
        private Button button2;
        private ComboBox comboBox1;
        public TabControl tabControl1;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private PictureBox pictureBox1;
        private PictureBox pictureBox4;
        private Label label2;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label7;
        private Label label10;
        private Label label8;
        private Label label11;
        private PictureBox pictureBox5;
        private Label label12;
        private CheckBox checkBox3;
        private CheckBox checkBox4;
        private CheckBox checkBox2;
        private CheckBox checkBox1;
        private Button button5;
        private GroupBox groupBox1;
        private Label label1;
        private TextBox textBoxUser;
        private TextBox textBoxInfo;
        private Label label13;
        private Label label15;
        private Label label14;
        private Label tip02;
        private CheckBox checkBox5;
        public Button button6;
        private CheckBox checkBox6;
        private GroupBox groupBox3;
        private Label label20;
        private Label label23;
        private Button button7;
        private CheckBox checkBox7;
        private TabPage tabPage6;
        private Label label21;
        private Label label24;
        private Label label25;
        private TextBox textBox1;
        private Button button8;
        private Label label22;
        private ComboBox comboBox3;
        private Label label26;
        private CheckBox checkBox9;
        private CheckBox checkBox8;
        private CheckedListBox checkedListBox1;
        private CheckBox checkBox10;
        private CheckBox checkBox11;
        private Label label27;
        private Label label29;
        private Label label28;
        private Label tip03;
        private ContextMenuStrip contextMenuStrip1;
        private PictureBox label19;
        private TabPage tabPage5;
        private DataGridView dataGridView1;
        private Label label9;
        private Button button12;
        private Button button11;
        private Button button10;
        private Button button13;
        private LinkLabel helpLink;
        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private CheckBox checkBox12;
        private CheckBox checkBox16;
        private CheckBox checkBox15;
        private Button stopStartHarmonogram;
    }
}
