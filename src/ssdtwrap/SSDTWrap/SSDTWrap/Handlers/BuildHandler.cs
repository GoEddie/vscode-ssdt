using System;
using System.Collections.Generic;
using Dir2Dac;
using Microsoft.SqlServer.Dac;
using Newtonsoft.Json;
using SSDTWrap.DacFx;

namespace SSDTWrap.Handlers
{
    public class BuildHandler : Handler
    {
        private ContextSlimMessage message;

        public BuildHandler(string configBlog) : base(configBlog)
        {
            message = JsonConvert.DeserializeObject<ContextSlimMessage>(configBlog);
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
            var creator = new DacCreator(Context.SsdtSettings.OutputFile, GetReferences(), Context.SsdtSettings.SqlServerVersion, Context.SsdtSettings.ModelOptions, Context.SsdtSettings.PreScript, Context.SsdtSettings.PostScript);
            creator.Write(model.CurrentModel());
            
            return JsonConvert.SerializeObject(Context);
        }
        
        private List<Dir2Dac.Reference> GetReferences()
        {
            var ret = new List<Dir2Dac.Reference>();

            foreach (var r in Context.SsdtSettings.References)
            {
                switch (r.Kind)
                {
                    case ReferenceKind.Same:
                        ret.Add(new ThisReference(r.Path, "aname"));
                        break;
                    case ReferenceKind.Database:
                        //ret.Add(new OtherReference(r.Path, "aname","dbunknownfornow"));
                        break;
                    case ReferenceKind.Server:
                        break;
                    case ReferenceKind.System:
                        ret.Add(new SystemReference(r.Path, r.Path.Contains("master") ? "master" : "msdb", r.Path.Contains("master") ? "master" : "msdb"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                
            }

            return ret;
        }
        
    }
}