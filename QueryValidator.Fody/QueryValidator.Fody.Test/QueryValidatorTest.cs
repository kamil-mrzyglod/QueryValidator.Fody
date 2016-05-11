using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace QueryValidator.Fody.Test
{
    public class QueryValidatorTest
    {
        private const string AssemblyPath = @"../../../../QueryValidator.Fody.TestAssembly/bin/Debug/QueryValidator.Fody.TestAssembly.dll";

        private string _weavedAssemblyName;

        private ModuleWeaver _weaver;

        [SetUp]
        public void SetUp()
        {
            _weavedAssemblyName = AssemblyDirectory + $"QueryValidator.Fody.TestAssembly{DateTime.Now.Ticks}.dll";

            var md = ModuleDefinition.ReadModule(Path.GetFullPath(AssemblyDirectory + AssemblyPath));
            var xe = new XElement(
                "QueryValidator",
                 new XAttribute("ConnectionStringName", "MyConnectionString"));

            _weaver = new ModuleWeaver { ModuleDefinition = md, Config = xe };

            _weaver.Execute();
            md.Write(_weavedAssemblyName);
        }

        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}