using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace XTransformEngine.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            XTransformEngine.Core.Installers.LibraryInstaller.Install(services);
            services.AddTransient<Examples.BasicTransformation.Example>();
            services.AddTransient<Example.Examples.CustomExtension.Example>();
                       
            
            var container = services.BuildServiceProvider();

            //var example = container.GetService<Examples.BasicTransformation.Example>();
            var example = container.GetService<Examples.CustomExtension.Example>();

            try
            {
                Console.WriteLine($"Executing example: {example.GetType().FullName}");
                example.Run().Wait();
                Console.WriteLine($"Example completed");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }

            Console.WriteLine("press any key");
            Console.ReadKey();
        }
    }



    public interface IExample
    {
        Task Run();
    }

    public static class ResourceReader
    {
        public static XmlReader GetXmlInputFile<T>()
        {
            var resource = GetResourceName(typeof(T).Namespace, "input.xml");
            if (resource == null) throw new EntryPointNotFoundException("failed to load input.xml");
            return ReadXmlStream(GetResourceStream(resource));
        }

        public static XmlReader GetXsltFile<T>()
        {
            var resource = GetResourceName(typeof(T).Namespace, "transformation.xslt");
            if (resource == null) throw new EntryPointNotFoundException("failed to load transformation.xml");
            return ReadXmlStream(GetResourceStream(resource));
        }

        public static string[] GetResourceNames()
        {
            return typeof(ResourceReader).Assembly.GetManifestResourceNames();
        }
        public static string GetResourceName(string ns, string filename)
        {
            var suffix = $"{ns}.{filename}";
            return GetResourceNames()
                .Where(name => name.EndsWith(suffix))
                .FirstOrDefault();
        }
        public static System.IO.Stream GetResourceStream(string name)
        {
            return typeof(ResourceReader).Assembly.GetManifestResourceStream(name);
        }
        public static XmlReader ReadXmlStream(System.IO.Stream stream)
        {
            return XmlReader.Create(stream, new XmlReaderSettings
            {
                CloseInput = true,
                Async = true
            });
        }
    }
}
