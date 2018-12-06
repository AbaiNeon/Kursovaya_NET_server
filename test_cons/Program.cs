using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dapper;
using System.IO;
using System.Configuration;

namespace test_cons
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            try
            {
                socket.Bind(new IPEndPoint(IPAddress.Any, 3535));
                socket.Listen(10);

                while (true)
                {
                    Socket client = socket.Accept();
                    Console.WriteLine("New connection");

                    byte[] buffer = new byte[1024];
                    int bytes = client.Receive(buffer);

                    //принимаем команду
                    string receivedCommand = Encoding.UTF8.GetString(buffer, 0, bytes);

                    if (receivedCommand.StartsWith("Add")) //Add:abai 12345
                    {
                        string receivedLoginPassword = receivedCommand.Split(":".ToCharArray())[1];
                        string login = receivedLoginPassword.Split(" ".ToCharArray())[0];
                        string password = receivedLoginPassword.Split(" ".ToCharArray())[1];

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "INSERT INTO Users(Login, Password) SELECT @login, @password";
                            db.Execute(sqlQuery, new { login, password });
                        }
                    }
                    else if (receivedCommand.StartsWith("checklogin")) //checklogin:abai
                    {
                        //есть ли login такой уже в БД
                        string receivedLogin = receivedCommand.Split(":".ToCharArray())[1];
                        List<User> users = new List<User>();
                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            users = db.Query<User>("SELECT * FROM Users where Login = @receivedLogin", new { receivedLogin }).ToList();
                        }

                        string response = "notExist";
                        if (users.Count == 1)
                        {
                            response = "exist";
                        }

                        // отправляем ответ
                        client.Send(Encoding.UTF8.GetBytes(response));
                    }
                    else if (receivedCommand.StartsWith("checklogandpswd")) //checklogandpswd:login password
                    {
                        string receivedLoginAndPswrd = receivedCommand.Split(":".ToCharArray())[1];
                        string login = receivedLoginAndPswrd.Split(" ".ToCharArray())[0];
                        string password = receivedLoginAndPswrd.Split(" ".ToCharArray())[1];

                        List<User> users = new List<User>();
                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            users = db.Query<User>("SELECT * FROM Users where Login = @login AND Password=@password", new { login, password }).ToList();
                        }

                        string response = "notExist";
                        if (users.Count == 1)
                        {
                            response = "exist";
                        }

                        // отправляем ответ
                        client.Send(Encoding.UTF8.GetBytes(response));
                    }
                    else if (receivedCommand.StartsWith("get30"))   //get30:login
                    {
                        string login = receivedCommand.Split(":".ToCharArray())[1];

                        //массив с 30 рандомными числами от 1 до 50
                        List<int> randomList = Get30UniqRandomNums();

                        //получаем контент из БД
                        List<Content> sentences = new List<Content>();
                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            sentences = db.Query<Content>("SELECT * FROM Content where (UserId = (select Id from Users where Login=@login) or UserId=1) and Id IN @list", new { login, list=randomList }).ToList();
                        }

                        string json = JsonConvert.SerializeObject(sentences);
                        client.Send(Encoding.UTF8.GetBytes(json));
                    }
                    else if (receivedCommand.StartsWith("postStat")) //postStat:login stat
                    {
                        string receivedLoginAndStat = receivedCommand.Split(":".ToCharArray())[1];
                        string login = receivedLoginAndStat.Split(" ".ToCharArray())[0];
                        string stat = receivedLoginAndStat.Split(" ".ToCharArray())[1];

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "INSERT INTO [Statistics](UserId, RightAnswers) SELECT (select Id from Users where Login=@login), @stat";
                            db.Execute(sqlQuery, new { login, stat });
                        }
                    }
                    else if (receivedCommand.StartsWith("getAllContent")) //getAllContent:login
                    {
                        string login = receivedCommand.Split(":".ToCharArray())[1];
                        List<Content> sentences = new List<Content>();
                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "SELECT * FROM Content where UserId = (select Id from Users where Login=@login) or UserId=1";
                            sentences = db.Query<Content>(sqlQuery, new { login }).ToList();
                        }
                        string json = JsonConvert.SerializeObject(sentences);
                        client.Send(Encoding.UTF8.GetBytes(json));
                    }
                    else if (receivedCommand.StartsWith("getAllStat"))// getAllStat:abai
                    {
                        string login = receivedCommand.Split(":".ToCharArray())[1];
                        List<Statistic> statistics = new List<Statistic>();
                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "SELECT * FROM [Statistics] where UserId = (select Id from Users where Login=@login)";
                            statistics = db.Query<Statistic>(sqlQuery, new { login }).ToList();
                        }
                        string json = JsonConvert.SerializeObject(statistics);
                        client.Send(Encoding.UTF8.GetBytes(json));
                    }
                    else if (receivedCommand.StartsWith("postContent")) // postContent:Eng#Rus#abai
                    {
                        string engRusLogin = receivedCommand.Split(":".ToCharArray())[1];
                        string eng = engRusLogin.Split("#".ToCharArray())[0];
                        string rus = engRusLogin.Split("#".ToCharArray())[1];
                        string login = engRusLogin.Split("#".ToCharArray())[2];

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "INSERT INTO [Content](Eng, Rus, UserId) SELECT @eng, @rus, (select Id from Users where Login=@login)";
                            db.Execute(sqlQuery, new { eng, rus, login });
                        }
                    }
                    else if (receivedCommand.StartsWith("updateContent")) //updateContent:Id#Eng#Rus#abai
                    {
                        string idEngRusLogin = receivedCommand.Split(":".ToCharArray())[1];
                        string id = idEngRusLogin.Split("#".ToCharArray())[0];
                        string eng = idEngRusLogin.Split("#".ToCharArray())[1];
                        string rus = idEngRusLogin.Split("#".ToCharArray())[2];
                        string login = idEngRusLogin.Split("#".ToCharArray())[3];

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "update [Content] SET Eng=@eng, Rus=@rus where Id=@id";
                            db.Execute(sqlQuery, new { eng, rus, id });
                        }
                    }
                    else if (receivedCommand.StartsWith("deleteContent")) //deleteContent:Id
                    {
                        string id = receivedCommand.Split(":".ToCharArray())[1];

                        using (IDbConnection db = new SqlConnection(connectionString))
                        {
                            var sqlQuery = "delete [Content] where Id=@id";
                            db.Execute(sqlQuery, new { id });
                        }
                    }
                    
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static List<int> Get30UniqRandomNums()
        {
            List<int> randomList = new List<int>();
            int contentRecords = 1330; //в таблице Content 1330 записей
            while (randomList.Count <= 30)
            {
                Random rnd = new Random();
                int number = rnd.Next(1, contentRecords);
                if (!randomList.Contains(number))
                    randomList.Add(number);
            }
            
            return randomList.GetRange(0, 30); ;
        }
    }
}
