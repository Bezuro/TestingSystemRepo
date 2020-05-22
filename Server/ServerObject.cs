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
using TrueXmlSerializerDLL;
using TestDLL;

namespace Server
{
    class ServerObject
    {
        static TcpListener tcpListener;
        Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();

        TestDLL.Test currentTest;
        string testTime;

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
                else if (trueMessage.Command == Command.AddUser)
                {
                    AddUser(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.DeleteUser)
                {
                    DeleteUser(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.Disconnect)
                {
                    string login = trueMessage.Login;

                    clients.Remove(login);

                    Console.WriteLine($"User {login} disconnected");
                }
                else if (trueMessage.Command == Command.TeacherConnect)
                {
                    TeacherConnect(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.UpdateMarks)
                {
                    UpdateMarks(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.AddTest)
                {
                    AddTest(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.DeleteTest)
                {
                    DeleteTest(tcpClient, trueMessage);
                }
                else if (trueMessage.Command == Command.ChooseCurrentTest)
                {
                    ChooseCurrentTest(tcpClient, trueMessage);
                }

                Console.WriteLine("456 COMMAND: " + trueMessage.Command.ToString());

                byte[] dataReadMessageNew = new byte[64000];
                (TcpClient tcpClient, byte[] dataReadMessageNew) stateReadMessageNew = (tcpClient, dataReadMessageNew);
                stream.BeginRead(dataReadMessageNew, 0, dataReadMessageNew.Length, new AsyncCallback(ReadMessage), stateReadMessageNew);
            }
        }

        private void ChooseCurrentTest(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine($"ChooseCurrentTest");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                KeyValuePair<string, string> choosePair = (KeyValuePair<string, string>)trueMessage.Message;

                string testNameToChoose = choosePair.Key;

                SqlCommand sqlCommand = new SqlCommand("Proc_GetTestPathByName", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Name", testNameToChoose);
                string path = Convert.ToString(sqlCommand.ExecuteScalar());

                TrueMessage trueMessageToClient = new TrueMessage();

                if (File.Exists(path))
                {
                    currentTest = TrueXmlSerializer.Load<TestDLL.Test>(path);
                    testTime = choosePair.Value;

                    trueMessageToClient.Command = Command.Approve;
                }
                else
                {
                    trueMessageToClient.Command = Command.Reject;
                    trueMessageToClient.Message = "Test not found";
                }

                byte[] dataChooseCurrentTestAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataChooseCurrentTestAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataChooseCurrentTestAnswer, 0, dataChooseCurrentTestAnswer.Length, new AsyncCallback(ChooseCurrentTestAnswer), tcpClient);

                Console.WriteLine($"ChooseCurrentTest ended");
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

        private void ChooseCurrentTestAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void DeleteTest(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine($"DeleteTest");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                string testNameToDelete = (string)trueMessage.Message;

                SqlCommand sqlCommand = new SqlCommand("Proc_GetTestPathByName", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Name", testNameToDelete);
                string path = Convert.ToString(sqlCommand.ExecuteScalar());

                if (File.Exists(path))
                {
                    File.Delete(path);
                } //else return file already deleted??

                sqlCommand = new SqlCommand("Proc_DeleteTestByName", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Name", testNameToDelete);
                int nonQ = sqlCommand.ExecuteNonQuery();

                TrueMessage trueMessageToClient = new TrueMessage();

                if (nonQ == 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                    trueMessageToClient.Message = testNameToDelete;
                }

                byte[] dataDeleteTestAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataDeleteTestAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataDeleteTestAnswer, 0, dataDeleteTestAnswer.Length, new AsyncCallback(DeleteTestAnswer), tcpClient);

                Console.WriteLine($"DeleteTest ended");
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

        private void DeleteTestAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void AddTest(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine($"AddTest");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                TestDLL.Test test = (TestDLL.Test)trueMessage.Message;

                string path = test.Name;
                string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

                foreach (char c in invalid)
                {
                    path = path.Replace(c.ToString(), "");
                }

                path = "..\\..\\..\\Tests\\" + path + ".xml";

                TrueMessage trueMessageToClient = new TrueMessage();

                Random random = new Random();
                while (File.Exists(path))
                {
                    path += random.Next(1, 100);
                }

                TrueXmlSerializer.Save(test, path);

                SqlCommand sqlCommand = new SqlCommand("Proc_AddTest", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Name", test.Name);
                sqlCommand.Parameters.AddWithValue("@Description", test.Description);
                sqlCommand.Parameters.AddWithValue("@Path", path);
                sqlCommand.ExecuteNonQuery();

                trueMessageToClient.Command = Command.Approve;
                trueMessageToClient.Message = test.Name;

                byte[] dataAddTestAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataAddTestAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataAddTestAnswer, 0, dataAddTestAnswer.Length, new AsyncCallback(AddTestAnswer), tcpClient);

                Console.WriteLine($"AddTest ended");
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

        private void AddTestAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void UpdateMarks(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine($"UpdateMarks");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                string testName = (string)trueMessage.Message;

                List<MarkLW> marks = new List<MarkLW>();

                SqlCommand sqlCommand = new SqlCommand("Proc_GetMarksByTestName", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Name", testName);

                using (SqlDataReader sqlReader = sqlCommand.ExecuteReader())
                {
                    while (sqlReader.Read())
                    {
                        marks.Add(new MarkLW
                        {
                            FullName = Convert.ToString(sqlReader["FullName"]),
                            Mark = Convert.ToInt32(sqlReader["Mark"])
                        });
                    }
                }

                TrueMessage trueMessageToClient = new TrueMessage();

                if (marks.Count <= 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                    trueMessageToClient.Message = marks.ToArray();
                }

                byte[] dataUpdateMarksAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataUpdateMarksAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataUpdateMarksAnswer, 0, dataUpdateMarksAnswer.Length, new AsyncCallback(UpdateMarksAnswer), tcpClient);
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

        private void UpdateMarksAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void TeacherConnect(TcpClient tcpClient, TrueMessage trueMessage)
        {
            string login = trueMessage.Login;
            IPEndPoint clientEP = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            clients.Add(login, clientEP);

            Console.WriteLine($"Teacher {login} connected");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                List<string> testNames = new List<string>();

                SqlCommand sqlCommand = new SqlCommand("Proc_GetTestNames", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;

                using (SqlDataReader sqlReader = sqlCommand.ExecuteReader())
                {
                    while (sqlReader.Read())
                    {
                        testNames.Add(Convert.ToString(sqlReader["Name"]));
                    }
                }

                TrueMessage trueMessageToClient = new TrueMessage();

                if (testNames.Count <= 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                    trueMessageToClient.Message = testNames.ToArray();
                }

                byte[] dataTeacherConnectAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataTeacherConnectAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataTeacherConnectAnswer, 0, dataTeacherConnectAnswer.Length, new AsyncCallback(TeacherConnectAnswer), tcpClient);
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

        private void TeacherConnectAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void DeleteUser(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine("DeleteUser");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                string userLoginToDelete = (string)trueMessage.Message;

                SqlCommand sqlCommand = new SqlCommand("Proc_DeleteUser", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Login", userLoginToDelete);
                int count = sqlCommand.ExecuteNonQuery();

                TrueMessage trueMessageToClient = new TrueMessage();

                if (count == 0)
                {
                    trueMessageToClient.Command = Command.Reject;
                }
                else
                {
                    trueMessageToClient.Command = Command.Approve;
                }

                byte[] dataDeleteUserAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataDeleteUserAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataDeleteUserAnswer, 0, dataDeleteUserAnswer.Length, new AsyncCallback(DeleteUserAnswer), tcpClient);
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

        private void DeleteUserAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
        }

        private void AddUser(TcpClient tcpClient, TrueMessage trueMessage)
        {
            Console.WriteLine("AddUser");

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                UserLW newUser = (UserLW)trueMessage.Message;

                SqlCommand sqlCommand = new SqlCommand("Proc_LoginAvailabilityCheck", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.AddWithValue("@Login", newUser.Login);
                int count = Convert.ToInt32(sqlCommand.ExecuteScalar());

                TrueMessage trueMessageToClient = new TrueMessage();

                if (count == 1)
                {
                    Console.WriteLine("AddUserReject Login exists");

                    trueMessageToClient.Command = Command.Reject;
                    trueMessageToClient.Message = "User with that login already exists";
                }
                else
                {
                    sqlCommand = new SqlCommand("Proc_GetUserTypeId", sqlConnection);
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@UserType", newUser.UserType);
                    int userTypeId = Convert.ToInt32(sqlCommand.ExecuteScalar());

                    if (userTypeId == 0)
                    {
                        Console.WriteLine("AddUserReject wrong UserType");

                        trueMessageToClient.Command = Command.Reject;
                        trueMessageToClient.Message = "Wrong UserType";
                    }
                    else
                    {
                        Console.WriteLine("AddUserApprove");

                        sqlCommand = new SqlCommand("Proc_AddUser", sqlConnection);
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.Parameters.AddWithValue("@Login", newUser.Login);
                        sqlCommand.Parameters.AddWithValue("@Password", newUser.Password);
                        sqlCommand.Parameters.AddWithValue("@FullName", newUser.FullName);
                        sqlCommand.Parameters.AddWithValue("@UserTypeId", userTypeId);
                        sqlCommand.ExecuteNonQuery();

                        trueMessageToClient.Command = Command.Approve;
                    }
                }

                byte[] dataAddUserAnswer;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToClient);
                    dataAddUserAnswer = stream.ToArray();
                }

                NetworkStream networkStream = tcpClient.GetStream();

                networkStream.BeginWrite(dataAddUserAnswer, 0, dataAddUserAnswer.Length, new AsyncCallback(AddUserAnswer), tcpClient);
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

        private void AddUserAnswer(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            NetworkStream networkStream = tcpClient.GetStream();

            networkStream.EndWrite(ar);
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
                    trueMessageToClient.Message = "Username or password is incorrect.";
                }
                else
                {
                    if (clients.Keys.Contains(login))
                    {
                        trueMessageToClient.Message = "This user is already logged in!";
                    }
                    else
                    {
                        trueMessageToClient.Command = Command.Approve;
                        trueMessageToClient.Message = userType;
                    }
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
