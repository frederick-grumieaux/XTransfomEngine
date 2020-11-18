using System;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace XTransformEngine.Core
{
    public record XParam(string Namespace, string Name, object Value);
    public record XExtension(string Namespace, object Extension);
    public record XTransformResult(XElement Xslt, IReadOnlyCollection<XParam> Params, IReadOnlyCollection<XExtension> Objects);

    public class XTransformEngineFactory
    {
        protected readonly Func<XTransformEngine> EngineFactory;
        protected readonly IStylesheetPreProcessor[] PreProcessors;

        public XTransformEngineFactory(Func<XTransformEngine> factory, IEnumerable<IStylesheetPreProcessor> preProcessors)
        {
            EngineFactory = factory;
            PreProcessors = preProcessors?.Where(x => x != null)?.ToArray() ?? new IStylesheetPreProcessor[0];
        }

        /// <summary>
        /// Returns a new XTransformationEngine for the specified Xslt.
        /// </summary>
        /// <param name="xslt">The transformation which should be applied.</param>
        /// <param name="enableDebug">Indicates if debugging is allowed.</param>
        /// <param name="xmlResolver">The Xml Resolver.</param>
        /// <returns></returns>
        public virtual async Task<XTransformEngine> CreateEngine(XmlReader xslt, bool enableDebug = false, XmlResolver xmlResolver = null)
        {
            var realEngine = new XslCompiledTransform(enableDebug);

            var original = XElement.Load(xslt, LoadOptions.None);
            var meta = await PreProcessStylesheet(original);

            using var xmlreader = await ConvertToXmlReader(meta.Xslt);
            realEngine.Load(xmlreader, new XsltSettings(true, true), xmlResolver);

            var wrapper = EngineFactory();            
            await wrapper.Init(realEngine, meta.Params, meta.Objects);

            return wrapper;
        }
        /// <summary>
        /// Handles the pre-processing of the XSLT.
        /// In this phase custom code can be generated, or added to the transformation engine.
        /// </summary>
        /// <param name="stylesheet">The stylesheet.</param>
        /// <param name="extensionParams">Extension parameters.</param>
        /// <param name="extensionObjects">Extension objects.</param>
        /// <returns>The modified stylesheet.</returns>
        protected virtual async Task<XTransformResult> PreProcessStylesheet(XElement stylesheet)
        {
            //deep-copy of the original stylesheet
            var modified = new XElement(stylesheet);

            //stuff that should be changed:
            var xparams = new LinkedList<XParam>();
            var xetentions = new LinkedList<XExtension>();
            var removed = new LinkedList<XElement>();

            //run preprocessors (should not modify the xelement)
            foreach (var processor in PreProcessors)
                await processor.Visit(modified,
                    xp => xparams.AddLast(xp),
                    xe => xetentions.AddLast(xe),
                    el => removed.AddLast(el));

            //now modify the xelement
            foreach (var node in removed)
                if (node.Parent != null)
                    node.Remove();

            //export parameters
            return new XTransformResult(modified, xparams, xetentions);
        }
        /// <summary>
        /// Converts the XElement to an XmlReader that will read the XElement.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        protected virtual async Task<XmlReader> ConvertToXmlReader(XElement xml)
        {
            var memorystream = new System.IO.MemoryStream();
            using var xmlwriter = XmlWriter.Create(memorystream, new XmlWriterSettings
            {
                Async = true,
                CloseOutput = false
            });
            //await xml.Save(xmlwriter, System.Threading.CancellationToken.None);
            xml.Save(memorystream, SaveOptions.DisableFormatting);
            memorystream.Position = 0;
            
            return XmlReader.Create(memorystream, new XmlReaderSettings
            {
                CloseInput = true,
                Async = true
            });
        }
    }

    /// <summary>
    /// This class can be used transform multiple xmls.
    /// Should be threadsafe.
    /// Use XTransformEngineFactory.CreateEngine(..) to get this object.
    /// </summary>
    public class XTransformEngine
    {
        protected XslCompiledTransform Engine;
        protected IReadOnlyCollection<XParam> ExtensionParams;
        protected IReadOnlyCollection<XExtension> ExtensionObjects;

        public virtual async Task Init(XslCompiledTransform engine, IReadOnlyCollection<XParam> parameters, IReadOnlyCollection<XExtension> extensions)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            ExtensionParams = parameters;
            ExtensionObjects = extensions;
        }

        public XmlWriterSettings OutputSettings => Engine?.OutputSettings ?? throw new Exception("XTransformaEngine not initialized");

        public void Transform(XmlReader input, XsltArgumentList arguments, System.IO.Stream output)
        {
            if (arguments == null)
                arguments = new XsltArgumentList();

            if (ExtensionParams != null)
                foreach (var param in ExtensionParams)
                    arguments.AddParam(param.Name, param.Namespace, param.Value);
            if (ExtensionObjects != null)
                foreach (var extension in ExtensionObjects)
                    arguments.AddExtensionObject(extension.Namespace, extension.Extension);

            Engine.Transform(input, arguments, output);
        }
    }

    /// <summary>
    /// Defines a stylesheet preprocessor.
    /// </summary>
    public interface IStylesheetPreProcessor
    {
        /// <summary>
        /// Will be called once for each XSLT, allows to inject parameters or code on the fly.
        /// </summary>
        /// <param name="stylesheet">Represents the xslt</param>
        /// <param name="registerParam">Allows to register a parameter.</param>
        /// <param name="registerObject">Allows to register an object.</param>
        /// <param name="delete">Remove this node from the XSLT that is processed further in the line.</param>
        /// <returns></returns>
        ValueTask Visit(XElement stylesheet, Action<XParam> registerParam, Action<XExtension> registerObject, Action<XElement> delete);
    }
}
