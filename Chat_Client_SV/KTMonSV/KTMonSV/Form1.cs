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

namespace KTMonSV
{
    public partial class Form1 : Form
    {
        private Socket server, client;
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
        private void startServer()
        {
            EndPoint ep = new IPEndPoint(IPAddress.Any, 2023);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ep);
            server.Listen(10);
            server.BeginAccept(new AsyncCallback(beginAccept), server);
            updateUi("Đang lắng nghe các kết nối....");
        }
        private void beginAccept(IAsyncResult ar)
        {
            Socket s = (Socket)ar.AsyncState;
            client = s.EndAccept(ar);
            updateUi("Đã nhận kết nối từ client " + client.RemoteEndPoint.ToString());
            string wc = "Xin chao client!...";
            buff2 = Encoding.ASCII.GetBytes(wc);
            client.BeginSend(buff2, 0, buff2.Length, SocketFlags.None, new AsyncCallback(sendata), client);
            client.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(beginReceive), client);
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
                        if (recv == 4)
                        {
                            int fileSize = BitConverter.ToInt32(buff, 0);
                            buff = new byte[fileSize];

                            s.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(receiveImageData), s);
                        }
                        else
                        {
                            string receivedData = Encoding.ASCII.GetString(buff, 0, recv);
                            Invoke(updateUi, "Client: " + receivedData);

                            s.BeginReceive(buff, 0, buff.Length, SocketFlags.None, new AsyncCallback(beginReceive), s);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    Invoke(updateUi, "Ngắt kết nối với client");
                }
            }
            else
            {
                Invoke(updateUi, "Ngắt kết nối với client");
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
                        // Lưu trữ dữ liệu của file ảnh nhận được vào một file trên server
                        string filePath = @"E:\LTwindows\Chat_Client_SV\Chat_Client_SV\images\server\nhan\nhan1.PNG"; // Đường dẫn và tên file để lưu trữ ảnh

                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            fileStream.Write(buff, 0, bytesRead);
                        }

                        Invoke(updateUi, "Đã nhận và lưu trữ file ảnh từ client vào địa chỉ'E:\\LTwindows\\Chat_Client_SV\\Chat_Client_SV\\images\\server\\nhan\\'.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    Invoke(updateUi, "Ngắt kết nối với client");
                }
            }
            else
            {
                Invoke(updateUi, "Ngắt kết nối với client");
            }
        }
        private void SendImageToClient(string imagePath)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    byte[] imageData = File.ReadAllBytes(imagePath);

                    byte[] fileSize = BitConverter.GetBytes(imageData.Length);
                    client.Send(fileSize);

                    client.Send(imageData);

                    Invoke(updateUi, "Đã gửi file ảnh đến client.");
                }
                else
                {
                    Invoke(updateUi, "Không có kết nối với client.");
                }
            }
            catch (Exception ex)
            {
                Invoke(updateUi, "Lỗi khi gửi file ảnh: " + ex.Message);
            }
        }

        private void sendata(IAsyncResult ia)
        {
            client.EndSend(ia);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            startServer();
            txtNhap.Focus();
        }
        private void send()
        {
            string hl = txtNhap.Text;
            byte[] gui = new byte[1024];
            gui = Encoding.ASCII.GetBytes(hl);
            txtNhap.Clear();
            updateUi("Server: " + hl);
            client.BeginSend(gui, 0, gui.Length, SocketFlags.None, new AsyncCallback(sendata), client);
        }
        private void btnGui_Click(object sender, EventArgs e)
        {
            send();
            txtNhap.Focus();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            updateUi("Ngắt kết nối với client");
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            server.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string imagePathToSend = openFileDialog.FileName;

                SendImageToClient(imagePathToSend);
            }
        }

        private void lbHienThi_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtNhap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                send();

            }
        }

    }
}
