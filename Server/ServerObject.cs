using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using TrueMessageDLL;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using Server.Model;
using MyListViewObjects;

namespace Server
{
    class ServerObject
    {
        static TcpListener tcpListener;
        Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();

        string connectionString = ConfigurationManager.ConnectionStrings["TestingSystemConnectionString"].ConnectionString;

        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Server started");

                tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptFunction), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        protected internal void Disconnect()
        {
            tcpListener.Stop();
        }

        private void AcceptFunction(IAsyncResult ar)
        {
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);

            NetworkStream stream = tcpClient.GetStream();
            byte[] dataReadMessage = new byte[64000];
            (TcpClient tcpClient, byte[] dataReadMessage) stateReadMessage = (tcpClient, dataReadMessage);
            stream.BeginRead(dataReadMessage, 0, dataReadMessage.Length, new AsyncCallback(ReadMessage), stateReadMessage);

            tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptFunction), null);
        }

        private void ReadMessage(IAsyncResult ar)
        {
            Console.WriteLine("123 ReadMessage");

            (TcpClient tcpClient, byte[] dataReadMessage) stateReadMessage = ((TcpClient tcpClient, byte[] dataReadMessage))ar.AsyncState;

            TcpClient tcpClient = stateReadMessage.tcpClient;
            byte[] dataReadMessage = stateReadMessage.dataReadMessage;
            NetworkStream stream = tcpClient.GetStream();

            int byteLenght = stream.EndRead(ar);

            if (byteLenght > 0)
            {
                IFormatter formatter = new BinaryFormatter();
                TrueMessage trueMessage = new TrueMessage();
                using (MemoryStream memoryStream = new MemoryStream(dataReadMessage))
                {
                    trueMessage = (TrueMessage)formatter.Deserialize(memoryStream);
                }

                if (trueMessage.Command == Command.LogIn)
                {
                    LogIn(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.AdminConnect)
                {
                    AdminConnect(tcpClient, trueMessage);
                }
            }
        }

        private void AdminConnect(TcpClient tcpClient, TrueMessage trueMessage)
        {
            string login = trueMessage.Login;
            IPEndPoint clientEP = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            clients.Add(login, clientEP);

            Console.WriteLine($"Admin {login} connected");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                SqlCommand sqlCommand = new SqlCommand("Proc_LoadUsers", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;

                List<UserLW> users = new List<UserLW>();

                using (SqlDataReader sqlReader = sqlCommand.ExecuteReader())
                {
                    while (sqlReader.Read())
                    {
                        users.Add(new UserLW
                        {
                            Login = Convert.ToString(sqlReader["Login"]),
                            Password = Convert.ToString(sqlReader["Password"]),
                            FullName = Convert.ToString(sqlReader["FullName"]),
                            UserType = Convert.ToString(sqlReader["Type"])
                        });
                    }
                }

                TrueMessage trueMessageToClient = new TrueMessage();

                if (users.Count <= 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                    trueMessageToClient.Message = users.ToArray();
                }

                byte[] dataAdminConnectAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataAdminConnectAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataAdminConnectAnswer, 0, dataAdminConnectAnswer.Length, new AsyncCallback(AdminConnectAnswer), tcpClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
        }

        private void AdminConnectAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void LogIn(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine("LogIn");

            string login = trueMessage.Login;
            string password = (string)trueMessage.Message;

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                SqlCommand sqlCommand = new SqlCommand("Proc_SelectUserType", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Login", login);
                sqlCommand.Parameters.AddWithValue("@Password", password);
                string userType = Convert.ToString(sqlCommand.ExecuteScalar());

                TrueMessage trueMessageToClient = new TrueMessage();

                Console.WriteLine($"UserType: {userType}. Length: {userType.Length}");
                if (userType.Length == 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                    if (clients.Keys.Contains(login))
                    {
                        trueMessageToClient.Message = "This user is already logged in!";
                    }
                    else
                    {
                        trueMessageToClient.Message = "Username or password is incorrect.";
                    }
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                    trueMessageToClient.Message = userType;
                }


                byte[] dataLogInAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataLogInAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataLogInAnswer, 0, dataLogInAnswer.Length, new AsyncCallback(LogInAnswer), tcpClient);

                Console.WriteLine("LogInEnd");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        private void LogInAnswer(IAsyncResult ar)
        {
            Console.WriteLine("LogInAnswer");
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
            Console.WriteLine("LogInAnswerEnd");
        }
    }
}
