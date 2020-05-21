using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueMessageDLL
{
    [Serializable]
    public enum Command
    {
        Approve,
        Reject,

        LogIn,
        Connect,
        AdminConnect,
        TeacherConnect,
        StudentConnect,
        Disconnect,

        LoadCurrentTest,
        SendMark,

        LoadTestList,
        LoadMarks,
        LoadTest,
        AddTest,
        DeleteTest,
        ChangeTest, //???
        ChooseCurrentTest,
        UpdateMarks,

        LoadUsers,
        AddUser,
        DeleteUser,
        ChangeUser //???
    }

    [Serializable]
    public class TrueMessage
    {
        public Command Command { get; set; }
        public string Login { get; set; }
        public object Message { get; set; }
    }
}
