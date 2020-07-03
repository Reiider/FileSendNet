using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace FileSendNet
{
    class ComputerServer : Computer
    {
        Socket socketSender; //клиент соединенный с сервером
        Thread threadServer; //поток входящих подключений
        Thread threadClient; //поток входящих сообщений от подключенного клиента
        bool isFreeServer; //true - нет подключенного клиента, false - есть
        bool isFile; // true - в данный момент передается файл
        FileInfo fileInfo;
        long lenghtLastFile;
        double valueProgressBar;
        string fromFolder = @".\Download"; //папка открывающаяся на удаленном пк

        Computer connectedClient;
        bool fileSending;

        public delegate void NewComputer(Computer computer);
        public event NewComputer AddNewComputer;
        public delegate void NewFileItem(FileItem item);
        public event NewFileItem AddNewFileItem;

        public ComputerServer() : base(Dns.GetHostName(), Dns.GetHostEntry(Dns.GetHostName()).AddressList[1].ToString())
        {
            isFreeServer = true;
            isFile = false;
            
            valueProgressBar = 0;
            fileSending = false;
            threadServer = new Thread(new ThreadStart(Server));
            threadServer.Start();

            if (!Directory.Exists(@".\Download"))
            {
                Directory.CreateDirectory(@".\Download");
            }
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
            try
            {
                mainSocket.Bind(ipPoint);
            }
            catch
            {
                ipPoint = new IPEndPoint(this.ip, port+1);
                mainSocket.Bind(ipPoint);
            }//не позволяет запустить 2ую копию приложения... Фича
            mainSocket.Listen(10);

            GetMessages();
        }

        private void GetMessages() // ожидает входящих подключений (клиентов)
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
                    switch(message[0]){ //сообщение от клиента
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

                                    //получить папки и файлы от клиента
                                    SendMessage("10", socketSender, false);
                                    SendMessage("12", socketSender, false);
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

        private void GetMessageFromConnectedClient() //ожидает входящий сообщений от подключенного клиента
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
                        case "5": //клиент собирается передавать файл - дальше имя файла [1] и размер [2] (для контроля полноты передачи)
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
                        case "6": //файл существует (и размер совпадает)
                            {

                                break;
                            }
                        case "7": //файл существует, и его текущий размер такой-то 
                            {

                                break;
                            }
                        case "8": //файл не существует
                            {

                                break;
                            }
                        case "9": //клиент будет отправлять файл (на запись/дозапись)
                            {
                                isFile = true;
                                fs = fileInfo.Open(FileMode.Append);
                                break;
                            }
                        case "10": //клиент зашивает список папок в катологе
                            {
                                string[] dirs = Directory.GetDirectories(fromFolder);
                                string listFolders = "11|";
                                foreach (string s in dirs)
                                {
                                    listFolders += s.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries).Last() + "|";
                                }

                                SendMessage(listFolders, socketSender, false);
                                //
                                break;
                            }
                        case "11": //получены названия папок с удаленного пк
                            {
                                for(int i = 1; i < message.Length; i++)
                                {
                                    AddNewFileItem(new FileItem(message[i], true));
                                }
                                break;
                            }
                        case "12": //клиент зашивает список файлов в катологе
                            {
                                string[] dirs = Directory.GetFiles(fromFolder);
                                string listFiles = "13|";
                                foreach (string s in dirs)
                                {
                                    listFiles += s.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries).Last() + "|";
                                }

                                SendMessage(listFiles, socketSender, false);
                                break;
                            }
                        case "13": //получены названия файлов с удаленного пк
                            {
                                for (int i = 1; i < message.Length; i++)
                                {
                                    AddNewFileItem(new FileItem(message[i], false));
                                }
                                break;
                            }
                        case "14": //запрос на переход в каталог
                            {
                                if (message.Length > 1)
                                {
                                    if (message[1] == "-") //возвращаемся в предыдущий каталог
                                    {
                                        string[] lf = fromFolder.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                                        if (lf.Last() != "Download") //если это не корень то возвращаемся
                                        {
                                            fromFolder = "";
                                            
                                            for(int i = 0; i < lf.Length - 1; i++)
                                            {
                                                fromFolder += @"\" + lf[i];
                                            }
                                        }
                                    }
                                    else
                                    {
                                        fromFolder += @"\" + message[1];
                                    }
                                }
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

        private void SendMessage(string message, Socket sendTo, bool needClose = false) //отправляет сообщение
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            sendTo?.Send(data);

            if(needClose) CloseSocket(sendTo);
        }

        public void ConnectWith(Computer client)// переключится к данному пк
        {
            //если файл передается то не переключать
            if (isFile) return;//return "Ошибка. Идет переача файла.";

            client.MainSocket.Connect(client.IpPoint);
            SendMessage("2", client.MainSocket, false);
            GetMessageFrom(client.MainSocket, false, client);
        }

        public void CanConnectWith(object strIp) //можно ли подключится по данному адресу
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse((string)strIp), port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(endPoint);
                SendMessage("0", socket, false);
                GetMessageFrom(socket, true, (string)strIp);
            }
            catch
            {
                socket.Close();
            }
        }


        private void GetMessageFrom(Socket socket, bool needClose, object ipORcomp = null) //сразу получает ответное сообщение (не ждет входящих), только до установления соединения
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

                        isFreeServer = false;
                        socketSender = socket;
                        threadClient = new Thread(GetMessageFromConnectedClient);
                        threadClient.Start();

                        //получить файлы и папки от клиента
                        SendMessage("10", socketSender, false);
                        SendMessage("12", socketSender, false);
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

        
        public void OpenFolder(string nameFolder)// открыть выбранную папку на удаленном пк
        {
            SendMessage("14|" + nameFolder, socketSender);
            //получить папки и файлы от клиента
            SendMessage("10", socketSender);
            SendMessage("12", socketSender);

        }

        public void StopServer()
        {
            threadServer.Abort();
        }
    }
}
