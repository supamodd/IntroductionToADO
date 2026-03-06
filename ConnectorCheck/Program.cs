using IntroductionToADO;
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
            string connection_string = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            Connector connector = new Connector(connection_string);

            Console.WriteLine("=== Тест DLL ===\n");

            // Тест Select
            connector.Select("title,year,first_name,last_name",
                             "Movies,Directors",
                             "director=director_id");

            Console.WriteLine("\n-------------------------------------------------------------\n");

            // Тест с защитой от дублей
            Console.WriteLine("Пытаемся добавить режиссёра (с проверкой)...");
            int nextId = connector.GetNextPrimaryKey("Directors");
            connector.Insert("Directors",
                             $"{nextId},N'Besson',N'Luc'",
                             "RTRIM(LTRIM(last_name)) = N'Besson' AND RTRIM(LTRIM(first_name)) = N'Luc'");

            connector.Select("*", "Directors");

            Console.WriteLine("\n=== DLL успешно работает! ===");
            Console.ReadKey();
        }
    }
}
