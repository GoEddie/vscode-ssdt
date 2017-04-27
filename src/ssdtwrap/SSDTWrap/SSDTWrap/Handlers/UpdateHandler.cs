using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Isam.Esent.Interop;
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
            var parseErrors = model.AddFile(file).ToList();

            model.Validate();
            Context.Messages.Clear();
            var modelErrors = model.CurrentModel().GetModelErrors().ToList().ToGenericError();
            Context.Messages.AddRange(modelErrors);
            Context.Messages.AddRange(parseErrors.ToGenericError(file.FullName));

            return JsonConvert.SerializeObject(Context);
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

    public static class GenericErrorExtensions
    {
        public static List<GenericError> ToGenericError(this List<ParseError> source, string filename)
        {
            var ret = new List<GenericError>();
            foreach (var error in source)
            {
                ret.Add(new GenericError(error, filename));
            }

            return ret;
        }

        public static List<GenericError> ToGenericError(this List<DacModelError> source)
        {
            var ret = new List<GenericError>();
            foreach (var error in source)
            {
                ret.Add(new GenericError(error));
            }

            return ret;
        }
    }

    public class GenericError
    {


        public string Message { get; set; }
        public int Line { get; set; }
        public string FileName { get; set; }
        public string Prefix { get; set; }
        public int Column { get;set; }

        public GenericError(ParseError err, string fileName)
        {
            Message = err.Message;
            Line = err.Line;
            FileName = fileName;
            Prefix = "Parse Error";
            SourceName = "Parse Error";
            Column = err.Column;
        }

        public GenericError(DacModelError modelError)
        {
            Message = modelError.Message;
            Line = modelError.Line;
            FileName = modelError.SourceName;
            Prefix = modelError.Prefix;
            SourceName = modelError.SourceName;
            Column = modelError.Column;
        }

        public string SourceName { get; set; }
    }


}