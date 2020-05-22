using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyListViewObjects
{
    [Serializable]
    public class AnswerLW
    {
        public string Answer { get; set; }
        public bool IsRight { get; set; }
    }
}
