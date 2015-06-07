using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class HTTPSClient
    {
        SslStream _ssls;
        byte[] _buffer = new byte[1024];
        string _request = string.Empty;
        public HTTPSClient(TcpClient _client, string _cert)
        {
            X509Certificate cert = new X509Certificate(_cert);
            _ssls = new SslStream(_client.GetStream(), false);
            try
            {
                _ssls.AuthenticateAsServer(cert);
            }
            catch 
            {

            }

            _request = ReadMessage(_ssls);

            if (_request.Length == 0)
            {
                _ssls.Close();
                _client.Close();
            }
            else
            {
                Regex _reg = new Regex(@"^\w+\s+/(\S+)+\s+HTTP/\d+.\d+");
                string _value = "/" + _reg.Match(_request).Groups[1].Value;
                // Приводим ее к изначальному виду, преобразуя экранированные символы
                // Например, "%20" -> " "
                _value = Uri.UnescapeDataString(_value);
                string[] _params = null;
                //Если есть - выделяем параметры
                if (_value.LastIndexOf('?') != -1)
                {
                    _params = _value.Remove(0, _value.LastIndexOf('?') + 1).Split(new char[] { '&', '=' }, StringSplitOptions.RemoveEmptyEntries);
                }
                if (_params != null)
                {
                    //Обработка параметров
                    if (_params.Length == 2)
                    {
                        string Html = "<html><body><h1 align = \"center\">" + _params[0] + " " + _params[1] + "</h1>";
                        if (_params[0] == "nikita" && _params[1] == "lalka")
                        {
                            Html += "</body></html>";
                        }
                        else
                        {
                            Html += "<h1 align = \"center\"> NIKITA LALKA </h1></body></html>";
                        }

                        string ContentLength = Html.Length.ToString();
                        string header = "HTTP/1.1 200 OK\nContent-Type: \nContent-Length: " + ContentLength + "\n\n" + Html;
                        _buffer = Encoding.UTF8.GetBytes(header);
                        _ssls.Write(_buffer, 0, _buffer.Length);
                        _buffer = new byte[Html.Length];
                        _ssls.Write(_buffer, 0, _buffer.Length);
                        _ssls.Close();
                        _client.Close();
                        return;
                    }
                }
                string temp = string.Empty;
                // Если строка запроса оканчивается на "/"
                if (_value.EndsWith("/"))
                {
                    _value += "index.html";
                }
                string FilePath = "Site";
                // Получаем расширение файла из строки запроса
                string Extension = _value.Substring(_value.LastIndexOf('.'));

                // Тип содержимого
                string ContentType = "";

                // Пытаемся определить тип содержимого по расширению файла
                switch (Extension)
                {
                    case ".htm":
                    case ".html":
                        ContentType = "text/html";
                        break;
                    case ".css":
                        ContentType = "text/css";
                        break;
                    case ".js":
                        ContentType = "text/javascript";
                        break;
                    case ".jpg":
                        ContentType = "image/jpeg";
                        break;
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                        ContentType = "image/" + Extension.Substring(1);
                        break;
                    default:
                        if (Extension.Length > 1)
                        {
                            ContentType = "application/" + Extension.Substring(1);
                        }
                        else
                        {
                            ContentType = "application/unknown";
                        }
                        break;
                }
                // Открываем файл, страхуясь на случай ошибки
                FilePath += _value;
                Console.WriteLine(FilePath);
                //Парсим внутренние теги
                string lineHtml;
                
                if (!File.Exists(FilePath))
                {
                    SendError(_ssls, 404);
                    _client.Close();
                    return;
                }

                if (ContentType == "text/html")
                {
                    StreamReader sr = new StreamReader(FilePath);
                    lineHtml = sr.ReadToEnd();
                    FilePath = "Site/temps.html";
                    sr.Close();
                    lineHtml = lineHtml.Replace("<lalka>", "<h1 align = \"center\">LALKA TUTA</h1>");
                    StreamWriter sw = new StreamWriter(FilePath);
                    sw.Write(lineHtml);
                    sw.Close();
                }

                FileStream FS;
                try
                {
                    FS = new FileStream(FilePath, FileMode.Open);
                    string ContentLength = FS.Length.ToString();
                    string header = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + ContentLength + "\n\n";
                    _buffer = Encoding.UTF8.GetBytes(header);
                    _ssls.Write(_buffer, 0, _buffer.Length);
                    _buffer = new byte[FS.Length];
                    FS.Read(_buffer, 0, Convert.ToInt32(FS.Length));
                    _ssls.Write(_buffer, 0, _buffer.Length);
                    FS.Close();
                }
                catch
                {
                    // Если случилась ошибка, посылаем клиенту ошибку 500
                    //SendError(_client, 500);
                    //Console.WriteLine(ex.Message);
                    
                }
                if (File.Exists("Site/temps.html"))
                {
                    File.Delete("Site/temps.html");
                }
                _ssls.Close();
                _client.Close();
            }
        }
        static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            try
            {
                while (true)
                {
                    int bytes = -1;
                    // Read the client's test message.
                    bytes = sslStream.Read(buffer, 0, buffer.Length);

                    // Use Decoder class to convert from bytes to UTF8
                    // in case a character spans two buffers.
                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);
                    // Check for EOF or an empty message.
                    if (messageData.ToString().IndexOf("\r\n\r\n") != -1)
                    {
                        break;
                    }
                    if (bytes == 0)
                    {
                        break;
                    }
                }
            }
            catch
            {

            }

            return messageData.ToString();
        }
        private void SendError(SslStream sslStream, int Code)
        {
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1 align = \"center\">" + CodeStr + "</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            sslStream.Write(Buffer, 0, Buffer.Length);
        }
    }
}
