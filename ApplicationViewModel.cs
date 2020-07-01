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
using System.Windows.Threading;

namespace FileSendNet
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        private ComputerServer myComputer;
        private Computer selectComputer;
        public ObservableCollection<Computer> Computers { get; set; }
        Dispatcher dispatcher;
        public ObservableCollection<FileItem> FileTree { get; set; }

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


        public ApplicationViewModel(Dispatcher dis)
        {
            dispatcher = dis;
            myComputer = new ComputerServer();
            myComputer.AddNewComputer += AddNewComputer;

            Computers = new ObservableCollection<Computer>();
        }

        private void AddNewComputer(Computer computer)
        {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, 
                (ThreadStart)delegate () 
                { 
                    Computers.Add(computer); 
                });
        }

        private void FindComputers()
        {
            string[] myIP = myComputer.Ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            string mainIp = $"{myIP[0]}.{myIP[1]}.{myIP[2]}.";

            for(int i = 1; i < 254; i++)
            {
                Thread th = new Thread( new ParameterizedThreadStart(myComputer.CanConnectWith));
                th.Start(mainIp + i.ToString());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }


        private RelayCommand connectWith;
        public RelayCommand ConnectWith
        {
            get
            {
                return connectWith ??
                    (connectWith = new RelayCommand(obj =>
                    {
                        myComputer.ConnectWith(SelectComputer);
                    }));
            }
        }

        private RelayCommand closedApp;
        public RelayCommand ClosedApp
        {
            get
            {
                return closedApp ??
                    (closedApp = new RelayCommand(obj =>
                    {
                        myComputer.StopServer();
                    }));
            }
        }

        private RelayCommand updateListPK;
        public RelayCommand UpdateListPK
        {
            get
            {
                return updateListPK ??
                    (updateListPK = new RelayCommand(obj =>
                    {
                        updator = new Thread(new ThreadStart(FindComputers));
                        updator.Start();
                    }));
            }
        }

        public void CloseApplication()
        {
            myComputer.StopServer();
        }
    }
}
