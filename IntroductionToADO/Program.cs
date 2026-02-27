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

            //connector.Insert("Directors", "6,N'Tarantino',N'Quentin'");
            connector.Select("*", "Directors");

            connector.Select("SELECT title, year, first_name, last_name FROM Movies JOIN Directors ON Movies.director = Directors.director_id");
            Console.WriteLine("\n-------------------------------------------------------------\n");

            connector.Select("SELECT * FROM Directors");
            Console.WriteLine("\n-------------------------------------------------------------\n");

            Console.WriteLine("Добавляем первичные ключи...\n");

            connector.AddPrimaryKey("Directors", "director_id");

            connector.AddPrimaryKey("Movies", "movie_id", "PK_Movies");
            Console.WriteLine("\n-------------------------------------------------------------\n");

            connector.Update("Directors",
                 "first_name = N'Christopher', last_name = N'Nolan'",
                 "director_id = 7");
        }
    }
}