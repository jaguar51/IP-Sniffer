using System;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Collections;

namespace Http
{
    public partial class MainWindow : Window
    {
        private String nameWindow = "Sniffer";
        private Socket mainSocket;                          //The socket which captures all incoming packets
        private byte[] byteData = new byte[4096];
        private bool bContinueCapturing = false;            //A flag to check if packets are to be captured or not

        private ObservableCollection<IPItem> listIPItems = new ObservableCollection<IPItem>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += WindowLoaded;
            Closed += WindowClosed;
        }

        public void StartSniffingClick(object sender, RoutedEventArgs e)
        {           
            if (combInterfaces.Text == "")
            {
                MessageBox.Show("Select an Interface to capture the packets.", nameWindow,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!bContinueCapturing)
                {
                    startSniffingButton.Content = "Stop";
                    combInterfaces.IsEnabled = false;
                    bContinueCapturing = true;

                    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw,
                        ProtocolType.IP);
                    //Bind the socket to the selected IP address
                    mainSocket.Bind(new IPEndPoint(IPAddress.Parse(combInterfaces.Text), 0));
                    mainSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                    byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
                    byte[] byOut = new byte[4] { 1, 0, 0, 0 };
                    mainSocket.IOControl(IOControlCode.ReceiveAll, byTrue, byOut);

                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
                else
                {
                    startSniffingButton.Content = "Start";
                    combInterfaces.IsEnabled = true;
                    bContinueCapturing = false;
                    //To stop capturing the packets close the socket
                    mainSocket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, nameWindow, MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = mainSocket.EndReceive(ar);
                ParseData(byteData, nReceived);

                if (bContinueCapturing)
                {
                    byteData = new byte[4096];

                    mainSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                        new AsyncCallback(OnReceive), null);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, nameWindow, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParseData(byte[] byteData, int nReceived)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                /*TreeViewItem rootNode = new TreeViewItem();

                //Since all protocol packets are encapsulated in the IP datagram
                //so we start by parsing the IP header and see what protocol data
                //is being carried by it
                IPHeader ipHeader = new IPHeader();
                ipHeader.Init(byteData, nReceived);

                TreeViewItem ipItem = MakeIPTreeNode(ipHeader);
                rootNode.Items.Add(ipItem);
                rootNode.Header = ipHeader.SourceAddress.ToString() + "-" +
                    ipHeader.DestinationAddress.ToString();

                treeView.Items.Add(rootNode);*/

                IPHeader ipHeader = new IPHeader();
                ipHeader.Init(byteData, nReceived);

                if (ipHeader.DestinationAddress.ToString().Equals(combInterfaces.Text)) return;

                bool isItemChanged = false;
                foreach (var el in listIPItems)
                {
                    if (el.IP.Equals(ipHeader.DestinationAddress.ToString()))
                    {
                        el.Count++;
                        listView.ItemsSource = null;
                        listView.ItemsSource = listIPItems;
                        isItemChanged = true;
                        break;
                    }
                }
                if (!isItemChanged)
                {
                    listIPItems.Add(new IPItem(ipHeader.DestinationAddress.ToString(), 1));
                }

            }));
        }

        private TreeViewItem MakeIPTreeNode(IPHeader ipHeader)
        {
            TreeViewItem ipItem = new TreeViewItem();

            ipItem.Header = "IP";
            ipItem.Items.Add("Ver: " + ipHeader.Version);
            ipItem.Items.Add("Header Length: " + ipHeader.HeaderLength);
            ipItem.Items.Add("Differntiated Services: " + ipHeader.DifferentiatedServices);
            ipItem.Items.Add("Total Length: " + ipHeader.TotalLength);
            ipItem.Items.Add("Identification: " + ipHeader.Identification);
            ipItem.Items.Add("Flags: " + ipHeader.Flags);
            ipItem.Items.Add("Fragmentation Offset: " + ipHeader.FragmentationOffset);
            ipItem.Items.Add("Time to live: " + ipHeader.TTL);
            switch (ipHeader.ProtocolType)
            {
                case Protocol.TCP:
                    ipItem.Items.Add("Protocol: " + "TCP");
                    break;
                case Protocol.UDP:
                    ipItem.Items.Add("Protocol: " + "UDP");
                    break;
                case Protocol.Unknown:
                    ipItem.Items.Add("Protocol: " + "Unknown");
                    break;
            }
            ipItem.Items.Add("Checksum: " + ipHeader.Checksum);
            ipItem.Items.Add("Source: " + ipHeader.SourceAddress.ToString());
            ipItem.Items.Add("Destination: " + ipHeader.DestinationAddress.ToString());

            return ipItem;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            if (hostEntry.AddressList.Length > 0)
            {
                foreach (var el in hostEntry.AddressList)
                {
                    combInterfaces.Items.Add(el.ToString());
                }
            }

            listView.ItemsSource = listIPItems;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            if (bContinueCapturing)
            {
                mainSocket.Close();
            }
        }

    }
}
