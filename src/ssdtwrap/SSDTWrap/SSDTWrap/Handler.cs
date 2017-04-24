using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Model;
using Newtonsoft.Json;

namespace SSDTWrap
{
    public class Handler
    {
        protected readonly string ConfigBlob;
        protected readonly Context Context;

        public Handler(string configBlob)
        {
            ConfigBlob = configBlob;

            Context = GetContext();

        }

        private Context GetContext()
        {
            var contextSlim = JsonConvert.DeserializeObject<ContextSlim>(ConfigBlob);

            if (!contextSlim.Token.HasValue)
            {
                contextSlim.Token = Guid.NewGuid();
            }

            if (ContextBag.Contexts.ContainsKey(contextSlim.Token.Value))
            {
                return ContextBag.Contexts[contextSlim.Token.Value];
            }

            var context = new Context
            {
                Token = contextSlim.Token.Value,
                Settings = new Dictionary<string, object>()
            };

            ContextBag.Contexts.Add(context.Token, context);
            return context;

        }

        public virtual string Execute()
        {
            return Context.Token.ToString();
        }
    }

    public class RegistrationHandler : Handler
    {
        private RegistrationHandlerMessage message;

        public RegistrationHandler(string configBlob) : base(configBlob)
        {
            message = JsonConvert.DeserializeObject<RegistrationHandlerMessage>(configBlob);
        }

        public override string Execute()
        {
            Context.Settings["Directory"] = message.Directory;
            Context.Settings["JsonConfig"] = message.JsonConfig;
            var wrapper = new ModelWrapper(SqlServerVersion.Sql130, message.Directory, null, null, null);
            Context.Settings["Model"] = wrapper;

            Context.Messages = wrapper.CurrentModel().GetModelErrors().ToList();
            return JsonConvert.SerializeObject(Context);
        }
    }

    public class RegistrationHandlerMessage : ContextSlim
    {
        public string Directory { get; set; }

        public string JsonConfig { get; set; }
    }

    public class ContextSlim
    {
        public Guid? Token;
    }

    public class Context
    {
        public Guid Token;

        public Dictionary<string, object> Settings = new Dictionary<string, object>();
        public List<DacModelError> Messages { get; set; }
    }

    public static class ContextBag
    {
        public static readonly Dictionary<Guid, Context> Contexts = new Dictionary<Guid, Context>();
    }
}
