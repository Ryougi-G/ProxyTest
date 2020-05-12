using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace HTTPProxyTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            int port = Convert.ToInt32(textBox2.Text);
            string ip = null;
            int rport = Convert.ToInt32(null);
            arg a = new arg(ip, port, rport);
            Thread th = new Thread(new ParameterizedThreadStart(ReadRequest));
            th.Start(a);
        }
        private void ReadRequest(object ax)
        {
            arg a = (arg)ax;
            TcpListener server = new TcpListener(IPAddress.Any,a.port);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread nt = new Thread(DoProxy);
                nt.Start(client);
            }
        }
        private void DoProxy(object ox)
        {
            TcpClient client = (TcpClient)ox;
            string content = "";
            var ns = client.GetStream();
            using (StreamReader reader = new StreamReader(ns))
            {
                while (ns.CanRead)
                {
                    string t = reader.ReadLine();
                    if (String.IsNullOrEmpty(t))
                    {
                        break;
                    }
                    else
                    {
                        content += t;
                        content += "\r\n";
                    }
                }
                SetStrDelegate del = SetStr;
                textBox1.Invoke(del, content);
                try
                {
                    ResolvedHttpProxyRequest request = HTTPRequestParser.ResolveHTTPProxyRequest(content);
                    TcpClient cst = new TcpClient();
                    string[] spchost = request.Host.Split(':');
                    // MessageBox.Show(spchost[0]);
                    //MessageBox.Show(spchost[1]);
                    string s = HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0]);
                    cst.Connect(Dns.GetHostEntry(HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0])).AddressList[0], Convert.ToInt32(spchost[1]));
                    textBox1.Invoke(del, request.ResolvedRequest);
                    NetworkStream serverstream = cst.GetStream();
                    serverstream.Write(Encoding.ASCII.GetBytes(request.ResolvedRequest), 0, Encoding.ASCII.GetByteCount(request.ResolvedRequest));
                    BridgeCon(serverstream, ns);
                    File.WriteAllText("test.txt", content);
                }
                catch(Exception ex)
                {

                }
            }
        }
        private delegate void SetStrDelegate(string mess);
        private void SetStr(string mess)
        {
            textBox1.Text = mess;
        }
        private void BridgeCon(NetworkStream sns,NetworkStream cns)
        {
            using(BinaryReader reader=new BinaryReader(sns))
            {
                using(BinaryWriter writer=new BinaryWriter(cns))
                {
                    while (sns.CanRead && cns.CanWrite)
                    {
                        try
                        {
                            byte[] buffer = new byte[1024];
                            int hasRead = reader.Read(buffer, 0, 1024);
                            writer.Write(buffer, 0, hasRead);
                        }catch(Exception ex)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
    class arg
    {
        public string ip;
        public int port;
        public int rport;
        public arg(string i,int p,int r)
        {
            ip = i;
            port = p;
            rport=r;
        }
    }
}
