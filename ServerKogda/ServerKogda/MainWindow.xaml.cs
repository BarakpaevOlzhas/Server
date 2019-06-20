using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    // Объект сокета сервера
    TcpListener sockServer = null;
    // рабочий поток сервера
    Thread thServer = null;
    // флаг запуска сервера
    bool bIsStart = false;

        List<ClientInfo> ListClients = new List<ClientInfo>();

    public MainWindow()
    {
      InitializeComponent();
      sockServer = null;
      thServer = null;
      bIsStart = false; // сервер не запущен
      cbIpServer.Items.Add("0.0.0.0");
      cbIpServer.Items.Add("127.0.0.1"); // localhost
      IPHostEntry ipSrv =
        Dns.GetHostEntry(Dns.GetHostName());
      foreach (var a in ipSrv.AddressList)
      {
        cbIpServer.Items.Add(a.ToString());
      }
            ListClients = new List<ClientInfo>();
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
      if (!bIsStart)
      {// запуск сервера
        sockServer = new TcpListener(
  IPAddress.Parse(cbIpServer.SelectedItem.ToString()),
  int.Parse(txtPort.Text));
        // начало прослушивания порта сервера
        sockServer.Start(100);
        // запуск потока сервера
        thServer = new Thread(ServerThreadProc);
        thServer.Start(sockServer);

        btnStart.Content = "Stop";
        bIsStart = true;
      }
      else
      { // останов запущенного сервера


        //bIsStart = !bIsStart;
        bIsStart = false;
      }
    } // BtnStart_Click();
    void ServerThreadProc(object obj)
    {
      TcpListener srvSocket = (TcpListener)obj;
      while (true) {
        // ожидание клиента через асинхронный метод
        //  BeginAcceptTcpClient()
        IAsyncResult iaRes =
          srvSocket.BeginAcceptTcpClient(
                      AcceptClientProc, srvSocket);
        // ожидание завершения асинхронного
        //  соединения со стороны клиента
        iaRes.AsyncWaitHandle.WaitOne();
      }
    } // ServerThreadProc()
    // txtLog.Text += str;
    public void SaveToLog(string str)
    {
      //txtLog.Text += str;
      Dispatcher.Invoke(new Action(
        () => { txtLog.AppendText(str); }));
    }
    void AcceptClientProc(IAsyncResult iaRes) {
      TcpListener srvSocket =
        (TcpListener)iaRes.AsyncState;
      TcpClient client =
        srvSocket.EndAcceptTcpClient(iaRes);
      SaveToLog("Клиент соединился с сервером\r\n");
      SaveToLog("Адрес клиента: " +
        client.Client.RemoteEndPoint.ToString());
      
      // Запуск потока для работы с клиентом
      //  в пуле потоков процесса
      // Передаем рабочий объект (сокет) типа
      //  TcpClient
      ThreadPool.QueueUserWorkItem(
        ClientThreadProc, client);
    } // AcceptClientProc()

    void ClientThreadProc(object obj) {
      TcpClient clientSocket = (TcpClient)obj;
      byte[] RecBuf = new byte[4 * 1024]; // 4 Kb
                                          // Необходимо реализовать протокол 
                                          // общения сервера с клиентом
      int recSize = clientSocket.Client.Receive(RecBuf);
            string userName = Encoding.UTF8.GetString(RecBuf, 0,recSize);            

            clientSocket.Client.Send(Encoding.UTF8.GetBytes("Hello " + userName + "!"));

            var vv = new ClientInfo
            {
                Name = userName,
                Client = clientSocket
            };

            ListClients.Add(vv);
      while (true) {
        //clientSocket.ReceiveTimeout = 200;
        recSize = clientSocket.Client.Receive(RecBuf);
        if(recSize <= 0) {
          // связь разована клиентом
          break;
        }
                // ответ серверу
                foreach (var i in ListClients)
                {
                    if (i.Client.Client.Connected) {
                        i.Client.Client.Send(RecBuf, recSize, SocketFlags.None);
                    }                    
                }
                ListClients.Remove(vv);
      }
    } // ClientThreadProc();
  } // class MainWindow

}
