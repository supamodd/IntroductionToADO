using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connector
{
    public class SqlParser
    {
        public string GetTableFromInsert(string cmd)
        {
            string[] parts = cmd.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            return parts[1]; // Assuming format "INSERT INTO table (fields) VALUES (values)"
        }

        public string GetFieldsFromInsert(string cmd)
        {
            int startIndex = cmd.IndexOf('(');
            int endIndex = cmd.IndexOf(')', startIndex + 1);
            if (startIndex == -1 || endIndex == -1) return null;
            return cmd.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        }

        public string GetValuesFromInsert(string cmd)
        {
            int valuesStart = cmd.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
            if (valuesStart == -1) return null;
            string valuesPart = cmd.Substring(valuesStart + "VALUES".Length).Trim();
            if (valuesPart.StartsWith("(") && valuesPart.EndsWith(")"))
            {
                return valuesPart.Substring(1, valuesPart.Length - 2).Trim();
            }
            return null;
        }

        public string BuildCondition(string fields, string values)
        {
            string[] s_fields = fields.Split(',');
            string[] s_values = values.Split(',');
            if (s_fields.Length != s_values.Length) return null;
            string condition = "";
            for (int i = 0; i < s_fields.Length; i++)
            {
                string field = s_fields[i].Trim();
                string value = s_values[i].Trim();
                if (field.Contains("_id")) continue;
                condition += (value.Length > 1 && value[0] != 'N' && value[1] != '\'') ?
                    $"{field}=N'{value}'"
                    : $"{field}={value}";
                if (i != s_values.Length - 1) condition += " AND ";
            }
            return condition;
        }
    }

    public class Connector
    {
        private string connection_string;
        private SqlConnection connection;
        private SqlParser parser;

        public Connector(string connection_string)
        {
            this.connection_string = connection_string;
            this.connection = new SqlConnection(connection_string);
            this.parser = new SqlParser();
        }

        public object GetPrimaryKey(string cmd)
        {
            SqlCommand command = new SqlCommand(cmd, connection);
            connection.Open();
            object primary_key = command.ExecuteScalar();
            connection.Close();
            return primary_key;
        }

        public object GetPrimaryKey(string table, string fields, string values)
        {
            string condition = parser.BuildCondition(fields, values);
            if (condition == null) return null;
            string cmd = $"SELECT {GetPrimaryKeyColumn(table)} FROM {table} WHERE {condition}";
            return Scalar(cmd);
        }

        public void Select(string fields, string tables, string condition = "")
        {
            string cmd = $"SELECT {fields} FROM {tables}";
            if (condition != "") cmd += $" WHERE {condition}";
            cmd += ";";
            Select(cmd);
        }

        public void Select(string cmd)
        {
            connection.Open();
            SqlCommand command = new SqlCommand(cmd, connection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write(reader[i].ToString().PadRight(28));
                }
                Console.WriteLine();
            }
            reader.Close();
            connection.Close();
        }

        public void Insert(string cmd)
        {
            string table = parser.GetTableFromInsert(cmd);
            string fields = parser.GetFieldsFromInsert(cmd);
            string values = parser.GetValuesFromInsert(cmd);

            Console.WriteLine(table);
            Console.WriteLine(fields);
            Console.WriteLine(values);

            if (GetPrimaryKey(table, fields, values) != null) return;

            connection.Open();
            SqlCommand command = new SqlCommand(cmd, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        public void Insert(string table, string values)
        {
            string cmd = $"INSERT INTO {table} VALUES ({values})";
            Insert(cmd);
        }

        public object Scalar(string cmd)
        {
            SqlCommand command = new SqlCommand(cmd, connection);
            connection.Open();
            object value = command.ExecuteScalar();
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
    }
}