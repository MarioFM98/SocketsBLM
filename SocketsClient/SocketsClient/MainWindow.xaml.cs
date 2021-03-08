using System;
using System.Windows;
using System.Text;
using System.Management;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SocketsClient
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private  readonly Socket ClientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private bool connected = false;

        private const int PORT = 100;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ConnectToServer();
        }


        #region Socket Methods


        private void ConnectToServer()
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Intento de conexion: " + attempts);
                    ClientSocket.Connect(IPAddress.Loopback, PORT);
                }
                catch (SocketException)
                {
                    // Se debe manejar la excepción para que el intento de conexión continue
                }
            }

            connected = true;

            Console.WriteLine("Conectado");
            this.Dispatcher.Invoke(() => {
                lblConnectionStatus.Content = "Conectado";
                cmbRequest.IsEnabled = connected;
                btnSendRequest.IsEnabled = connected;
            });
        }


        /// <summary>
        /// Close socket and exit program.
        /// </summary>
        private void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Receive);
            ClientSocket.Close();
            Environment.Exit(0);
        }


        private void SendRequest(string request)
        {
            SendString(request);
            if (request.ToLower() == "exit")
            {
                Exit();
            }
        }


        /// <summary>
        /// Sends a string to the server with ASCII encoding.
        /// </summary>
        private  void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }


        private  void ReceiveResponse()
        {
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            switch (cmbRequest.SelectedIndex)
            {
                case 0:
                    All all = JsonConvert.DeserializeObject<All>(text);
                    text = all.MemoryRam.TotalFreeSpace.ToString();
                    break;
                case 1:
                    List<GPU> videoControllers = JsonConvert.DeserializeObject<List<GPU>>(text);
                    text = videoControllers.First().Name;
                    break;
                case 2:
                    List<Storage> storages = JsonConvert.DeserializeObject<List<Storage>>(text);
                    text = storages.First().RootDirectory;
                    break;
                case 3:
                    MemoryRam memoryRam = JsonConvert.DeserializeObject<MemoryRam>(text);
                    text = memoryRam.TotalPhysicalMemory.ToString();
                    break;
            }
            Console.WriteLine(text);
            this.Dispatcher.Invoke(() => {
                lblResponse.Content = text;
            });
        }
        #endregion


        private void btnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbRequest.SelectedIndex;
            string request = "";

            if (index == 0)
            {
                request = "getAll";
            }
            else if (index == 1)
            {
                request = "getVideoController";
            }
            else if (index == 2)
            {
                request = "getStorage";
            }
            else if (index == 3)
            {
                request = "getMemoryRam";
            }
            else
            {
                request = "exit";
            }

            SendRequest(request);
            ReceiveResponse();
        }
    }
}
