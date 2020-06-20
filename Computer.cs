using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Hosting;
using System.Text;
using System.Threading.Tasks;

namespace FileSendNet
{
    class Computer : INotifyPropertyChanged
    {
        private string nickName;
        private IPAddress ip;
        private string localPath;

        protected IPEndPoint ipPoint;
        protected Socket socket;

        protected static int port = 8005;


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
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            localPath = @".\Download";
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

        public bool isConnectWith(string ip) //можно ли подключится по данному адресу
        {

            return false;
        }

        public Computer ConnectWith(string ip)// подключится к данному пк Если подключение удачно возвращает объект, иначе NULL
        {

            return null;
        }

        public bool SendTo(string[] path, Computer comp) //передать файлы
        {

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public void client()
        {
            try
            {
                socket.Connect(ipPoint);
                string mes = "";
                byte[] data = Encoding.Unicode.GetBytes(mes);
                socket.Send(data);

                //get answer

                data = new byte[256];
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = socket.Receive(data);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);


                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch { }
        }
    }
}
