using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace FileSendNet
{
    class Computer : INotifyPropertyChanged
    {
        protected string nickName;
        protected IPAddress ip;
        protected string localPath;

        protected IPEndPoint ipPoint;
        protected Socket mainSocket;

        protected static int port = 8005;

        protected string stringObj = "123sdsad";


        public Computer(string nickName, string ip)
        {
            NickName = nickName;
            
            try
            {
                this.ip = IPAddress.Parse(ip);
            }
            catch
            {
                this.ip = IPAddress.Parse("127.0.0.1");
            }
            finally
            {
                OnPropertyChanged();
            }
            ipPoint = new IPEndPoint(this.ip, port);
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            localPath = @".\Download";
            StringObj = NickName + " " + Ip;
        }

        public string StringObj
        {
            get { return stringObj; }
            set
            {
                stringObj = NickName + " " + Ip;
                OnPropertyChanged();
            }
        }
        public string NickName
        {
            get { return nickName; }
            set
            {
                nickName = value;
                OnPropertyChanged();
            }
        }

        public string Ip
        {
            get { return ip.ToString(); }
            set
            {
                try
                {
                    ip = IPAddress.Parse(value);
                    OnPropertyChanged();
                }
                catch 
                { 
                    OnPropertyChanged(); 
                }
            }
        }

        public string LocalPath
        {
            get { return localPath; }
            set
            {
                localPath = value;
                OnPropertyChanged();
            }
        }

        public IPEndPoint IpPoint
        {
            get { return ipPoint; }
            set
            {
                ipPoint = value;
                OnPropertyChanged();
            }
        }

        public Socket MainSocket
        {
            get { return mainSocket; }
            set
            {
                mainSocket = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        protected void CloseSocket(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
