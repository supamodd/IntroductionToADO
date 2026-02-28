using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SqlClient;

namespace IntroductionToADO
{
    class Program
    {
        static void Main(string[] args)
        {
            string connection_string = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Movies_SPU_411;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            SqlConnection connection = new SqlConnection(connection_string);
            string cmd = "SELECT title,year,first_name,last_name FROM Movies JOIN Directors ON(director=director_id)";

            Connector connector = new Connector(connection_string);
            connector.Select("title,year,first_name,last_name", "Movies,Directors", "director=director_id");
            Console.WriteLine("\n-------------------------------------------------------------\n");

            string table = "Movies";
            //Console.WriteLine(connector.Scalar($"SELECT MAX(director_id) FROM Directors"));
            Console.WriteLine(connector.GetLastPrimaryKey(table));
            Console.WriteLine(connector.GetNextPrimaryKey(table));
            Console.WriteLine(connector.GetPrimaryKeyColumn(table));
            connector.Insert("Directors", $"{connector.GetNextPrimaryKey("Directors")},N'Besson',N'Luc'");
            connector.Select("*", "Directors");
            Console.WriteLine("\n-------------------------------------------------------------\n");

            connector.Select(cmd);

        }
    }
}