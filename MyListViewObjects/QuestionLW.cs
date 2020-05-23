using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MyListViewObjects
{
    [Serializable]
    public class QuestionLW: INotifyPropertyChanged
    {
        private string questionText;
        private double scoreModifier;

        public string QuestionText
        {
            get
            {
                return this.questionText;
            }
            set
            {
                if (value != this.questionText)
                {
                    this.questionText = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public double ScoreModifier
        {
            get
            {
                return this.scoreModifier;
            }
            set
            {
                if (value != this.scoreModifier)
                {
                    this.scoreModifier = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
