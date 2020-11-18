using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTransformEngine.Example.Examples.BasicTransformation
{
    public class Example : IExample
    {
        protected readonly XTransformEngine.Core.XTransformEngineFactory Factory;
        public Example(XTransformEngine.Core.XTransformEngineFactory factory)
        {
            Factory = factory;
        }

        public async Task Run()
        {
            var xml = ResourceReader.GetXmlInputFile<Example>();
            var xlst = ResourceReader.GetXsltFile<Example>();

            Console.WriteLine("Creating transformation engine");
            var engine = await Factory.CreateEngine(xlst);

            Console.WriteLine("Transforming source xml");
            using var stream = new System.IO.MemoryStream();
            engine.Transform(xml, null, stream);
            Console.WriteLine("Transformation ready");

            Console.WriteLine("-- result: --------------------------------------------------");
            Console.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length));
            Console.WriteLine("-------------------------------------------------------------");

        }

    }
}
