using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GOEddie.Dacpac.References;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Extensions;
using Microsoft.SqlServer.Dac.Model;

namespace Dir2Dac
{
    public class Program
    {
        public static int Main(params string[] args)
        {
            var argParser = new Args(args);

            if (argParser.Parse() != ParseResult.Ok)
            {
               Args.PrintArgs();
                return -1;
            }

            var creator = new DacCreator(argParser);
            if (!creator.Write())
            {
                return -2;
            }
            

            return 0;
        }

        private static void ShowHelp()
        {
            
        }
    }
}
