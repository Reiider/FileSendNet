using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Threading;

namespace FileSendNet
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        private Computer myComputer;
        private Computer selectComputer;
        public ObservableCollection<Computer> Computers { get; set; }

        Thread updator;

        public Computer MyComputer
        {
            get { return myComputer; }
            set
            {
                myComputer = value;
                OnPropertyChanged();
            }
        }

        public Computer SelectComputer
        {
            get { return selectComputer; }
            set
            {
                selectComputer = value;
                OnPropertyChanged();
            }
        }

        public ApplicationViewModel()
        {
            string host = Dns.GetHostName();
            myComputer = new Computer(host, Dns.GetHostEntry(host).AddressList[1].ToString());

            Computers = new ObservableCollection<Computer>();
            updator = new Thread(new ThreadStart(FindComputers));
            updator.Start();
        }

        private async void FindComputers()
        {
            string[] myIP = myComputer.Ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            string mainIp = $"{myIP[0]}.{myIP[1]}.{myIP[2]}.";

            while (true)
            {
                for(int i = 1; i < 254; i++)
                {
                      
                }
                Thread.Sleep(1500);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
