using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileSendNet
{
    class ComputerServer : Computer
    {
        Socket socketSender;
        Thread threadServer;
        bool isFreeServer;
        bool isFile;
        FileInfo fileInfo;
        

        public ComputerServer(string nickName, string ip) : base(nickName, ip)
        {
            isFreeServer = true;
            isFile = false;
            threadServer = new Thread(Server);
            threadServer.Start();
        }

        private void Server()
        {
            socket.Bind(ipPoint);
            socket.Listen(10);

            GetMessages();
        }

        private void GetMessages()
        {
            while (true)
            {
                try
                {
                    socketSender = socket.Accept();
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];
                    do
                    {
                        bytes = socketSender.Receive(data);
                        if (!isFile)
                        {
                            builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        else // записывать полученные байты в файл - сразу
                        {

                        }
                        
                    }
                    while (socketSender.Available > 0);

                    string[] message = builder.ToString().Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
                    switch(message[0]){
                        case "0": //сервер свободен?
                            {
                                if (isFreeServer) SendMessage("1");// да свободен
                                else SendMessage("2");// нет, занят
                                break;
                            }
                        case "3": //пытаюсь занять сервер
                            {
                                if (isFreeServer)
                                {
                                    isFreeServer = false;
                                    SendMessage("4"); //сервер успешно занят
                                }
                                else
                                {
                                    SendMessage("5"); //ошибка, не удалось занять сервер
                                }
                                break;
                            }
                        case "6": //передаю файл - дальше имя файла [1] и размер [2] (для контроля полноты передачи)
                            {
                                fileInfo = new FileInfo(LocalPath + @"\" + message[1]);
                                if (fileInfo.Exists)
                                {
                                    //дописывать или качать по новой, если дописывать, то нужно отправлять текущий размер
                                }
                                break;
                            }
                        default: break;
                    }
                }
                catch { }
            }
        }

        private void SendMessage(string message)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            socketSender.Send(data);

            CloseSocketSender();
        }

        private void CloseSocketSender()
        {
            socketSender.Shutdown(SocketShutdown.Both);
            socketSender.Close();
        }

    }
}
