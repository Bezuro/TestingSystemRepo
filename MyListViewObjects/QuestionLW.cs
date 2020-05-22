using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyListViewObjects
{
    [Serializable]
    public class QuestionLW
    {
        public string QuestionText { get; set; }
        public double ScoreModifier { get; set; }
    }
}
