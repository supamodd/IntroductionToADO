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
using System.Configuration;

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
                "stud_id,last_name,first_name,middle_name,group_name,direction_name",
                "[group]=group_id AND direction=direction_id"
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
            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);
            movies_connector = new Connector("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            //dgvDirections.DataSource = movies_connector.Select("SELECT * FROM Movies");
            //toolStripStatusLabel.Text = $"Колчиество направлений обучения: {connector.Scalar("SELECT COUNT(*) FROM Directions")}";
            //tabControl.SelectedIndex = 1;
            tabControl_SelectedIndexChanged(tabControl, null);
            dgvTeachers.CellMouseDoubleClick += dgvTeachers_CellMouseDoubleClick;

            d_trees = new Dictionary<string, Dictionary<string, int>>();
            d_trees.Add(nameof(d_directions), d_directions);
            LoadDataToComboBox(cbGroupsDirection);
            LoadDataToComboBox(cbStudentsGroup);
            LoadDataToComboBox(cbStudentsDirection);
            LoadDataToComboBox(cbDisciplinesDirection);
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
            Console.WriteLine(nameof(comboBox));
            Console.WriteLine("\n====================================================================\n");
            d_trees[dictionary_name] = connector.LoadDictionary(table);
            foreach (KeyValuePair<string, int> i in d_trees[dictionary_name])
            {
                comboBox.Items.Add(i.Key);
            }
        }
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = tabControl.SelectedIndex;
            tables[i].DataSource = connector.Select(queries[i].ToString());
            toolStripStatusLabel.Text = $"{statusBarSignatures[i]}: {tables[i].RowCount - 1}";
            if (i == 0) 
            {
                buttonAdd.Visible = true;
                buttonAdd.Parent = tabPageStudents;
                buttonAdd.Location = new System.Drawing.Point(666, 12);
                buttonAdd.Text = "Добавить студента";
            }
            else if (i == 4)
            {
                buttonAdd.Visible = true;
                buttonAdd.Parent = tabPageTeachers;
                buttonAdd.Location = new System.Drawing.Point(666, 12);
                buttonAdd.Text = "Добавить преподавателя";
            }
            else
            {
                buttonAdd.Visible = false;
            }
        }

        private void cbGroupsDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbGroupsDirection.SelectedIndex != -1)
                tables[1].DataSource = connector.Select
                    (
                queries[1].ToString() + $" AND direction={d_trees["d_directions"][cbGroupsDirection.SelectedItem.ToString()]}"
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
            dgvStudents.DataSource = connector
                .Select(queries[0].ToString() + $" AND [group]={d_trees["d_groups"][cbStudentsGroup.SelectedItem.ToString()]}");
            toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {dgvStudents.RowCount - 1}";
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0) 
            {
                StudentForm form = new StudentForm();
                form.ShowDialog();
            }
            else if (tabControl.SelectedIndex == 4)
            {
                TeacherForm form = new TeacherForm();
                form.ShowDialog();
            }

            tabControl_SelectedIndexChanged(tabControl, null);
        }

        private void dgvStudents_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int i = Convert.ToInt32(dgvStudents.Rows[e.RowIndex].Cells[0].Value);
            StudentForm form = new StudentForm(i);
            form.ShowDialog();
        }

        private void dgvTeachers_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(dgvTeachers.Rows[e.RowIndex].Cells[0].Value);
            TeacherForm form = new TeacherForm(id);
            form.ShowDialog();
            tabControl_SelectedIndexChanged(tabControl, null);
        }
    }
}