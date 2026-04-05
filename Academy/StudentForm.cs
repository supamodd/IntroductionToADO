using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Configuration;
using DBtools;                    // ← добавил

namespace Academy
{
    public partial class StudentForm : HumanForm
    {
        private Connector connector;   // ← добавил

        public StudentForm()
        {
            InitializeComponent();

            // создаём подключение один раз
            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);

            cbStudentsGroup.DataSource = connector.Select("*", "Groups");
            cbStudentsGroup.DisplayMember = "group_name";
            cbStudentsGroup.ValueMember = "group_id";
        }

        public StudentForm(int id) : this()
        {
            DataTable data = connector.Select("*", "Students", $"stud_id={id}");
            labelID.Text = $"ID: {id}";
            rtbLastName.Text = data.Rows[0]["last_name"].ToString();
            rtbFirstName.Text = data.Rows[0]["first_name"].ToString();
            rtbMiddleName.Text = data.Rows[0]["middle_name"].ToString();
            dtpBirthDate.Value = Convert.ToDateTime(data.Rows[0]["birth_date"].ToString());
            rtbEmail.Text = data.Rows[0]["email"].ToString();
            rtbPhone.Text = data.Rows[0]["phone"].ToString();
            cbStudentsGroup.SelectedValue = Convert.ToInt32(data.Rows[0]["group"].ToString());
            pictureBoxPhoto.Image = connector.DownloadPhoto(id, "Students", "photo");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            int id = labelID.Text.Split(':').Last() == "" ? 0 : Convert.ToInt32(labelID.Text.Split(':').Last());

            Academy.Models.Student student = new Models.Student
            (
                id,
                rtbLastName.Text,
                rtbFirstName.Text,
                rtbMiddleName.Text,
                dtpBirthDate.Value.ToString("yyyy-MM-dd"),
                rtbEmail.Text,
                rtbPhone.Text,
                pictureBoxPhoto.Image,
                Convert.ToInt32(cbStudentsGroup.SelectedValue)
            );

            if (student.id == 0)
            {
                connector.Insert($"INSERT Students({student.GetNames()}) VALUES ({student})");
                student.id = (int)connector.Scalar($"SELECT stud_id FROM Students WHERE {student.GetCondition()}");
            }
            else
            {
                connector.Update($"UPDATE Students SET {student.ToStringUpdate()} WHERE stud_id={student.id}");
            }

            if (student.photo != null)
                connector.UploadPhoto(student.SerializePhoto(), student.id, "photo", "Students");
        }
    }
}