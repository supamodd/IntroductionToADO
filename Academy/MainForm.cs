using DBtools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Academy
{
    public partial class MainForm : Form
    {
        Query[] queries =
        {
           new Query
                (
                "Students,Groups,Directions",
                "stud_id, [Student] = FORMATMESSAGE(N'%s %s %s',last_name,first_name,middle_name),group_name,direction_name",
                "[group]=group_id AND direction=direction_id"
                ),
            new Query
                (
                "Groups,Directions",
                "group_name,weekdays,start_time,start_date,direction_name",
                "direction = direction_id"
                ),
            new Query("Directions", "*"),
            new Query("Disciplines","*"),
            new Query("Teachers",   "*")
        };
        Connector connector;
        //Connector movies_connector;
        DataGridView[] tables = null;
        ///////////////////////////////////

        Dictionary<string, int> d_directions = null;
        Dictionary<string, Dictionary<string, int>> d_trees = null;
        string[] statusBarSignatures =
        {
            "Количество студетов",
            "Количество групп",
            "Количество направлеоий",
            "Количество дисциплин",
            "Количество преподавателей"
        };
        public MainForm()
        {
            InitializeComponent();
            tables = new DataGridView[] { dgvStudents, dgvGroups, dgvDirections, dgvDisciplines, dgvTeachers };
            AllocConsole();
            //connector = new Connector("Data Source=DESKTOP-MU2UJAA\\SQLEXPRESS;"
            //                            + "Initial Catalog=SPU_411_Import;"
            //                            + "Integrated Security=True;"
            //                            + "Connect Timeout=30;"
            //                            + "Encrypt=True;"
            //                            + "TrustServerCertificate=True;"
            //                            + "ApplicationIntent=ReadWrite;"
            //                            + "MultiSubnetFailover=False");
            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);
            //movies_connector = new Connector("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            //dgvDirections.DataSource = movies_connector.Select("SELECT [№\nп/п] = movie_id,[Название фильма] = title,[Режиссер] = FORMATMESSAGE(N'%s %s', first_name,last_name) FROM Movies, Directors WHERE director = director_id ORDER BY movie_id");
            //dgvDirections.DataSource = movies_connector.Select("SELECT * FROM Movies");
            tabControl_SelectedIndexChanged(tabControl, null);

            d_trees = new Dictionary<string, Dictionary<string, int>>();
            d_trees.Add(nameof(d_directions), d_directions);
            LoadDataToComboBox(cbGroupsDirection);
            LoadDataToComboBox(cbStudentsGroup);
            LoadDataToComboBox(cbStudentsDirection);
            LoadDataToComboBox(cbDisciplinesDirection);
            dgvStudents.CellDoubleClick += dgvStudents_CellDoubleClick;
        }
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
        void LoadDataToComboBox(ComboBox comboBox)
        {
            string table = comboBox.Name.Substring(Array.FindLastIndex<char>(comboBox.Name.ToCharArray(), Char.IsUpper)) + "s";
            string dictionary_name = $"d_{table}".ToLower();
            Console.WriteLine("======================================");
            Console.WriteLine(table);
            Console.WriteLine(dictionary_name);
            Console.WriteLine(nameof(dictionary_name));
            Console.WriteLine("======================================");
            d_trees[dictionary_name] = connector.LoadDictionary(table);
            foreach (KeyValuePair<string, int> i in d_trees[dictionary_name])
            {
                comboBox.Items.Add(i.Key);
            }
        }
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Console.WriteLine($"{(sender as TabControl).SelectedIndex}\t{tabControl.SelectedTab.Text}");

            //Работает при модификаторе доступа у dvg - public
            /*DataGridView dgv = this.GetType().GetField($"dgv{tabControl.SelectedTab.Text}").GetValue(this) as DataGridView;
            dgv.DataSource = connector.Select($"SELECT * FROM {tabControl.SelectedTab.Text}");
            toolStripStatusLabel.Text = $"Количество записей: {dgv.RowCount - 1}";*/

            int i = tabControl.SelectedIndex;
            tables[i].DataSource = connector.Select(queries[i].ToString());
            toolStripStatusLabel.Text = $"{statusBarSignatures[i]}: {tables[i].RowCount - 1}";
        }
        private void cbGroupsDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbGroupsDirection.SelectedIndex != -1)
                tables[1].DataSource = connector.Select
                    (
                    queries[1].ToString() + $" AND direction = {d_trees["d_directions"][cbGroupsDirection.SelectedItem.ToString()]}"
                    );
        }
        private void cbStudentsDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbStudentsGroup.Items.Clear();
            d_trees["d_groups"] = connector.
                LoadDictionary("Groups", $"direction={d_trees["d_directions"][cbStudentsDirection.SelectedItem.ToString()]}");
            cbStudentsGroup.Items.AddRange(d_trees["d_groups"].Keys.ToArray());
            dgvStudents.DataSource = connector.
                Select(queries[0].ToString() + $" AND direction={d_trees["d_directions"][cbStudentsDirection.SelectedItem.ToString()]}");
            toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {dgvStudents.RowCount - 1}";
        }
        private void cbStudentsGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            dgvStudents.DataSource = connector.
                Select(queries[0].ToString() + $" AND [group]={d_trees["d_groups"][cbStudentsGroup.SelectedItem.ToString()]}");
            toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {dgvStudents.RowCount - 1}";
        }

        private void buttonAddStudent_Click(object sender, EventArgs e)
        {
            StudentForm form = new StudentForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                tables[0].DataSource = connector.Select(queries[0].ToString());

                toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {tables[0].RowCount - 1}";
            }
        }

        private void buttonAddTeacher_Click(object sender, EventArgs e)
        {
            TeacherForm form = new TeacherForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                tables[4].DataSource = connector.Select(queries[4].ToString());

                toolStripStatusLabel.Text = $"{statusBarSignatures[4]}: {tables[4].RowCount - 1}";
            }
        }

        private void dgvStudents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvStudents.Rows[e.RowIndex];

            if (row.Cells[0].Value == null || row.Cells[0].Value == DBNull.Value)
            {
                MessageBox.Show("Не удалось определить ID студента", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int studentId = Convert.ToInt32(row.Cells[0].Value);
            string studentName = row.Cells[1].Value?.ToString() ?? "Студент";

            try
            {
                DataTable dt = connector.Select($"SELECT photo FROM Students WHERE stud_id = {studentId}");

                if (dt.Rows.Count == 0 || dt.Rows[0]["photo"] == DBNull.Value)
                {
                    MessageBox.Show($"У студента {studentName} нет фото.", "Фото отсутствует",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                object photoObj = dt.Rows[0]["photo"];
                byte[] photoBytes = ConvertPhotoToBytes(photoObj);

                if (photoBytes == null || photoBytes.Length == 0)
                {
                    MessageBox.Show($"У студента {studentName} нет фото (или данные повреждены).", "Фото отсутствует",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (MemoryStream ms = new MemoryStream(photoBytes))
                {
                    Image photoImage = Image.FromStream(ms);
                    ShowPhotoViewer(studentName, photoImage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фото:\n{ex.Message}", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private byte[] ConvertPhotoToBytes(object photoValue)
        {
            if (photoValue == null || photoValue == DBNull.Value)
                return null;
            if (photoValue is byte[] bytes)
                return bytes;
            MessageBox.Show(
                $"Неизвестный тип данных: {photoValue.GetType().FullName}\n" +
                $"Значение: {photoValue.ToString().Substring(0, Math.Min(photoValue.ToString().Length, 200))}\n\n" +
                "Пришли мне это сообщение.",
                "Диагностика photo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        private void ShowPhotoViewer(string studentName, Image photo)
        {
            Form viewer = new Form
            {
                Text = $"Фото студента: {studentName}",
                Size = new Size(800, 650),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            PictureBox pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = photo
            };

            viewer.Controls.Add(pb);
            viewer.ShowDialog();
        }
    }
}