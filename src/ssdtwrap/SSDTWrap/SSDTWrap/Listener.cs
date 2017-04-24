using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Extensions;
using Newtonsoft.Json;

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

    public class BuildHandler : Handler
    {
        private ContextSlim message;

        public BuildHandler(string configBlog) : base(configBlog)
        {
            message = JsonConvert.DeserializeObject<ContextSlim>(configBlog);
        }

        public override string Execute()
        {
            var model = Context.Settings["Model"] as ModelWrapper;
            if (model == null)
            {
                return JsonConvert.SerializeObject(new ReferencesResponse()
                {
                    References = new List<Reference>()
                });
            }

            DacPackageExtensions.BuildPackage("C:\\dev\\a.dacpac", model.CurrentModel(), new PackageMetadata());
            return JsonConvert.SerializeObject(Context);
        }
    }

    public class UpdateHandler : Handler
    {
        private UpdateHandlerMessage message;

        public UpdateHandler(string configBlob) : base(configBlob)
        {
            message = JsonConvert.DeserializeObject<UpdateHandlerMessage>(configBlob);
        }

        public override string Execute()
        {
            var model = Context.Settings["Model"] as ModelWrapper;
            if (model == null)
            {
                return JsonConvert.SerializeObject(new ReferencesResponse()
                {
                    References = new List<Reference>()
                });
            }

            
            var file = new FileInfo(message.File);
            model.AddFile(file);

            model.Validate();
            Context.Messages = model.CurrentModel().GetModelErrors().ToList();
            return JsonConvert.SerializeObject(Context);
        }
    }

    public class UpdateHandlerMessage : ContextSlim
    {
        public string File { get; set; }
    }

    public class ReferencesResponse : ContextSlim
    {
        public List<Reference> References { get; set; }
    }

    public class Reference
    {
        public string FileNane { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    public class ReferencesHandlerMessage : ContextSlim
    {
        public string SchemaName { get; set; }
        public string Name { get; set; }
    }
}