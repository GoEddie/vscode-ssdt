using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.SqlServer.Dac.Extensions;
using SSDTWrap.Handlers;

namespace SSDTWrap
{
    public class Listener
    {
        private readonly HttpListener _listener;
        private int _port;

        public Listener(int port)
        {
            _port = port;
            _listener = new HttpListener();
        }

        public void Listen()
        {
            _listener.Prefixes.Add("http://127.0.0.1:14801/register/");
            _listener.Prefixes.Add("http://127.0.0.1:14801/build/");
            _listener.Prefixes.Add("http://127.0.0.1:14801/references/");
            _listener.Prefixes.Add("http://127.0.0.1:14801/update/");

            _listener.Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    try
                    {
                        var context = _listener.GetContext();
                        var request = context.Request;
                        var response = context.Response;

                        var url = request.Url;
                        Console.WriteLine("URL: " + url.AbsolutePath);

                        var reader = new StreamReader(request.InputStream);
                        Handler handler = null;

                        switch (url.AbsolutePath)
                        {
                            case "/register/":
                                handler = new RegistrationHandler(reader.ReadToEnd());
                                break;
                            case "/references/":
                                handler = new ReferencesHandler(reader.ReadToEnd());
                                break;
                            case "/update/":
                                handler = new UpdateHandler(reader.ReadToEnd());
                                break;
                            case "/build/":
                                handler = new BuildHandler(reader.ReadToEnd());
                                break;
                        }

                        if (handler != null)
                        {
                            var writer = new StreamWriter(response.OutputStream);
                            writer.Write(handler.Execute());
                            writer.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"exception: {e.Message}");
                    }
                }
            }).Start();
        }
    }
}