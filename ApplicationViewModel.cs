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
        Dispatcher dispatcher;
        Thread updator;

        public ObservableCollection<Computer> Computers { get; set; }
        private Computer selectComputer;
        
        public ObservableCollection<FileItem> FileTree { get; set; }
        private FileItem selectedFile;
        

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

        public FileItem SelectedFile
        {
            get { return selectedFile; }
            set
            {
                selectedFile = value;
                OnPropertyChanged();
            }
        }

        public ApplicationViewModel(Dispatcher dis)
        {
            dispatcher = dis;
            myComputer = new ComputerServer();
            myComputer.AddNewComputer += AddNewComputer;
            myComputer.AddNewFileItem += AddNewFileItem;

            Computers = new ObservableCollection<Computer>();
            FileTree = new ObservableCollection<FileItem>();
        }

        private void AddNewComputer(Computer computer)
        {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, 
                (ThreadStart)delegate () 
                { 
                    Computers.Add(computer); 
                });
        }

        private void AddNewFileItem(FileItem item)
        {
            dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    FileTree.Add(item);
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

        private RelayCommand openOrDownload;
        public RelayCommand OpenOrDownload
        {
            get
            {
                return openOrDownload ??
                    (openOrDownload = new RelayCommand(obj =>
                    {
                        if (selectedFile.IsFolder)
                        {
                            FileTree.Clear();
                            myComputer.OpenFolder(selectedFile.Name);
                        }
                    }));
            }
        }

        private RelayCommand backFolder;
        public RelayCommand BackFolder
        {
            get
            {
                return backFolder ??
                    (backFolder = new RelayCommand(obj =>
                    {
                        FileTree.Clear();
                        myComputer.OpenFolder("-");
                    }));
            }
        }

        public void CloseApplication()
        {
            myComputer.StopServer();
        }
    }
}
