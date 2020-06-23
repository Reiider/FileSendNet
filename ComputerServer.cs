using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FileSendNet
{
    class ComputerServer : Computer
    {
        Socket socketSender;
        Thread threadServer;
        Thread threadClient;
        bool isFreeServer;
        bool isFile;
        FileInfo fileInfo;
        long lenghtLastFile;
        double valueProgressBar;

        Computer connectedClient;
        bool fileSending;

        public delegate void NewComputer(Computer computer);
        public event NewComputer AddNewComputer;


        public ComputerServer() : base(Dns.GetHostName(), Dns.GetHostEntry(Dns.GetHostName()).AddressList[1].ToString())
        {
            isFreeServer = true;
            isFile = false;
            
            valueProgressBar = 0;
            fileSending = false;
            threadServer = new Thread(new ThreadStart(Server));
            threadServer.Start();
        }

        public double ValueProgressBar
        {
            get { return valueProgressBar; }
            set
            {
                valueProgressBar = value;
                OnPropertyChanged();
            }
        }

        private void Server()
        {
            mainSocket.Bind(ipPoint); //не позволяет запустить 2ую копию приложения... Фича
            mainSocket.Listen(10);

            GetMessages();
        }

        private void GetMessages()
        {
            while (true)
            {
                try
                {

                    Socket sender = mainSocket.Accept();
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];
                    do
                    {
                        bytes = sender.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (sender.Available > 0);

                    string[] message = builder.ToString().Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
                    switch(message[0]){
                        case "0": //сервер существует?
                            {
                                SendMessage($"1|{nickName}", sender, true);//да
                                break;
                            }
                        case "2": //пытаюсь занять сервер
                            {
                                if (isFreeServer)
                                {
                                    isFreeServer = false;
                                    socketSender = sender;
                                    threadClient = new Thread(GetMessageFromConnectedClient);
                                    threadClient.Start();
                                    SendMessage("3", socketSender, false); //сервер успешно занят
                                }
                                else
                                {
                                    SendMessage("4", sender, true); //ошибка, не удалось занять сервер
                                }
                                break;
                            }
                        default: break;
                    }
                }
                catch { }
            }
        }

        private void GetMessageFromConnectedClient()
        {
            FileStream fs = null; //поток для записи файла
            long lenghtFile = 0; //текущий размер файла
            while (true)
            {
                try
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];
                    do
                    {
                        bytes = socketSender.Receive(data);
                        if (isFile)
                        {
                            lenghtFile += bytes;
                            if (lenghtFile > lenghtLastFile) throw new Exception("Ошибка размера файла. Размер файла больше предполагаемого.");
                            fs.Write(data, 0, bytes);
                            ValueProgressBar = lenghtFile*100 / lenghtLastFile;
                            if(lenghtFile == lenghtLastFile)
                            {
                                isFile = false;
                                fs.Close();
                            }
                        }
                        else
                        {
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                    }
                    while (socketSender.Available > 0);

                    if(isFile && lenghtFile != lenghtLastFile) throw new Exception("Ошибка размера файла. Размер файла меньше предпологаемого.");

                    string[] message = builder.ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    switch (message[0])
                    {
                        case "5": //передаю файл - дальше имя файла [1] и размер [2] (для контроля полноты передачи)
                            {

                                fileInfo = new FileInfo(LocalPath + @"\" + message[1]);
                                lenghtLastFile = long.Parse(message[2]);
                                if (fileInfo.Exists)
                                {
                                    if(lenghtLastFile == fileInfo.Length)
                                    {
                                        SendMessage("6", socketSender, false); //файл существует
                                        lenghtFile = lenghtLastFile;
                                    }
                                    else
                                    {
                                        SendMessage("7|" + fileInfo.Length, socketSender, false); //файл существует, и его текущий размер такой-то 
                                        lenghtFile = fileInfo.Length;
                                    }
                                }
                                else
                                {
                                    SendMessage("8", socketSender, false); //файл не существует
                                    lenghtFile = 0;
                                }
                                break;
                            }
                        case "9": // отправляю файл (на запись/дозапись)
                            {
                                isFile = true;
                                fs = fileInfo.Open(FileMode.Append);
                                break;
                            }
                        default: break;
                    }
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show(e.Message);
                }
                isFile = false;
            }
        }

        private void SendMessage(string message, Socket sendTo, bool needClose)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            sendTo.Send(data);

            if(needClose) CloseSocket(sendTo);
        }

        private void GetMessageFrom(Socket socket, bool needClose, object ipORcomp = null)
        {
            StringBuilder builder = new StringBuilder();
            int bytes;
            byte[] data = new byte[256];
            do
            {
                bytes = socket.Receive(data);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (socket.Available > 0);

            string[] message = builder.ToString().Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
            switch (message[0])
            {
                case "1": //сервер существует
                    {
                        AddNewComputer(new Computer(message[1], (string)ipORcomp));
                        break;
                    }
                case "3"://сервер успешно занят
                    {
                        connectedClient = (Computer)ipORcomp;
                        break;
                    }
                case "4": //не удалось занять сервер
                    {
                        break;
                    }
                default : break;
            }

            if (needClose) CloseSocket(socket);
        }

        public bool SendTo(string[] path) //передать файлы подключенному пк
        {

            return false;
        }

        public async void ConnectWith(Computer client)// переключится к данному пк Если подключение удачно возвращает объект, иначе NULL
        {
            //если файл передается то не переключать
            if (fileSending) return;//return "Ошибка. Идет переача файла.";

            client.MainSocket.Connect(client.IpPoint);
            SendMessage("0", client.MainSocket, false);
            await Task.Run(() => GetMessageFrom(client.MainSocket, false, client));
        }

        public async void CanConnectWith(string ip) //можно ли подключится по данному адресу
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(endPoint);
                SendMessage("0", socket, false);
                await Task.Run(() => GetMessageFrom(socket, true, ip));
            }
            catch
            {
                socket.Close();
            }
            
        }

        public void StopServer()
        {
            threadServer.Abort();
        }
    }
}
