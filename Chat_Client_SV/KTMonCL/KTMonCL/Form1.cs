using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace KTMonCL
{
    public partial class Form1 : Form
    {
        
        private Socket client;
        byte[] buff = new byte[1024];
        byte[] buff2 = new byte[1024];

        private delegate void updateUI(string massage);
        private updateUI updateUi;
        public Form1()
        {
            InitializeComponent();
            updateUi = new updateUI(update);
            CheckForIllegalCrossThreadCalls = false;
        }
        private void update(string m)
        {
            lbHienThi.Items.Add(m);
        }
        private List<Socket> connectedClients = new List<Socket>();
        private void startClient()
        {
            EndPoint ep = new IPEndPoint(IPAddress.Parse(txtIP.Text), Int32.Parse(txtPort.Text));
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            updateUi("Đang kết nối tới server...");
            client.BeginConnect(ep, new AsyncCallback(beginConnect), client);
            connectedClients.Add(client);
        }
        private void beginConnect(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            s.EndConnect(ar);
            updateUi("Đã nhận kết nối từ server " + s.RemoteEndPoint.ToString());
            string wc = "Xin chao server!...";
            buff2 = Encoding.ASCII.GetBytes(wc);
            client.BeginSend(buff2, 0, buff2.Length, SocketFlags.None, new AsyncCallback(sendata), client);

            client.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(beginReceive), client); 
            //OnReceiveData(client);
        }
        private void sendata(IAsyncResult ia)
        {
           client.EndSend(ia);
        }
        private void beginReceive(IAsyncResult ia)
        {
            Socket s = (Socket)ia.AsyncState;
            if (s != null && s.Connected)
            {
                try
                {
                    int recv = s.EndReceive(ia);
                    if (recv > 0)
                    {
                        // Kiểm tra nếu dữ liệu nhận được là kích thước của file
                        if (recv == 4) // Ví dụ: kích thước của file ảnh được gửi là 4 bytes (một integer)
                        {
                            int fileSize = BitConverter.ToInt32(buff, 0);
                            buff = new byte[fileSize];

                            // Tiếp tục nhận dữ liệu của file ảnh
                            s.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(receiveImageData), s);
                        }
                        else
                        {
                            // Xử lý thông tin khác nhận được từ server (nếu cần)
                            string receivedData = Encoding.ASCII.GetString(buff, 0, recv);
                            Invoke(updateUi, "Server: " + receivedData);

                            s.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(beginReceive), s);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    Invoke(updateUi, "Ngắt kết nối với server");
                }
            }
            else
            {
                Invoke(updateUi, "Ngắt kết nối với server");
            }
        }

        private void receiveImageData(IAsyncResult ia)
        {
            Socket s = (Socket)ia.AsyncState;
            if (s != null && s.Connected)
            {
                try
                {
                    int bytesRead = s.EndReceive(ia);
                    if (bytesRead > 0)
                    {
                        // Lưu trữ dữ liệu của file ảnh nhận được vào một file trên client
                        string filePath = @"E:\LTwindows\Chat_Client_SV\Chat_Client_SV\images\client\nhan\test.PNG"; // Đường dẫn và tên file để lưu trữ ảnh

                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            fileStream.Write(buff, 0, bytesRead);
                        }

                        Invoke(updateUi, "Đã nhận và lưu trữ file ảnh từ server vào địa chỉ 'E:\\LTwindows\\Chat_Client_SV\\Chat_Client_SV\\images\\client\\nhan\\test.PNG'.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    Invoke(updateUi, "Ngắt kết nối với server");
                }
            }
            else
            {
                Invoke(updateUi, "Ngắt kết nối với server");
            }
        }



        private void CloseConnection(Socket s)
        {
            if (s != null && s.Connected)
            {
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                    s.Close();
                    connectedClients.Remove(s);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Lỗi khi thực hiện Shutdown hoặc Close: " + ex.Message);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            startClient();
            txtSend.Focus();
        }
        private void send()
        {
            string wc = txtSend.Text;
            byte[] gui= new byte[1024];
            gui = Encoding.ASCII.GetBytes(wc);
            txtSend.Clear();
            updateUi("Client: " + wc);
            client.BeginSend(gui, 0, gui.Length, SocketFlags.None, new AsyncCallback(sendata), client);
        }

        private void btnGui_Click(object sender, EventArgs e)
        {
            send();
            txtSend.Focus();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            updateUi("Ngắt kết nối với server");
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            this.Close();
        }

        private void txtPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                startClient();

            }
        }

        private void txtSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                send();

            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                SendImage(filePath);
            }
        }

        private void SendImage(string filePath)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    byte[] fileData = File.ReadAllBytes(filePath);

                    // Gửi kích thước của file trước
                    byte[] fileSize = BitConverter.GetBytes(fileData.Length);
                    client.Send(fileSize);

                    // Gửi dữ liệu của file ảnh
                    client.Send(fileData);

                    updateUi("Đã gửi ảnh đến server.");
                }
                else
                {
                    updateUi("Không có kết nối với server.");
                }
            }
            catch (Exception ex)
            {
                updateUi("Lỗi khi gửi ảnh: " + ex.Message);
            }
        }
    }
}
