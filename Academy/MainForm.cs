using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.InteropServices;

using DBtools;

namespace Academy
{
    public partial class MainForm : Form
    {
        Query[] queries =
        {
            new Query
                (
                 "Students,Groups,Directions",
                 "last_name,first_name,middle_name,group_name,direction_name",
                 "[group]=group_id AND direction_id=direction_id" 
                ),
            new Query
                (
                "Groups,Directions",
                "group_name,weekdays,start_time,start_date,direction_name",
                "direction=direction_id"
                ),
            new Query("Directions", "*"),
            new Query("Disciplines","*"),
            new Query("Teachers",   "*"),
        };
        string[] statusBarSignatures =
        {
            "Количество студентов",
            "Количество групп",
            "Количество направлений",
            "Количество дисциплин",
            "Количество преподавателей",
        };
        DBtools.Connector connector;
        DBtools.Connector movies_connector;

        DataGridView[] tables = null;
        /// ///////////////////////////////////////////////////////////
        Dictionary<string, int> d_directions = null;
        Dictionary<string, Dictionary<string, int>> d_trees = null;
        public MainForm()
        {
            InitializeComponent();
            tables = new DataGridView[] { dgvStudents, dgvGroups, dgvDirections, dgvDisciplines, dgvTeachers };
            AllocConsole();
            connector = new DBtools.Connector(
"Data Source=SUPAMODDPC\\SQLEXPRESS;Initial Catalog=SPU_411_Import;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            movies_connector = new DBtools.Connector("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            //dgvDirections.DataSource = movies_connector.Select("SELECT * FROM Movies");
            //toolStripStatusLabel.Text = $"Колчиество направлений обучения: {connector.Scalar("SELECT COUNT(*) FROM Directions")}";
            //tabControl.SelectedIndex = 1;
            tabControl_SelectedIndexChanged(tabControl, null);

            d_trees = new Dictionary<string, Dictionary<string, int>>();
            d_trees.Add(nameof(d_directions), d_directions);
            LoadDataToComboBox(cbGroupsDirection);
            LoadDataToComboBox(cbStudentsGroup);
            LoadDataToComboBox(cbStudentsDirection);
            LoadDataToComboBox(cbDisciplinesDirection);
            cbGroupsDirection.SelectedIndexChanged += cbGroupsDirection_SelectedIndexChanged;
            cbStudentsGroup.SelectedIndexChanged += cbStudentsGroup_SelectedIndexChanged;
            cbStudentsDirection.SelectedIndexChanged += cbStudentsDirection_SelectedIndexChanged;
            cbDisciplinesDirection.SelectedIndexChanged += cbDisciplinesDirection_SelectedIndexChanged;
            btnAddGroup.Click += btnAddGroup_Click;
            btnAddStudent.Click += btnAddStudent_Click;
            btnAddTeacher.Click += btnAddTeacher_Click;
        }
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        void LoadDataToComboBox(ComboBox comboBox)
        {
            string table = comboBox.Name.Substring(Array.FindLastIndex<char>(comboBox.Name.ToCharArray(), Char.IsUpper)) + "s";
            string dictionary_name = $"d_{table}".ToLower();

            Console.WriteLine("\n====================================================================\n");
            Console.WriteLine(table);
            Console.WriteLine(dictionary_name);
            Console.WriteLine("\n====================================================================\n");

            d_trees[dictionary_name] = connector.LoadDictionary(table);

            comboBox.Items.Clear();
            comboBox.Items.Add("Все");
            foreach (KeyValuePair<string, int> i in d_trees[dictionary_name])
            {
                comboBox.Items.Add(i.Key);
            }

            comboBox.SelectedIndex = 0;
        }
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = tabControl.SelectedIndex;

            switch (i)
            {
                case 0: UpdateStudentsFilter(); break;
                case 1: cbGroupsDirection_SelectedIndexChanged(null, null); break;
                case 3: cbDisciplinesDirection_SelectedIndexChanged(null, null); break;
                default:
                    tables[i].DataSource = connector.Select(queries[i].ToString());
                    break;
            }

            toolStripStatusLabel.Text = $"{statusBarSignatures[i]}: {tables[i].RowCount - 1}";
        }

        private void UpdateStudentsFilter()
        {
            string baseQuery = queries[0].ToString();
            string extra = "";

            if (cbStudentsGroup.SelectedItem?.ToString() != "Все" && cbStudentsGroup.SelectedIndex != -1)
            {
                int groupId = d_trees["d_groups"][cbStudentsGroup.SelectedItem.ToString()];
                extra += $" AND [group]={groupId}";
            }
            if (cbStudentsDirection.SelectedItem?.ToString() != "Все" && cbStudentsDirection.SelectedIndex != -1)
            {
                int dirId = d_trees["d_directions"][cbStudentsDirection.SelectedItem.ToString()];
                extra += $" AND direction_id={dirId}";
            }

            tables[0].DataSource = connector.Select(baseQuery + extra);
            toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {tables[0].RowCount - 1}";
        }

        private void cbStudentsGroup_SelectedIndexChanged(object sender, EventArgs e) => UpdateStudentsFilter();
        private void cbStudentsDirection_SelectedIndexChanged(object sender, EventArgs e) => UpdateStudentsFilter();

        private void cbDisciplinesDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDisciplinesDirection.SelectedItem?.ToString() == "Все")
            {
                tables[3].DataSource = connector.Select(queries[3].ToString());
            }
            else if (cbDisciplinesDirection.SelectedIndex != -1)
            {
                int dirId = d_trees["d_directions"][cbDisciplinesDirection.SelectedItem.ToString()];

                string sql = $@"
                    SELECT d.discipline_id, d.discipline_name, d.number_of_lessons
                    FROM Disciplines d
                    INNER JOIN DisciplinesDirectionsRelation r ON d.discipline_id = r.discipline
                    WHERE r.direction = {dirId}
                    ORDER BY d.discipline_name";

                tables[3].DataSource = connector.Select(sql);
            }
            toolStripStatusLabel.Text = $"{statusBarSignatures[3]}: {tables[3].RowCount - 1}";
        }

        private void cbGroupsDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbGroupsDirection.SelectedItem?.ToString() == "Все")
            {
                tables[1].DataSource = connector.Select(queries[1].ToString());
            }
            else if (cbGroupsDirection.SelectedIndex != -1)
            {
                int dirId = d_trees["d_directions"][cbGroupsDirection.SelectedItem.ToString()];
                tables[1].DataSource = connector.Select(
                    queries[1].ToString() + $" AND direction={dirId}");
            }
            toolStripStatusLabel.Text = $"{statusBarSignatures[1]}: {tables[1].RowCount - 1}";
        }
        private void btnAddGroup_Click(object sender, EventArgs e)
        {
            string groupName = Prompt("Название группы:", "Добавить группу");
            if (string.IsNullOrWhiteSpace(groupName)) return;

            string dirName = ChooseItem("Выберите направление:", d_trees["d_directions"]);
            if (string.IsNullOrEmpty(dirName)) return;
            int dirId = d_trees["d_directions"][dirName];

            int groupId = connector.GetNextPrimaryKey("Groups");

            string weekdaysInput = Prompt(
                "Значение для дней недели (tinyint):\n\n1 = Пн\n2 = Вт\n3 = Пн+Вт\n13 = Пн+Ср+Чт и т.д.\n\nВведите число:",
                "Добавить группу", "1");

            if (!int.TryParse(weekdaysInput, out int weekdays))
            {
                MessageBox.Show("Нужно ввести число (tinyint)!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string startTime = Prompt("Время начала занятий (HH:mm):", "Добавить группу", "09:00");
            string startDate = Prompt("Дата начала (гггг-мм-дд):", "Добавить группу", DateTime.Now.ToString("yyyy-MM-dd"));

            string cmd = $@"
        INSERT INTO Groups (group_id, group_name, weekdays, start_time, start_date, direction)
        VALUES ({groupId}, N'{groupName.Replace("'", "''")}', {weekdays}, '{startTime}', '{startDate}', {dirId})";

            Console.WriteLine("=== SQL для вставки группы ===\n" + cmd);
            connector.Insert(cmd);

            // Обновляем списки
            d_trees["d_groups"] = connector.LoadDictionary("Groups");
            LoadDataToComboBox(cbStudentsGroup);
            cbGroupsDirection_SelectedIndexChanged(null, null);

            MessageBox.Show($"Группа «{groupName}» успешно добавлена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAddStudent_Click(object sender, EventArgs e)
        {
            string lastName = Prompt("Фамилия:", "Добавить студента");
            if (string.IsNullOrWhiteSpace(lastName)) return;

            string firstName = Prompt("Имя:", "Добавить студента");
            string middleName = Prompt("Отчество:", "Добавить студента");

            string birthDateStr = Prompt(
                "Дата рождения (гггг-мм-дд):",
                "Добавить студента",
                DateTime.Now.AddYears(-18).ToString("yyyy-MM-dd"));

            if (!DateTime.TryParse(birthDateStr, out DateTime birthDate))
            {
                MessageBox.Show("Неверный формат даты рождения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string groupName = ChooseItem("Выберите группу:", d_trees["d_groups"]);
            if (string.IsNullOrEmpty(groupName)) return;
            int groupId = d_trees["d_groups"][groupName];

            string cmd = $@"
        INSERT INTO Students (last_name, first_name, middle_name, birth_date, [group])
        VALUES (N'{lastName.Replace("'", "''")}', 
                N'{firstName.Replace("'", "''")}', 
                N'{middleName.Replace("'", "''")}', 
                '{birthDate:yyyy-MM-dd}', 
                {groupId})";

            connector.Insert(cmd);

            UpdateStudentsFilter();
            MessageBox.Show($"Студент «{lastName} {firstName}» успешно добавлен!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAddTeacher_Click(object sender, EventArgs e)
        {
            string lastName = Prompt("Фамилия:", "Добавить преподавателя");
            if (string.IsNullOrWhiteSpace(lastName)) return;

            string firstName = Prompt("Имя:", "Добавить преподавателя");
            string middleName = Prompt("Отчество:", "Добавить преподавателя");

            string birthDateStr = Prompt(
                "Дата рождения (гггг-мм-дд):",
                "Добавить преподавателя",
                DateTime.Now.AddYears(-35).ToString("yyyy-MM-dd"));

            if (!DateTime.TryParse(birthDateStr, out DateTime birthDate))
            {
                MessageBox.Show("Неверный формат даты рождения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int teacherId = connector.GetNextPrimaryKey("Teachers");

            string cmd = $@"
        INSERT INTO Teachers (teacher_id, last_name, first_name, middle_name, birth_date)
        VALUES ({teacherId}, 
                N'{lastName.Replace("'", "''")}', 
                N'{firstName.Replace("'", "''")}', 
                N'{middleName.Replace("'", "''")}', 
                '{birthDate:yyyy-MM-dd}')";

            connector.Insert(cmd);

            if (tabControl.SelectedIndex == 4)
                tables[4].DataSource = connector.Select(queries[4].ToString());

            MessageBox.Show($"Преподаватель «{lastName} {firstName}» успешно добавлен!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string Prompt(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label() { Left = 20, Top = 30, Width = 350, Text = text };
            TextBox textBox = new TextBox() { Left = 20, Top = 60, Width = 340, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 220, Width = 80, Top = 110, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Отмена", Left = 310, Width = 80, Top = 110, DialogResult = DialogResult.Cancel };

            prompt.Controls.Add(label);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : "";
        }

        private string ChooseItem(string title, Dictionary<string, int> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                MessageBox.Show("Список пуст!", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            Form form = new Form()
            {
                Text = title,
                Size = new Size(420, 180),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            ComboBox cmb = new ComboBox()
            {
                Location = new Point(20, 20),
                Size = new Size(360, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmb.Items.AddRange(dictionary.Keys.ToArray());
            cmb.SelectedIndex = 0;
            form.Controls.Add(cmb);

            Button btnOk = new Button() { Text = "OK", Location = new Point(200, 70), Width = 90 };
            btnOk.Click += (s, e) => form.DialogResult = DialogResult.OK;
            form.Controls.Add(btnOk);

            Button btnCancel = new Button() { Text = "Отмена", Location = new Point(300, 70), Width = 90, DialogResult = DialogResult.Cancel };
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return (form.ShowDialog() == DialogResult.OK) ? cmb.SelectedItem?.ToString() : null;
        }
    }
}