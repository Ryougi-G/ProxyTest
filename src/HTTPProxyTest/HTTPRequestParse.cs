using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HTTPProxyTest
{
    static class HTTPRequestParser
    {
        public static string DeleteFrontWhiteSpace(string str)
        {
            int pos = 0;
            while (str[pos] == ' ' && pos < str.Length)
            {
                pos++;
            }
            if (pos < str.Length)
            {
                string res = "";
                for (int i = pos; i < str.Length; i++)
                {
                    res += str[i];
                }
                return res;
            }
            else
            {
                return str;
            }
        }
        public static ResolvedHttpProxyRequest ResolveHTTPProxyRequest(string rawRequest)
        {
            char[] spc= { '\r','\n'};
            string[] allHeaders = rawRequest.Split(spc);
            string[] firstLineParas = allHeaders[0].Split(' ');
            string Method = firstLineParas[0];string URI = firstLineParas[1];string HttpInfo = firstLineParas[2];
            List<HeaderPair> rawHeader=new List<HeaderPair>();
            List<string> Connection=new List<string>();
            List<string> ProxyConnection = new List<string>();
            string Host="";
            for(int i=1;i<allHeaders.Length;i++)
            {
                if (String.IsNullOrWhiteSpace(allHeaders[i]))
                {
                    continue;
                }
                string header = allHeaders[i];
                string[] head=header.Split(':');
                string key = head[0];
                string value = DeleteFrontWhiteSpace(head[1]);
                rawHeader.Add(new HeaderPair(key, value));
                if (key.ToLower() == "connection")
                {
                    foreach(string s in value.Split(','))
                    {
                        Connection.Add(s);
                    }
                }
                if (key.ToLower() == "proxy-connection")
                {
                    foreach (string s in value.Split(','))
                    {
                        ProxyConnection.Add(s);
                    }
                }
                if (key.ToLower() == "host")
                {
                    if(head.Length>2)
                    Host = HTTPRequestParser.DeleteFrontWhiteSpace(head[1] + ":" + head[2]);
                    else
                        Host = HTTPRequestParser.DeleteFrontWhiteSpace(head[1]);
                }
            }
            List<HeaderPair> resovledheaders = new List<HeaderPair>();
            foreach(HeaderPair raw in rawHeader)
            {
                if(raw.Key.ToLower() == "proxy-connection")
                {
                    continue;
                }
                resovledheaders.Add(raw);
            }
            foreach(HeaderPair header in resovledheaders)
            {
                if (header.Key.ToLower() == "connection")
                {
                    header.Value = "close";
                }
                if (header.Key.ToLower() == "proxy-connection")
                {
                    header.Value = "close";
                }
            }
            string resolvedreq = "";
            resolvedreq += Method+" ";
            resolvedreq += URI+" ";
            resolvedreq += HttpInfo+"\r\n";
            foreach(HeaderPair header in resovledheaders)
            {
                resolvedreq += header.ToString() + "\r\n";
            }
            resolvedreq += "\r\n";
            ResolvedHttpProxyRequest result = new ResolvedHttpProxyRequest(Host, Method, Connection, ProxyConnection, rawRequest, resolvedreq, URI, HttpInfo, rawHeader, resovledheaders);
            return result;
        }
    }
    public class ResolvedHttpProxyRequest
    {
        public string Host
        {
            get;
            private set;
        }
        public string Method
        {
            get;
            private set;
        }
        public List<string> Connection
        {
            get;
            private set;
        }
        public List<string> Proxy_Connection
        {
            get;
            private set;
        }
        public string RawRequest
        {
            get;
            private set;
        }
        public string ResolvedRequest
        {
            get;
            private set;
        }
        public string URI
        {
            get;
            private set;
        }
        public string HTTPInfo
        {
            get;
            private set;
        }
        public List<HeaderPair> RawHeadersExceptFirstLine
        {
            get;
            private set;
        }
        public List<HeaderPair> ResolvedHeadersExceptFirstLine
        {
            get;
            set;
        }
        public ResolvedHttpProxyRequest(string host,string method,List<string> connection,List<string> proxy_connection,string rawRequest,string resolvedrequest,string uri,string httpver,List<HeaderPair> rawheaders,List<HeaderPair> resheaders)
        {
            Host = host;
            Method = method;
            Connection = connection;
            Proxy_Connection = proxy_connection;
            RawRequest = rawRequest;
            ResolvedRequest = resolvedrequest;
            URI = uri;
            HTTPInfo = httpver;
            RawHeadersExceptFirstLine = rawheaders;
            ResolvedHeadersExceptFirstLine = resheaders;
        }
        
    }
    public class HeaderPair
    {
        public string Key
        {
            get;
            private set;
        }
        public  string Value
        {
            get;
            set;
        }
        public HeaderPair(string key,string value)
        {
            Key = key;
            Value = value;
        }
        public override string ToString()
        {
            return Key + ":" + Value;
        }
    }
}
