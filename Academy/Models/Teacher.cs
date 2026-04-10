using System;
using System.Drawing;

namespace Academy.Models
{
    class Teacher : Human
    {
        public string work_since { get; set; }

        public Teacher(
            int id,
            string last_name,
            string first_name,
            string middle_name,
            string birth_date,
            string work_since,
            Image photo)
            : base(id, last_name, first_name, middle_name, birth_date, "", "", photo)
        {
            this.work_since = work_since;
        }

        public override string GetNames()
        {
            return "teacher_id,last_name,first_name,middle_name,birth_date,work_since";
        }

        public override string ToString()
        {
            return $"{id},N'{last_name}',N'{first_name}',N'{middle_name ?? ""}',N'{birth_date}',N'{work_since}'";
        }

        public override string ToStringUpdate()
        {
            return $"last_name=N'{last_name}',first_name=N'{first_name}',middle_name=N'{middle_name ?? ""}',birth_date=N'{birth_date}',work_since=N'{work_since}'";
        }

        public string GetCondition()
        {
            return $"last_name=N'{last_name}' AND first_name=N'{first_name}' AND middle_name=N'{middle_name ?? ""}' AND birth_date=N'{birth_date}'";
        }
    }
}