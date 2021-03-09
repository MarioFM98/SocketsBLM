using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Management;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace SocketsServer
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private string IP = "192.168.0.13";
        private readonly byte[] buffer = new byte[BUFFER_SIZE];


        public MainWindow()
        {
            InitializeComponent();
        }


        private void SetupServer()
        {
            Console.WriteLine("Levantando Servidor...");
            lblServerStatus.Content = "Levantando Servidor...";

            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(IP), PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);

            Console.WriteLine("Servidor Activo");
            lblServerStatus.Content = "Servidor Activo";
        }


        #region Sockets Methods


        /// <summary>
        /// Método recursivo para aceptar diferentes conexiones simultaneas
        /// </summary>
        /// <param name="AR"></param>
        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);

            Console.WriteLine("Cliente conectado, esperando petición...");

            serverSocket.BeginAccept(AcceptCallback, null);
        }


        /// <summary>
        /// Método para recibir la petición de un cliente
        /// </summary>
        /// <param name="AR"></param>
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Cliente desconectado forzadamente");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            Console.WriteLine("Texto Recibido: " + text);
            this.Dispatcher.Invoke(() => {
                lblReceived.Content = text;
            });

            if (text == "getVideoController") // Información de tarjetas gráficas
            {
                Console.WriteLine("Text is a get video controller request");
                List<GPU> videoControllers = new List<GPU>();
                ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in myVideoObject.Get())
                {
                    videoControllers.Add(new GPU()
                    {
                        Name = obj["Name"].ToString(),
                        Status = obj["Status"].ToString(),
                        AdapterRAM = obj["AdapterRAM"].ToString(),
                        AdapterDACType = obj["AdapterDACType"].ToString(),
                        DriverVersion = obj["DriverVersion"].ToString()
                    });
                }
                string result = JsonConvert.SerializeObject(videoControllers);
                byte[] data = Encoding.ASCII.GetBytes(result);
                current.Send(data);
                Console.WriteLine("Info sent to client");
            } else if (text == "getStorage") // Información de los discos de almacenamiento
            {
                Console.WriteLine("Text is a get storage request");
                List<Storage> storages = new List<Storage>();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady == true)
                    {
                        storages.Add(new Storage()
                        {
                            TotalAvailableSpace = d.TotalFreeSpace,
                            TotalSizeOfDrive = d.TotalSize,
                            RootDirectory = d.RootDirectory.Name
                        });
                    }
                }
                string result = JsonConvert.SerializeObject(storages);
                byte[] data = Encoding.ASCII.GetBytes(result);
                current.Send(data);
                Console.WriteLine("Info sent to client");
            }
            else if (text == "getMemoryRam") // Información de la memoria ram
            {
                Console.WriteLine("Text is a get memory ram request");
                PerformanceCounter ram = new PerformanceCounter();
                ComputerInfo infoDevice = new ComputerInfo();
                ram.CategoryName = "Memory";
                ram.CounterName = "Available Bytes";
                MemoryRam memoryRam = new MemoryRam()
                {
                    TotalPhysicalMemory = infoDevice.TotalPhysicalMemory,
                    TotalFreeSpace = ram.NextValue()
                };
                string result = JsonConvert.SerializeObject(memoryRam);
                byte[] data = Encoding.ASCII.GetBytes(result);
                current.Send(data);
                Console.WriteLine("Info sent to client");
            }
            else if (text == "getAll") // Toda la información
            {
                Console.WriteLine("Text is a get all request");
                All all = new All()
                {
                    GPUs = new List<GPU>(),
                    Storages = new List<Storage>()
                };
                ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in myVideoObject.Get())
                {
                    all.GPUs.Add(new GPU()
                    {
                        Name = obj["Name"].ToString(),
                        Status = obj["Status"].ToString(),
                        AdapterRAM = obj["AdapterRAM"].ToString(),
                        AdapterDACType = obj["AdapterDACType"].ToString(),
                        DriverVersion = obj["DriverVersion"].ToString()
                    });
                }
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady == true)
                    {
                        all.Storages.Add(new Storage()
                        {
                            TotalAvailableSpace = d.TotalFreeSpace,
                            TotalSizeOfDrive = d.TotalSize,
                            RootDirectory = d.RootDirectory.Name
                        });
                    }
                }
                PerformanceCounter ram = new PerformanceCounter();
                ComputerInfo infoDevice = new ComputerInfo();
                ram.CategoryName = "Memory";
                ram.CounterName = "Available Bytes";
                all.MemoryRam = new MemoryRam()
                {
                    TotalPhysicalMemory = infoDevice.TotalPhysicalMemory,
                    TotalFreeSpace = ram.NextValue()
                };
                string result = JsonConvert.SerializeObject(all);
                byte[] data = Encoding.ASCII.GetBytes(result);
                current.Send(data);
                Console.WriteLine("Info sent to client");
            }
            else if (text == "exit")
            {
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Cliente desconectado");
                return;
            }
            else
            {
                Console.WriteLine("Peticion Invalida");
                byte[] data = Encoding.ASCII.GetBytes("Peticion Invalida");
                current.Send(data);
                Console.WriteLine("Alerta Enviada");
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
        #endregion


        private void btnSetUpServer_Click(object sender, RoutedEventArgs e)
        {
            IP = txtIp.Text;
            SetupServer();
            btnSetUpServer.IsEnabled = false;
            txtIp.IsEnabled = false;
        }
    }
}
