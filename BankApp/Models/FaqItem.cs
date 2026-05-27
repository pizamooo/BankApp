using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.Models
{
    public class FaqItem : INotifyPropertyChanged
    {
        private bool _isOpen;

        public string Question { get; set; }
        public string Answer { get; set; }

        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                _isOpen = value;
                PropertyChanged?.Invoke(this,
                    new PropertyChangedEventArgs(nameof(IsOpen)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
