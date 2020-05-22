using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TestDLL
{
    [Serializable]
    [XmlType("Answer")]
    public class Answer
    {
        [XmlElement("AnswerText")]
        public string Text { get; set; }
        [XmlElement("IsRight")]
        public bool IsRight { get; set; }
        public bool IsChecked { get; set; }
    }

    [Serializable]
    [XmlType("Question")]
    public class Question
    {
        [XmlElement("QuestionText")]
        public string Text { get; set; }
        [XmlElement("Answers")]
        public List<Answer> Answers { get; set; } = new List<Answer>();

        [XmlElement("IsAnsweredCorrectly")]
        public bool IsAnsweredCorrectly { get; set; }
        [XmlElement("ScoreModifier")]
        public double ScoreModifier { get; set; }
        [XmlElement("IsManyAnswers")]
        public bool IsManyAnswers { get; set; }
    }

    [Serializable]
    [XmlType("Test")]
    public class Test
    {
        [XmlElement("TestName")]
        public string Name { get; set; }
        [XmlElement("Description")]
        public string Description { get; set; }

        [XmlElement("Questions")]
        public ObservableCollection<Question> Questions { get; set; } = new ObservableCollection<Question>();
    }
}
