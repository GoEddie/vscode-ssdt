using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using SSDTWrap.DacFx;

namespace SSDTWrap
{
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
            var parseErrors = model.AddFile(file);

            model.Validate();
            Context.Messages = model.CurrentModel().GetModelErrors().ToList();
            Context.Messages.AddRange(parseErrors);
            return JsonConvert.SerializeObject(Context);
        }

        public class GenericError
        {
            public GenericError(ParseError err)
            {
                MasterKey this FullTextCatalogAndFileGroup return these instead of specific errors - also on register probably need to do this as well so we pick up all initial erros and not just model validation errrors.
            }

            public GenericError(DacModelError modelError)
            {
                
            }
        }
        /*
         public ParseError(int number, int offset, int line, int column, string message);
    public int Number { get; }
    public int Offset { get; }
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
         */
        /*
            public ModelErrorType ErrorType { get; }
   /// <summary>DacModelError error code</summary>
   public int ErrorCode { get; }
   /// <summary>Line Number of the error</summary>
   public int Line { get; }
   /// <summary>Column Number of the error</summary>
   public int Column { get; }
   /// <summary>DacModelError prefix</summary>
   public string Prefix { get; }
   /// <summary>Message from DacModelError</summary>
   public string Message { get; }
   /// <summary>
   /// The TSqlObject with error.
   /// Can be null if the object creation failed completely.
   /// Could be a partially constructed object in case of partial failures in object creation.
   /// </summary>
   public string SourceName { get; }*/
    }
}