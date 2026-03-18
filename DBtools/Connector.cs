using System;
using System.Data;
using System.Data.SqlClient;

namespace DBtools
{
    public class Connector
    {
        private readonly string connection_string;

        public Connector(string connection_string)
        {
            this.connection_string = connection_string;
        }
        public DataTable Select(string cmd)
        {
            DataTable table = new DataTable();

            using (SqlConnection conn = new SqlConnection(connection_string))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(cmd, conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            table.Columns.Add(reader.GetName(i));

                        while (reader.Read())
                        {
                            DataRow row = table.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[i] = reader[i];
                                Console.Write(reader[i].ToString().PadRight(28));
                            }
                            Console.WriteLine();
                            table.Rows.Add(row);
                        }
                    }
                }
            } 

            return table;
        }

        public DataTable Select(string fields, string tables, string condition = "")
        {
            string cmd = $"SELECT {fields} FROM {tables}";
            if (condition != "") cmd += $" WHERE {condition}";
            cmd += ";";
            return Select(cmd);
        }

        public object Scalar(string cmd)
        {
            using (SqlConnection conn = new SqlConnection(connection_string))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(cmd, conn))
                {
                    return command.ExecuteScalar();
                }
            }
        }

        public object GetPrimaryKey(string cmd)
        {
            return Scalar(cmd);
        }

        public object GetPrimaryKey(string table, string fields, string values)
        {
            string[] s_fields = fields.Split(',');
            string[] s_values = values.Split(',');
            if (s_fields.Length != s_values.Length) return null;

            string condition = "";
            for (int i = 0; i < s_fields.Length; i++)
            {
                if (s_fields[i].Contains("_id")) continue;
                string value = s_values[i].Trim();
                condition += (value.Length > 1 && value[0] != 'N' && value[1] != '\'') ?
                    $"{s_fields[i].Trim()}=N'{s_values[i].Trim()}'" :
                    $"{s_fields[i].Trim()}={s_values[i].Trim()}";
                if (i != s_values.Length - 1) condition += " AND ";
            }

            string cmd = $"SELECT {GetPrimaryKeyColumn(table)} FROM {table} WHERE {condition}";
            return Scalar(cmd);
        }

        public void Insert(string cmd)
        {
            Console.WriteLine(GetTableFromInsert(cmd));
            Console.WriteLine(GetFieldFromInsert(cmd));
            Console.WriteLine(GetValuesFromInsert(cmd));

            if (GetPrimaryKey(GetTableFromInsert(cmd), GetFieldFromInsert(cmd), GetValuesFromInsert(cmd)) != null)
                return;

            using (SqlConnection conn = new SqlConnection(connection_string))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(cmd, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Insert(string table, string values)
        {
            string cmd = $"INSERT INTO {table} VALUES ({values})";
            Insert(cmd);
        }

        public void Update(string cmd)
        {
            using (SqlConnection conn = new SqlConnection(connection_string))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(cmd, conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public string GetTableFromInsert(string cmd)
        {
            string[] parts = cmd.Split(' ', '(', ')');
            return parts[1];
        }

        public string GetFieldFromInsert(string cmd)
        {
            string[] parts = cmd.Split('(', ')');
            return parts[1];
        }

        public string GetValuesFromInsert(string cmd)
        {
            string[] parts = cmd.Split('(', ')');
            return parts[3];
        }

        public string GetPrimaryKeyColumn(string table)
        {
            string cmd = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WHERE CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME=N'{table}' AND CONSTRAINT_TYPE=N'PRIMARY KEY')";
            return (string)Scalar(cmd);
        }

        public int GetLastPrimaryKey(string table)
        {
            return Convert.ToInt32(Scalar($"SELECT MAX({GetPrimaryKeyColumn(table)}) FROM {table}"));
        }

        public int GetNextPrimaryKey(string table)
        {
            return GetLastPrimaryKey(table) + 1;
        }
    }
}