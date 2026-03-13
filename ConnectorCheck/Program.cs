using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectorCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            string connection_string = "Data Source=SUPAMODDPC\\SQLEXPRESS;Initial Catalog=SPU_411_Import;Integrated Security=True;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            Connector.Connector connector = new Connector.Connector(connection_string);

            Console.WriteLine("Тест 1: Select из Directions");
            connector.Select("SELECT * FROM Directions");

            Console.WriteLine("\nТест 2: Select с условием из Groups");
            connector.Select("group_id, group_name", "Groups", "group_id > 1");

            Console.WriteLine("\nТест 3: Select из Teachers");
            connector.Select("SELECT TOP 5 * FROM Teachers");

            Console.WriteLine("\nТест 4: GetPrimaryKeyColumn для Directions");
            string pkColumn = connector.GetPrimaryKeyColumn("Directions");
            Console.WriteLine($"Первичный ключ для Directions: {pkColumn}");

            Console.WriteLine("\nТест 5: GetLastPrimaryKey и GetNextPrimaryKey для Directions");
            int lastPk = connector.GetLastPrimaryKey("Directions");
            int nextPk = connector.GetNextPrimaryKey("Directions");
            Console.WriteLine($"Последний PK: {lastPk}, Следующий PK: {nextPk}");

            Console.WriteLine("\nТест 6: Scalar (COUNT из Students)");
            object count = connector.Scalar("SELECT COUNT(*) FROM Students");
            Console.WriteLine($"Количество студентов: {count}");

            Console.WriteLine("\nТест 7: GetPrimaryKey по условию из Groups");
            
            object pk = connector.GetPrimaryKey("Groups", "group_name", "SPU 411"); 
            Console.WriteLine($"PK для группы: {pk ?? "Не найдено"}");

            Console.WriteLine("\nТест 8: GetPrimaryKey через cmd из Holidays");
            object holidayPk = connector.GetPrimaryKey("SELECT TOP 1 holiday_id FROM Holidays WHERE holiday_date = '2026-02-23'");
            Console.WriteLine($"PK для праздника: {holidayPk ?? "Не найдено"}");

            Console.WriteLine("\nТест 11: Select из Schedule");
            connector.Select("SELECT TOP 10 * FROM Schedule");
        }
    }
}