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
    public class AnswerLW: INotifyPropertyChanged
    {
        private string answer;
        private bool isRight;

        public string Answer 
        {
            get
            {
                return this.answer;
            }
            set
            {
                if (value != this.answer)
                {
                    this.answer = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsRight
        {
            get
            {
                return this.isRight;
            }
            set
            {
                if (value != this.isRight)
                {
                    this.isRight = value;
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
