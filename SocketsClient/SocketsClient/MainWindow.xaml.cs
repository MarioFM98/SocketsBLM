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
using LiveCharts;
using LiveCharts.Wpf;
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
            hidden_card();
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
                    ClientSocket.Connect(IPAddress.Parse("127.0.0.1"), PORT);
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
                btnSendRequests.IsEnabled = connected;
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
            txtjson.Text = string.Empty;
            var buffer = new byte[2048];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);
            string json = "";
            switch (cmbRequest.SelectedIndex)
            {
                case 0:

                    cardram.Visibility = Visibility.Hidden;
                    hidden_card();
                    hidden_video();
                    All all = JsonConvert.DeserializeObject<All>(text);
                    text = all.MemoryRam.TotalFreeSpace.ToString();

                    json = JsonConvert.SerializeObject(all, Formatting.Indented);                    
                    txtjson.Text = json;

                    
                    break;
                case 1:
                    cardram.Visibility = Visibility.Hidden;
                    hidden_card();


                    List<GPU> videoControllers = JsonConvert.DeserializeObject<List<GPU>>(text);
                    ListGPUS(videoControllers);
                    text = videoControllers.First().Name;
                    json = JsonConvert.SerializeObject(videoControllers, Formatting.Indented);
                    txtjson.Text = json;
                    break;
                case 2:

                    cardram.Visibility = Visibility.Hidden;
                    hidden_video();
                    List<Storage> storages = JsonConvert.DeserializeObject<List<Storage>>(text);
                    Storages(storages);
                    
                    text = storages.First().RootDirectory;
                    json = JsonConvert.SerializeObject(storages, Formatting.Indented);
                    txtjson.Text = json;
                    break;
                case 3:

                    hidden_video();
                    hidden_card();

                    MemoryRam memoryRam = JsonConvert.DeserializeObject<MemoryRam>(text);
                    Ram(memoryRam);
                    text = memoryRam.TotalPhysicalMemory.ToString();
                    json = JsonConvert.SerializeObject(memoryRam, Formatting.Indented);
                    txtjson.Text = json;

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
           
        }
        void ListGPUS(List<GPU> listgpu) {

            if (listgpu.Count()==1)
            {
                txtgrafic1.Text = listgpu[0].Name;
                txtresname.Text= listgpu[0].Name;
                txtresstatus.Text = listgpu[0].Status;
                txtresAdapter.Text = listgpu[0].AdapterRAM;
                txtresAdapterDAC.Text = listgpu[0].AdapterDACType;
                txtresDriver.Text = listgpu[0].DriverVersion;

                card_video1.Visibility = Visibility.Visible;
                card_video2.Visibility = Visibility.Hidden;
            }
            if (listgpu.Count() == 2)
            {
                txtgrafic1.Text = listgpu[0].Name;
                txtresname.Text = listgpu[0].Name;
                txtresstatus.Text = listgpu[0].Status;
                txtresAdapter.Text = listgpu[0].AdapterRAM;
                txtresAdapterDAC.Text = listgpu[0].AdapterDACType;
                txtresDriver.Text = listgpu[0].DriverVersion;

                txtgrafic2.Text = listgpu[1].Name;
                txtresname1.Text = listgpu[1].Name;
                txtresstatus1.Text = listgpu[1].Status;
                txtresAdapter1.Text = listgpu[1].AdapterRAM;
                txtresAdapterDAC1.Text = listgpu[1].AdapterDACType;
                txtresDriver1.Text = listgpu[1].DriverVersion;

                card_video1.Visibility = Visibility.Visible;
                card_video2.Visibility = Visibility.Visible;
            }

        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
        List<string> list_storages = new List<string>();
        Func<ChartPoint, string> labelPoint = chartpoint => string.Format("{0} ({1:P})", chartpoint.Y, chartpoint.Participation);
        void Storages(List<Storage> List_storages) {
            list_storages.Clear();
            list_storages.Add("Total Available Space");
            list_storages.Add("Total used Space");
            SeriesCollection series1 = new SeriesCollection();
            SeriesCollection series2 = new SeriesCollection();
            SeriesCollection series3 = new SeriesCollection();
            SeriesCollection series4 = new SeriesCollection();
            
            if (List_storages.Count()==1)
            {
                series1.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[0].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                series1.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[0].TotalSizeOfDrive - List_storages[0].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                txtstorage1.Text = "Disco: " + List_storages[0].RootDirectory;
                
                card1.Visibility=Visibility.Visible;
                card2.Visibility = Visibility.Hidden;
                card3.Visibility = Visibility.Hidden;
                card4.Visibility = Visibility.Hidden;
            }
            if (List_storages.Count() == 2)
            {
                series1.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[0].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                series1.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[0].TotalSizeOfDrive - List_storages[0].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                txtstorage1.Text = "Disco: " + List_storages[0].RootDirectory;

                series2.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[1].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                series2.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[1].TotalSizeOfDrive - List_storages[1].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                txtstorage2.Text = "Disco: " + List_storages[1].RootDirectory;

                card1.Visibility = Visibility.Visible;
                card2.Visibility = Visibility.Visible;
                card3.Visibility = Visibility.Hidden;
                card4.Visibility = Visibility.Hidden;
            }
            if (List_storages.Count() == 3)
            {
                series1.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[0].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                series1.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[0].TotalSizeOfDrive - List_storages[0].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                txtstorage1.Text = "Disco: " + List_storages[0].RootDirectory;

                series2.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[1].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                series2.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[1].TotalSizeOfDrive - List_storages[1].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                txtstorage2.Text = "Disco: " + List_storages[1].RootDirectory;

                series3.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[2].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart3.Series = series3;
                series3.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[2].TotalSizeOfDrive - List_storages[2].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart3.Series = series3;
                txtstorage3.Text = "Disco: " + List_storages[2].RootDirectory;

                card1.Visibility = Visibility.Visible;
                card2.Visibility = Visibility.Visible;
                card3.Visibility = Visibility.Visible;
                card4.Visibility = Visibility.Hidden;
            }
            if (List_storages.Count() == 4)
            {
                series1.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[0].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                series1.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[0].TotalSizeOfDrive - List_storages[0].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart1.Series = series1;
                txtstorage1.Text = "Disco: " + List_storages[0].RootDirectory;

                series2.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[1].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                series2.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[1].TotalSizeOfDrive - List_storages[1].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart2.Series = series2;
                txtstorage2.Text = "Disco: " + List_storages[1].RootDirectory;

                series3.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[2].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart3.Series = series3;
                series3.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[2].TotalSizeOfDrive - List_storages[2].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart3.Series = series3;
                txtstorage3.Text = "Disco: " + List_storages[2].RootDirectory;

                series4.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { List_storages[3].TotalAvailableSpace }, DataLabels = true, LabelPoint = labelPoint });
                piechart4.Series = series4;
                series4.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (List_storages[3].TotalSizeOfDrive - List_storages[3].TotalAvailableSpace) }, DataLabels = true, LabelPoint = labelPoint });
                piechart4.Series = series4;
                txtstorage4.Text = "Disco: " + List_storages[3].RootDirectory;

                card1.Visibility = Visibility.Visible;
                card2.Visibility = Visibility.Visible;
                card3.Visibility = Visibility.Visible;
                card4.Visibility = Visibility.Visible;
            }

        }

        void Ram(MemoryRam ram) {
            cardram.Visibility = Visibility.Visible;
            list_storages.Clear();
            list_storages.Add("Total Available Space");
            list_storages.Add("Total used Space");
            SeriesCollection series = new SeriesCollection();
            series.Add(new PieSeries() { Title = list_storages[0], Values = new ChartValues<double> { ram.TotalFreeSpace }, DataLabels = true, LabelPoint = labelPoint });
            piechart_ram.Series = series;
            series.Add(new PieSeries() { Title = list_storages[1], Values = new ChartValues<double> { (ram.TotalPhysicalMemory - ram.TotalFreeSpace) }, DataLabels = true, LabelPoint = labelPoint });
            piechart_ram.Series = series;
            txtram.Text = "Memory Ram: " ;
            txtphysicalram.Text = ram.TotalPhysicalMemory.ToString();
            txtspaceram.Text = ram.TotalFreeSpace.ToString();
        }
        private void BtnSendRequests1_Click(object sender, RoutedEventArgs e)
        {
            int index = cmbRequest.SelectedIndex;
            string request = "";

            if (index == 0)
            {
                request = "getAll";
                hidden_card();
            }
            else if (index == 1)
            {
                request = "getVideoController";
                hidden_card();
            }
            else if (index == 2)
            {
                request = "getStorage";
                
            }
            else if (index == 3)
            {
                request = "getMemoryRam";
                hidden_card();
            }
            else
            {
                request = "exit";
            }

            SendRequest(request);
            ReceiveResponse();
        }
        void hidden_card() {
            card1.Visibility = Visibility.Hidden;
            card2.Visibility = Visibility.Hidden;
            card3.Visibility = Visibility.Hidden;
            card4.Visibility = Visibility.Hidden;
        }
        void hidden_video() {
            card_video1.Visibility = Visibility.Hidden;
            card_video2.Visibility = Visibility.Hidden;
        }
        void clear_Grafics() {

            txtgrafic1.Text = string.Empty;
            txtresname.Text = string.Empty;
            txtresstatus.Text = string.Empty;
            txtresAdapter.Text = string.Empty;
            txtresAdapterDAC.Text = string.Empty;
            txtresDriver.Text = string.Empty;

            txtgrafic2.Text = string.Empty;
            txtresname1.Text = string.Empty;
            txtresstatus1.Text = string.Empty;
            txtresAdapter1.Text = string.Empty;
            txtresAdapterDAC1.Text = string.Empty;
            txtresDriver1.Text = string.Empty;
        }
    }
}
