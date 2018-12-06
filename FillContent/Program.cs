using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FillContent
{
    class Program
    {
        static void Main(string[] args)
        {
            //собираем коолекцию
            var lines = File.ReadAllLines(@"контент_итогUTF8.txt");

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (DbConnection con = new SqlConnection(connectionString))
            {
                if (con.State == ConnectionState.Closed)
                    con.Open();

                for (int i = 0; i < lines.Length; i = i + 2)
                {
                    string eng = lines[i];
                    string rus = lines[i + 1];
                    var sqlQuery = "INSERT INTO Content (Eng, Rus, UserId) VALUES(@eng, @rus, (select Id from Users where Login='admin'));";
                    con.Execute(sqlQuery, new { eng, rus });
                }

            }
        }
    }
}
