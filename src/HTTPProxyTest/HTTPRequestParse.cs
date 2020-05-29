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
        /// <summary>
        /// 删除字符串的前导空格
        /// </summary>
        /// <param name="str">
        /// 要处理的字符串
        /// </param>
        /// <returns>
        /// 处理后的字符串
        /// </returns>
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
        /// <summary>
        /// 解析HTTP报文的头部
        /// </summary>
        /// <param name="rawRequest">未经分析的原始报文头部</param>
        /// <returns>返回一个包含各种HTTP信息的解析结果</returns>
        public static ResolvedHttpProxyRequest ResolveHTTPProxyRequest(string rawRequest)
        {
            char[] spc= { '\r','\n'};
            //将HTTP报文头部按照以"\r\n"结尾分成多行
            string[] allHeaders = rawRequest.Split(spc);
            //单独处理第一行，第一行包含HTTP请求方法，URI和HTTP版本。按照空格将第一行分为三部分
            string[] firstLineParas = allHeaders[0].Split(' ');
            //分别赋值
            string Method = firstLineParas[0];string URI = firstLineParas[1];string HttpInfo = firstLineParas[2];
            //原始头部集合（List类似于C++中的Vector）
            List<HeaderPair> rawHeader=new List<HeaderPair>();
            //对于每一个头部进行处理
            for(int i=1;i<allHeaders.Length;i++)
            {
                if (String.IsNullOrWhiteSpace(allHeaders[i]))
                {
                    continue;
                }
                //获取该头部=
                string header = allHeaders[i];
                //将头部按照“：“分为多部分
                string[] head=header.Split(':');
                //获取该头部的名称（key）和值（value）
                string key = head[0];
                string value = "";
                for(int j = 1; j < head.Length; j++)
                {
                    value += head[j];
                }
                value = DeleteFrontWhiteSpace(head[1]);
                //添加到原始头部集合中
                rawHeader.Add(new HeaderPair(key, value));
                /*
                if (key.ToLower() == "host")
                {
                    if(head.Length>2)
                    Host = HTTPRequestParser.DeleteFrontWhiteSpace(head[1] + ":" + head[2]);
                    else
                        Host = HTTPRequestParser.DeleteFrontWhiteSpace(head[1]);
                }
                */
            }
            //初始化解析结果并将原头部添加进去等待下一步处理
            List<HeaderPair> resovledheaders = new List<HeaderPair>();
            foreach(HeaderPair raw in rawHeader)
            {
                //这里的特判是为了不让代理将本该发给代理的头部发给HTTP服务器
                if(raw.Key.ToLower() == "proxy-connection")
                {
                    continue;
                }
                resovledheaders.Add(raw);
            }
            //对一些头部进行特殊处理
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
            //获取解析后的报文头
            string resolvedreq = "";
            resolvedreq += Method+" ";
            resolvedreq += URI+" ";
            resolvedreq += HttpInfo+"\r\n";
            foreach(HeaderPair header in resovledheaders)
            {
                resolvedreq += header.ToString() + "\r\n";
            }
            resolvedreq += "\r\n";
            //生成结果并返回
            ResolvedHttpProxyRequest result = new ResolvedHttpProxyRequest( Method,resolvedreq , URI, HttpInfo,resovledheaders);
            return result;
        }
    }
    /// <summary>
    /// 这是解析结果类
    /// 下面这些既不像方法也不像变量的东西叫索引器，可以暂时理解为能控制访问权限的变量
    /// </summary>
    public class ResolvedHttpProxyRequest
    {
        public int Port
        {
            get
            {
                foreach (string s in URI.Split(':'))
                {
                    try
                    {
                        return Convert.ToInt32(s);
                    }
                    catch (Exception)
                    {

                    }
                }
                if (Method != HTTPMethod.Connect)
                {
                    return 80;
                }
                else
                {
                    return 443;
                }
            }
        }
        public HeaderPair Host
        {
            get
            {
                foreach (HeaderPair header in HeadersExceptFirstLine)
                {
                    if (header.Key.ToLower() == "host")
                    {
                        return header;
                    }
                }
                return null;
            }
            set
            {
                foreach (HeaderPair header in HeadersExceptFirstLine)
                {
                    if (header.Key.ToLower() == "host")
                    {
                        header.Value = value.Value;
                    }
                }
            }
        }
        public string Method
        {
            get;
            private set;
        }
        public HeaderPair Connection
        {
            get
            {
                foreach (HeaderPair header in HeadersExceptFirstLine)
                {
                    if (header.Key.ToLower() == "connection")
                    {
                        return header;
                    }
                }
                return null;
            }
            set
            {
                foreach (HeaderPair header in HeadersExceptFirstLine)
                {
                    if (header.Key.ToLower() == "connection")
                    {
                        header.Value = value.Value;
                    }
                }
            }
        }
        public HeaderPair Proxy_Connection
        {
            get
            {
                foreach(HeaderPair header in HeadersExceptFirstLine)
                {
                    if(header.Key.ToLower()== "proxy-connection")
                    {
                        return header;
                    }
                }
                return null;
            }
            set
            {
                foreach (HeaderPair header in HeadersExceptFirstLine)
                {
                    if (header.Key.ToLower() == "proxy-connection")
                    {
                        header.Value=value.Value;
                    }
                }
            }
        }
        public string RequestString
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
        public List<HeaderPair> HeadersExceptFirstLine
        {
            get;
            private set;
        }
        /// <summary>
        /// 这是构造函数
        /// </summary>
        /// <param name="host"></param>
        /// <param name="method"></param>
        /// <param name="connection"></param>
        /// <param name="proxy_connection"></param>
        /// <param name="uri"></param>
        /// <param name="httpver"></param>
        public ResolvedHttpProxyRequest(string method,string requestString,string uri,string httpver,List<HeaderPair> headers)
        {
            Method = method;
            URI = uri;
            HTTPInfo = httpver;
            HeadersExceptFirstLine = headers;
            RequestString = requestString;
        }
        
    }
    /// <summary>
    /// 这是表示一个头部的类
    /// </summary>
    public static class HTTPMethod
    {
        
        public static string Connect = "CONNECT";
        public static string Get = "GET";
        public static string Post = "POST";
        public static string Put = "PUT";
        public static string Delete = "DELETE";
        public static string Options="OPTIONS";
        public static string Trace= "TRACE";
        public static string Head = "HEAD";
        public static string Patch = "PATCH";
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
