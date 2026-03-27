using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DBtools
{
    public class Connector
    {
        string connection_string;
        SqlConnection connection;
        //public int GetPrimaryKey(string table, string condition)
        public void Insert(string cmd)
        {
            Console.WriteLine("=== INSERT ===");
            Console.WriteLine(cmd);

            connection.Open();
            SqlCommand command = new SqlCommand(cmd, connection);
            command.ExecuteNonQuery();
            connection.Close();

            Console.WriteLine("Запись успешно добавлена");
        }
        public object GetPrimaryKey(string table, string fields, string values)
        {
            var fieldList = fields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(f => f.Trim()).ToList();

            var valueList = values.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(v => v.Trim()).ToList();

            if (fieldList.Count != valueList.Count) return null;

            var conditions = new List<string>();

            for (int i = 0; i < fieldList.Count; i++)
            {
                string field = fieldList[i];
                string val = valueList[i];

                if (field.ToLower().Contains("_id") ||
                    field == "[group]" ||
                    field == "direction")
                    continue;

                string part;
                if (val.StartsWith("N'") || val.StartsWith("'"))
                {
                    part = $"{field} = {val}";
                }
                else if (int.TryParse(val, out _) || val.Contains(":") || val.Contains("-"))
                {
                    part = $"{field} = {val}";
                }
                else
                {
                    part = $"{field} = N'{val.Replace("'", "''")}'";
                }

                conditions.Add(part);
            }

            if (conditions.Count == 0)
                return null;

            string condition = string.Join(" AND ", conditions);

            string pkColumn = GetPrimaryKeyColumn(table);
            string cmd = $"SELECT {pkColumn} FROM {table} WHERE {condition}";

            return Scalar(cmd);
        }

        public Connector(string connection_string)
        {
            this.connection_string = connection_string;
            this.connection = new SqlConnection(connection_string);
        }
        public DataTable Select(string fields, string tables, string condition = "")
        {
            string cmd = $"SELECT {fields} FROM {tables}";
            if (condition != "") cmd += $" WHERE {condition}";
            cmd += ";";

            return Select(cmd);
        }
        public DataTable Select(string cmd)
        {
            DataTable table = new DataTable();
            connection.Open();
            SqlCommand command = new SqlCommand(cmd, connection);
            SqlDataReader reader = command.ExecuteReader();
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i));
            while (reader.Read())
            {
                DataRow row = table.NewRow();
                //Console.WriteLine($"{reader[0]}\t{reader[1]} {reader[2]}");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader[i];
                    Console.Write(reader[i].ToString().PadRight(28));
                }
                Console.WriteLine();
                table.Rows.Add(row);
            }

            reader.Close();
            connection.Close();
            return table;
        }
        public string GetTableFromInsert(string cmd)
        {
            string upper = cmd.ToUpper().Trim();
            int pos = upper.IndexOf("INSERT INTO");
            if (pos == -1) return "";
            pos += 12;

            while (pos < upper.Length && char.IsWhiteSpace(upper[pos])) pos++;

            int end = pos;
            while (end < upper.Length && !char.IsWhiteSpace(upper[end]) && upper[end] != '(')
                end++;

            return cmd.Substring(pos, end - pos).Trim();
        }
        public string GetFieldFromInsert(string cmd)
        {
            int firstOpen = cmd.IndexOf('(');
            if (firstOpen == -1) return "";
            int firstClose = cmd.IndexOf(')', firstOpen);
            if (firstClose == -1) return "";
            return cmd.Substring(firstOpen + 1, firstClose - firstOpen - 1).Trim();
        }
        public string GetValuesFromInsert(string cmd)
        {
            int lastClose = cmd.LastIndexOf(')');
            if (lastClose == -1) return "";
            int lastOpen = cmd.LastIndexOf('(', lastClose);
            if (lastOpen == -1) return "";
            return cmd.Substring(lastOpen + 1, lastClose - lastOpen - 1).Trim();
        }
       
        public object Scalar(string cmd)
        {
            SqlCommand command = new SqlCommand(cmd, connection);
            connection.Open();
            object value = command.ExecuteScalar();
            //int value = Convert.ToInt32(command.ExecuteScalar());
            connection.Close();
            return value;
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

        public void Update(string cmd)
        {
            SqlCommand command = new SqlCommand(cmd, connection);
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        ///////////////////////////////////////////////////////////

        public Dictionary<string, int> LoadDictionary(string table)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            string prefix = table.ToLower().Substring(0, table.Length - 1);
            string key_name = $"{prefix}_name";
            string value_name = $"{prefix}_id";
            string cmd = $"SELECT {key_name},{value_name} FROM {table}";

            SqlCommand command = new SqlCommand(cmd, connection);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
                dictionary.Add(Convert.ToString(reader[0]), Convert.ToInt32(reader[1]));
            connection.Close();

            return dictionary;
        }
    }
}