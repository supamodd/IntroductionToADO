using DBtools;
using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Academy
{
    public partial class MainForm : Form
    {
        Query[] queries =
        {
            new Query("Students,Groups,Directions", "last_name,first_name,middle_name,group_name,direction_name", "[group]=group_id AND direction=direction_id"),
            new Query("Groups,Directions", "group_name,weekdays,start_time,start_date,direction_name", "direction=direction_id"),
            new Query("Directions", "*"),
            new Query("Disciplines","*"),
            new Query("Teachers", "*"),
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

        public MainForm()
        {
            InitializeComponent();

            tables = new DataGridView[] { dgvStudents, dgvGroups, dgvDirections, dgvDisciplines, dgvTeachers };

            AllocConsole();

            connector = new DBtools.Connector(
                "Data Source=SUPAMODDPC\\SQLEXPRESS;Initial Catalog=SPU_411_Import;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            movies_connector = new Connector("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            // ====================== ИНИЦИАЛИЗАЦИЯ ФИЛЬТРОВ ======================
            LoadDirections(cmbDirectionsGroups);
            LoadDirections(cmbDirectionDisciplines);
            LoadDirections(cmbDirectionStudents);
            LoadGroupsForDirection(cmbGroupStudents, null);

            // Подписки на события
            cmbDirectionsGroups.SelectedIndexChanged += (s, e) => ReloadFilteredGroups();
            cmbDirectionDisciplines.SelectedIndexChanged += (s, e) => ReloadFilteredDisciplines();

            cmbDirectionStudents.SelectedIndexChanged += (s, e) =>
            {
                LoadGroupsForDirection(cmbGroupStudents, cmbDirectionStudents.SelectedValue);
                ReloadFilteredStudents();
            };
            cmbGroupStudents.SelectedIndexChanged += (s, e) => ReloadFilteredStudents();

            dgvDisciplines.SelectionChanged += DgvDisciplines_SelectionChanged;
            dgvTeachers.SelectionChanged += DgvTeachers_SelectionChanged;

            // Первая загрузка
            tabControl_SelectedIndexChanged(tabControl, null);
        }

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        // ====================== ОБРАБОТЧИК ВКЛАДОК ======================
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = tabControl.SelectedIndex;

            switch (i)
            {
                case 0: ReloadFilteredStudents(); break;
                case 1: ReloadFilteredGroups(); break;
                case 3: ReloadFilteredDisciplines(); break;
                default:
                    tables[i].DataSource = connector.Select(queries[i].ToString());
                    toolStripStatusLabel.Text = $"{statusBarSignatures[i]}: {tables[i].RowCount - 1}";
                    break;
            }
        }

        // ====================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ======================
        private void LoadDirections(ComboBox cmb)
        {
            var dt = connector.Select("SELECT direction_id, direction_name FROM Directions ORDER BY direction_name");
            cmb.DataSource = dt;
            cmb.DisplayMember = "direction_name";
            cmb.ValueMember = "direction_id";
            cmb.SelectedIndex = -1;
        }

        private void LoadGroupsForDirection(ComboBox cmb, object dirValue)
        {
            cmb.DataSource = null;
            string sql = dirValue == null || dirValue == DBNull.Value
                ? "SELECT group_id, group_name FROM Groups ORDER BY group_name"
                : $"SELECT group_id, group_name FROM Groups WHERE direction = {dirValue} ORDER BY group_name";

            var dt = connector.Select(sql);
            cmb.DataSource = dt;
            cmb.DisplayMember = "group_name";
            cmb.ValueMember = "group_id";
            cmb.SelectedIndex = -1;
        }

        // ====================== ФИЛЬТРЫ ======================
        private void ReloadFilteredGroups()
        {
            int? dirId = cmbDirectionsGroups.SelectedValue != null ? (int?)Convert.ToInt32(cmbDirectionsGroups.SelectedValue) : null;
            string sql = "SELECT group_name,weekdays,start_time,start_date,direction_name FROM Groups,Directions WHERE direction=direction_id";
            if (dirId.HasValue && dirId > 0)
                sql += $" AND direction_id = {dirId}";

            dgvGroups.DataSource = connector.Select(sql);
            toolStripStatusLabel.Text = $"{statusBarSignatures[1]}: {dgvGroups.RowCount - 1}";
        }

        private void ReloadFilteredDisciplines()
        {
            int? dirId = cmbDirectionDisciplines.SelectedValue != null
                ? Convert.ToInt32(cmbDirectionDisciplines.SelectedValue)
                : (int?)null;

            string sql;

            if (dirId.HasValue && dirId > 0)
            {
                sql = $@"
            SELECT d.discipline_id, d.discipline_name, d.number_of_lessons
            FROM Disciplines d
            INNER JOIN DisciplinesDirectionsRelation r ON d.discipline_id = r.discipline
            WHERE r.direction = {dirId}
            ORDER BY d.discipline_name";
            }
            else
            {
                sql = "SELECT discipline_id, discipline_name, number_of_lessons FROM Disciplines ORDER BY discipline_name";
            }

            dgvDisciplines.DataSource = connector.Select(sql);
            toolStripStatusLabel.Text = $"{statusBarSignatures[3]}: {dgvDisciplines.RowCount - 1}";
        }

        private void ReloadFilteredStudents()
        {
            int? dirId = null;
            if (cmbDirectionStudents.SelectedValue is DataRowView drvDir)
                dirId = drvDir["direction_id"] as int?;
            else if (cmbDirectionStudents.SelectedValue != null)
                dirId = Convert.ToInt32(cmbDirectionStudents.SelectedValue);

            int? groupId = null;
            if (cmbGroupStudents.SelectedValue is DataRowView drvGrp)
                groupId = drvGrp["group_id"] as int?;
            else if (cmbGroupStudents.SelectedValue != null)
                groupId = Convert.ToInt32(cmbGroupStudents.SelectedValue);

            string where = "[group]=group_id AND direction=direction_id";
            if (dirId.HasValue && dirId > 0) where += $" AND direction = {dirId}";
            if (groupId.HasValue && groupId > 0) where += $" AND [group] = {groupId}";

            string sql = $"SELECT last_name,first_name,middle_name,group_name,direction_name FROM Students,Groups,Directions WHERE {where}";

            dgvStudents.DataSource = connector.Select(sql);
            toolStripStatusLabel.Text = $"{statusBarSignatures[0]}: {dgvStudents.RowCount - 1}";
        }

        // ====================== 4 и 5: СВЯЗАННЫЕ СПИСКИ ======================
        private void DgvDisciplines_SelectionChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex != 3 || dgvDisciplines.CurrentRow == null)
            {
                dgvTeachersForDiscipline.DataSource = null;
                return;
            }

            // Важно: имя колонки в DataGridView = discipline_id (как в базе)
            var cell = dgvDisciplines.CurrentRow.Cells["discipline_id"];
            if (cell?.Value == null || cell.Value == DBNull.Value) return;

            int discId = Convert.ToInt32(cell.Value);

            string sql = $@"
        SELECT t.last_name, t.first_name, t.middle_name 
        FROM Teachers t 
        INNER JOIN TeachersDisciplinesRelation td ON t.teacher_id = td.teacher 
        WHERE td.discipline = {discId}";

            dgvTeachersForDiscipline.DataSource = connector.Select(sql);
        }

        private void DgvTeachers_SelectionChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex != 4 || dgvTeachers.CurrentRow == null)
            {
                dgvDisciplinesForTeacher.DataSource = null;
                return;
            }

            // Важно: имя колонки в DataGridView = teacher_id (как в базе)
            var cell = dgvTeachers.CurrentRow.Cells["teacher_id"];
            if (cell?.Value == null || cell.Value == DBNull.Value) return;

            int teachId = Convert.ToInt32(cell.Value);

            string sql = $@"
        SELECT d.discipline_name 
        FROM Disciplines d 
        INNER JOIN TeachersDisciplinesRelation td ON d.discipline_id = td.discipline 
        WHERE td.teacher = {teachId}";

            dgvDisciplinesForTeacher.DataSource = connector.Select(sql);
        }
    }
}