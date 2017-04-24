using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Model;

namespace SSDTWrap
{
    class Program
    {
        static void Main(string[] args)
        {

           //var model = new TSqlModel(@"C:\dev\SSDT-DevPack\src\Test\Common\SampleSolutions\NestedProjects\Nested2\bin\Debug\Nested2.dacpac");
           // var options = model.CopyModelOptions();
           // Console.WriteLine(options.AutoShrink);

            var listener = new Listener(3232);
            listener.Listen();

            Console.ReadLine();

        }
    }
}
