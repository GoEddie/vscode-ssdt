using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SSDTWrap.Messages;

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
            var contextSlim = JsonConvert.DeserializeObject<ContextSlimMessage>(ConfigBlob);

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
}
