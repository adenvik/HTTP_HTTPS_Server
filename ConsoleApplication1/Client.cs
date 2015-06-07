using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Client
    {
        // Объявим строку, в которой будет хранится запрос клиента
        string _request = "";
        // Буфер для хранения принятых от клиента данных
        byte[] _buffer = new byte[1024];
        // Переменная для хранения количества байт, принятых от клиента
        int Count;

        public Client(TcpClient _client)
        {
            //Получаем запрос клиента
            _client.GetStream().Read(_buffer, 0, 1024);
            _request = Encoding.ASCII.GetString(_buffer);

            // Парсим строку запроса с использованием регулярных выражений
            // При этом отсекаем все переменные GET-запроса
            Match ReqMatch = Regex.Match(_request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");
            
            //1 - ^\w+\s+          -- запроос вида буквы+пробел в начале строки
            //2 - /(\S)+           -- группа символов, файл, который нужно подгрузить + возможные параметры
            //3 - \s+\HTTP/\d+.\d+ -- конец строки
            Regex _reg = new Regex(@"^\w+\s+/(\S+)+\s+HTTP/\d+.\d+");

            string _value = _reg.Match(_request).Groups[1].Value;
            string[] _params = null;
            //Если есть - выделяем параметры
            if (_value.LastIndexOf('?') != -1)
                _params = _value.Remove(0, _value.LastIndexOf('?') + 1).Split(new char[] {'&','='},StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("\n" + _value + "\n");
            if (_params != null)
            {
                for (int i = 0; i < _params.Length; i++)
                {
                    Console.Write(_params[i] + " ");
                }
            }
            
            // Если запрос не удался
            if (ReqMatch == Match.Empty)
            {
                // Передаем клиенту ошибку 400 - неверный запрос
                SendError(_client, 400);
                return;
            }

            // Получаем строку запроса
            string RequestUri = ReqMatch.Groups[1].Value;

            // Приводим ее к изначальному виду, преобразуя экранированные символы
            // Например, "%20" -> " "
            RequestUri = Uri.UnescapeDataString(RequestUri);
            //Console.WriteLine(RequestUri);
            // Если в строке содержится двоеточие, передадим ошибку 400
            // Это нужно для защиты от URL типа http://example.com/../../file.txt
            if (RequestUri.IndexOf("..") >= 0)
            {
                SendError(_client, 400);
                return;
            }

            // Если строка запроса оканчивается на "/", то добавим к ней index.html
            if (RequestUri.EndsWith("/"))
            {
                //RequestUri += "index.html";
                if (RequestUri.Length > 1)
                {
                    RequestUri = RequestUri.Remove(RequestUri.LastIndexOf('/') - 1, RequestUri.Length - RequestUri.LastIndexOf('/') - 1);
                    RequestUri = RequestUri.Remove(RequestUri.Length - 1);
                }
                else
                {
                    RequestUri += "index.html";
                }
                Console.WriteLine(RequestUri);
            }

            string FilePath = "Site/" + RequestUri;

            // Если в папке www не существует данного файла, посылаем ошибку 404
            if (!File.Exists(FilePath))
            {
                SendError(_client, 404);
                return;
            }

            // Получаем расширение файла из строки запроса
            string Extension = RequestUri.Substring(RequestUri.LastIndexOf('.'));
            Console.WriteLine("Ext");

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
            FileStream FS;
            try
            {
                FS = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                // Если случилась ошибка, посылаем клиенту ошибку 500
                SendError(_client, 500);
                return;
            }

            // Посылаем заголовки
            string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + FS.Length + "\n\n";
            byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
            _client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);

            // Пока не достигнут конец файла
            while (FS.Position < FS.Length)
            {
                // Читаем данные из файла
                Count = FS.Read(_buffer, 0, _buffer.Length);
                // И передаем их клиенту
                _client.GetStream().Write(_buffer, 0, Count);
            }

            // Закроем файл и соединение
            FS.Close();
            _client.Close();
        }
        private void SendError(TcpClient Client, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички

            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            // Закроем соединение
            Client.Close();
        }
    }
}
