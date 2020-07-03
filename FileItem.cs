using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FileSendNet
{
    class FileItem : INotifyPropertyChanged
    {

        

        private string name;
        private bool isFolder;

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(); }
        }
        public bool IsFolder
        {
            get { return isFolder; }
            set { isFolder = value; OnPropertyChanged(); }
        }

        public FileItem(string name, bool isFolder)
        {
            this.name = name;
            this.isFolder = isFolder;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
