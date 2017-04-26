using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Newtonsoft.Json;
using SSDTWrap.DacFx;

namespace SSDTWrap
{
    public class RegistrationHandler : Handler
    {
        private readonly RegistrationHandlerMessage message;

        public RegistrationHandler(string configBlob) : base(configBlob)
        {
            message = JsonConvert.DeserializeObject<RegistrationHandlerMessage>(configBlob);
        }

        public override string Execute()
        {
            Context.Settings["Directory"] = message.Directory;
            Context.Settings["JsonConfig"] = message.JsonConfig;
            Context.SsdtSettings = JsonConvert.DeserializeObject<SsdtConfig>(new StreamReader(message.JsonConfig).ReadToEnd());
            var wrapper = new ModelWrapper(Context.SsdtSettings.SqlServerVersion, message.Directory, Context.SsdtSettings.RefactorLog, Context.SsdtSettings.PreScript, Context.SsdtSettings.PostScript, Context.SsdtSettings.References);
            Context.Settings["Model"] = wrapper;
            
            Context.Messages = wrapper.CurrentModel().GetModelErrors().ToList();
            return JsonConvert.SerializeObject(Context);
        }
    }

 
    public class SsdtConfig
    {
        public string OutputFile { get; set; }
        public SqlServerVersion SqlServerVersion { get; set; }
        public string PreScript { get; set; }
        public string PostScript { get; set; }
        public string RefactorLog { get; set; }
        public string IgnoreFilter { get; set; }
        public List<DacReference> References { get; set; }
        public TSqlModelOptions ModelOptions { get; set; }  //this shouldn't be serialized....
        public string PublishProfilePath { get; set; }
    }

    public class DacReference
    {
        public ReferenceKind Kind { get; set; }
        public string Path { get; set; }
    }

    public enum ReferenceKind
    {
        Same,
        Database,
        Server,
        System
    }
}

/*
 {
    "outputFile": "c:\\dev\\b.dacpac",
    "sqlVersion": 140,
    "references":[
        {
            "type": "same",
            "path": "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\IDE\\Extensions\\Microsoft\\SQLDB\\Extensions\\SqlServer\\140\\SQLSchemas\\master.dacpac"
        }
    ],
    "preDeployScript": "",
    "postDeployScript": "",
    "refactorLog": "",
    "ignoreFilter": "*script*"
}
     */
