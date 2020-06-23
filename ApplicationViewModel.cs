using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace FileSendNet
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        private ComputerServer myComputer;
        private Computer selectComputer;
        public ObservableCollection<Computer> Computers { get; set; }
        

        Thread updator;

        public ComputerServer MyComputer
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
            myComputer = new ComputerServer();
            myComputer.AddNewComputer += AddNewComputer;

            Computers = new ObservableCollection<Computer>();
            updator = new Thread(new ThreadStart(FindComputers));
            updator.Start();

            Computers.Add(new Computer("asd1", "127.0.0.1"));
            Computers.Add(new Computer("ewae23", "123.0.0.13"));
        }

        private void AddNewComputer(Computer computer)
        {
            Computers.Add(computer);
        }

        private async void FindComputers()
        {
            string[] myIP = myComputer.Ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            string mainIp = $"{myIP[0]}.{myIP[1]}.{myIP[2]}.";

            while (true)
            {
                for(int i = 1; i < 254; i++)
                {
                    await Task.Run(() => myComputer.CanConnectWith(mainIp + i.ToString()));
                }
                Thread.Sleep(1500);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }


        private RelayCommand selectCommand;
        public RelayCommand SelectCommand
        {
            get
            {
                return selectCommand ??
                    (selectCommand = new RelayCommand(obj =>
                    {
                        myComputer.ConnectWith(SelectComputer);
                    }));
            }
        }

        public void CloseApplication()
        {
            myComputer.StopServer();
        }
    }
}
