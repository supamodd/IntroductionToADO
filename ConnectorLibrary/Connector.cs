using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace IntroductionToADO
{
    public class Connector   // сделал паблик
    {
        string connection_string;
        SqlConnection connection;

        public Connector(string connection_string) 
        {
            this.connection_string = connection_string;
            this.connection = new SqlConnection(connection_string);
        }

        
        public void Select(string cmd)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write(reader[i].ToString().PadRight(29));
                        }
                        Console.WriteLine();
                    }
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        public void Select(string fields, string tables, string condition = "")
        {
            string cmd = $"SELECT {fields} FROM {tables}";
            if (!string.IsNullOrWhiteSpace(condition))
                cmd += $" WHERE {condition}";
            cmd += ";";
            Select(cmd);
        }

        public void Insert(string cmd)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        public void Insert(string table, string values)
        {
            string cmd = $"INSERT INTO {table} VALUES ({values})";
            Insert(cmd);
        }

        public void Insert(string table, string values, string uniqueCondition = null)
        {
            if (!string.IsNullOrWhiteSpace(uniqueCondition))
            {
                if (RecordExists(table, uniqueCondition))
                {
                    Console.WriteLine($"⚠️  Запись уже существует в таблице '{table}'. Добавление пропущено.");
                    return;
                }
            }
            Insert(table, values);
        }

        public object Scalar(string cmd)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    return command.ExecuteScalar();
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        public string GetPrimaryKeyColumn(string table)
        {
            return (string)Scalar(
                $"SELECT COLUMN_NAME " +
                $"FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE " +
                $"WHERE CONSTRAINT_NAME = (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = N'{table}' AND CONSTRAINT_TYPE=N'PRIMARY KEY')"
            );
        }

        public int GetLastPrimaryKey(string table)
        {
            return Convert.ToInt32(Scalar($"SELECT MAX({GetPrimaryKeyColumn(table)}) FROM {table}"));
        }

        public int GetNextPrimaryKey(string table)
        {
            return GetLastPrimaryKey(table) + 1;
        }

        public bool RecordExists(string table, string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return false;

            string safeCondition = condition
                .Replace("first_name =", "RTRIM(LTRIM(first_name)) =")
                .Replace("last_name =", "RTRIM(LTRIM(last_name)) =")
                .Replace("title =", "RTRIM(LTRIM(title)) =");

            Console.WriteLine($"[DEBUG] Проверяем существование в таблице '{table}' по условию: {safeCondition}");

            object result = Scalar($"SELECT COUNT(*) FROM {table} WHERE {safeCondition}");
            int count = Convert.ToInt32(result);

            Console.WriteLine($"[DEBUG] COUNT(*) = {count}");

            return count > 0;
        }
    }
}