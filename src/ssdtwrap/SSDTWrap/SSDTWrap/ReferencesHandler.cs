using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Newtonsoft.Json;

namespace SSDTWrap
{
    public class ReferencesHandler : Handler
    {
        private ReferencesHandlerMessage message;

        public ReferencesHandler(string configBlob) : base(configBlob)
        {
            message = JsonConvert.DeserializeObject<ReferencesHandlerMessage>(configBlob);
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

            var currentModel = model.CurrentModel();
            var identifier = new ObjectIdentifier();

            if (!string.IsNullOrEmpty(message.SchemaName))
            {
                identifier.Parts.Add(message.SchemaName);
            }

            identifier.Parts.Add(message.Name);

            var sqlObject = currentModel.GetObjects(DacQueryScopes.All, ModelSchema.Procedure, ModelSchema.Table, ModelSchema.View, ModelSchema.ScalarFunction, ModelSchema.TableValuedFunction).FirstOrDefault(p => p.Name.Parts.Last() == message.Name);

            if (sqlObject == null)
            {
                return JsonConvert.SerializeObject(new ReferencesResponse()
                {
                    References = new List<Reference>()
                });
            }

            var referencing = sqlObject.GetReferencing();
            foreach (var r in referencing)
            {
                Console.WriteLine($"referenced by {r.Name.Parts.Last()} ");
            }

            return JsonConvert.SerializeObject(new ReferencesResponse()
            {
                References = new List<Reference>()
            });


        }
    }
}