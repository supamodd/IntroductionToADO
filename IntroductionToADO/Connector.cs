using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SqlClient;

namespace IntroductionToADO
{
    class Connector
    {
        private readonly string connection_string;
        private readonly SqlConnection connection;

        public Connector(string connection_string)
        {
            this.connection_string = connection_string;
            this.connection = new SqlConnection(connection_string);
        }

        public void Select(string fields, string tables, string condition = "")
        {
            string cmd = $"SELECT {fields} FROM {tables}";
            if (!string.IsNullOrWhiteSpace(condition))
                cmd += $" WHERE {condition}";

            cmd += ";";
            ExecuteAndPrint(cmd);
        }

        public void Select(string fullQuery)
        {
            string cmd = fullQuery.Trim();
            if (!cmd.EndsWith(";")) cmd += ";";
            ExecuteAndPrint(cmd);
        }

        public void AddPrimaryKey(string table, string pkColumn, string constraintName = null)
        {
            if (string.IsNullOrWhiteSpace(constraintName))
                constraintName = $"PK_{table}_{pkColumn}";

            string cmd = $"ALTER TABLE {table} " +
                         $"ADD CONSTRAINT {constraintName} " +
                         $"PRIMARY KEY ({pkColumn});";

            try
            {
                ExecuteNonQuery(cmd);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Первичный ключ '{constraintName}' успешно добавлен к таблице '{table}' " +
                                  $"(колонка: {pkColumn})");
                Console.ResetColor();
            }
            catch (SqlException ex)
                when (ex.Message.Contains("already has a primary key") ||
                      ex.Number == 1750 || ex.Number == 1779)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"ℹ️  Таблица '{table}' уже имеет первичный ключ. Пропускаем.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка при добавлении PK для таблицы '{table}':");
                Console.WriteLine($"   {ex.Message}");
                Console.ResetColor();
                throw; 
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Insert (теперь тоже через общий метод)
        // ──────────────────────────────────────────────────────────────
        public void Insert(string table, string values)
        {
            string cmd = $"INSERT INTO {table} VALUES ({values});";
            ExecuteNonQuery(cmd);
        }
         
        // ──────────────────────────────────────────────────────────────
        // Приватные вспомогательные методы
        // ──────────────────────────────────────────────────────────────
      
        private void ExecuteAndPrint(string commandText)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = new SqlCommand(commandText, connection))
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

        private void ExecuteNonQuery(string commandText)
        {
            connection.Open();
            try
            {
                using (SqlCommand command = new SqlCommand(commandText, connection))
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
    }
}