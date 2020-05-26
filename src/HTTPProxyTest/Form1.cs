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
            th.IsBackground = true;
            th.Start(a);
            //ReadRequest(a);
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
                nt.IsBackground = true;
                nt.Start(client);
                //DoProxy(client);
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
                //textBox1.Invoke(del, content);
                try
                {
                    ResolvedHttpProxyRequest request = HTTPRequestParser.ResolveHTTPProxyRequest(content);
                    
                    if (request.Method.ToLower() == "connect")
                    {
                        TcpClient cst = new TcpClient();
                        string[] spchost = request.Host.Split(':');
                        int port;
                        if (spchost.Length < 2)
                        {
                            port = Convert.ToInt32(request.URI.Split(':')[1]);
                        }
                        else if(spchost.Length<3)
                        {
                            port = 443;
                        }
                        else
                        {
                            port = Convert.ToInt32(spchost[2]);
                        }
                        //MessageBox.Show(Convert.ToString(port));
                        string s = HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0]);
                        cst.Connect(Dns.GetHostEntry(HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0])).AddressList[0],port);
                       // MessageBox.Show(Convert.ToString(cst.Connected));
                        textBox1.Invoke(del, request.ResolvedRequest);
                        NetworkStream serverstream = cst.GetStream();
                        string res = "HTTP/1.1 200 Connection Established\r\n\r\n";
                        if (cst.Connected)
                        {

                        }
                        else
                        {
                            res = "HTTP/1.1 404 Error\r\n\r\n";
                        }
                        byte[] buf = Encoding.ASCII.GetBytes(res);
                        ns.Write(buf, 0, buf.Length);
                        ns.Flush();
                        //MessageBox.Show(res);
                       // object box1 = (object)(new TcpClient[] { client, cst });
                        object box2 = (object)(new TcpClient[] { cst, client });
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(TunnelCon), box1);
                        Thread Wt = new Thread(new ParameterizedThreadStart(TunnelCon));
                        Wt.IsBackground = true;
                        Wt.Start(box2);
                        try
                        {
                            NetworkStream ns1 = client.GetStream();
                            NetworkStream ns2 = cst.GetStream();
                            int hasRead;
                            do
                            {
                                Thread.Sleep(1);
                                byte[] buf2 = new byte[4096];
                                hasRead = ns1.Read(buf2, 0, 4096);
                                ns2.Write(buf2, 0, hasRead);
                            } while (hasRead >= 0);
                        }
                        catch (Exception ex)
                        {
                            client.Close();
                            cst.Close();

                        }
                    }
                    else
                    {
                        TcpClient cst = new TcpClient();
                        string[] spchost = request.Host.Split(':');
                        string s = HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0]);
                        cst.Connect(Dns.GetHostEntry(HTTPRequestParser.DeleteFrontWhiteSpace(spchost[0])).AddressList[0], Convert.ToInt32(spchost[1]));
                        textBox1.Invoke(del, request.ResolvedRequest);
                        NetworkStream serverstream = cst.GetStream();
                        byte[] buffer = Encoding.ASCII.GetBytes(request.ResolvedRequest);
                        serverstream.Write(buffer, 0, buffer.Length);
                        serverstream.Flush();
                        BridgeCon(serverstream, ns);
                    }
                    // MessageBox.Show(spchost[0]);
                    //MessageBox.Show(spchost[1]);
                }
                catch(Exception ex)
                {
                  //  MessageBox.Show(ex.Message);
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
        private void TunnelCon(object x)
        {
            TcpClient client = ((TcpClient[])x)[0];
            TcpClient server = ((TcpClient[])x)[1];
            //MessageBox.Show(Convert.ToString(client.Connected)+Convert.ToString(client.Connected));
            
            try
            {
                NetworkStream ns1 = client.GetStream();
                NetworkStream ns2 = server.GetStream();
                int hasRead;
                do
                {
                    Thread.Sleep(1);
                    byte[] buf = new byte[4096];
                    hasRead = ns1.Read(buf, 0, 4096);
                    ns2.Write(buf, 0, hasRead);
                } while (hasRead >= 0);
            }catch(Exception ex)
            {
                client.Close();
                server.Close();
                
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
