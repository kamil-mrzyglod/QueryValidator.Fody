using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace QueryValidator.Fody.Test
{
    public class QueryValidatorWebTest
    {
        private const string AssemblyPath = @"../../../../QueryValidator.Fody.TestWeb/bin/QueryValidator.Fody.TestWeb.dll";

        private string _weavedAssemblyName;

        private ModuleWeaver _weaver;

        [SetUp]
        public void SetUp()
        {
            _weavedAssemblyName = AssemblyDirectory + $"QueryValidator.Fody.TestWeb{DateTime.Now.Ticks}.dll";

            var md = ModuleDefinition.ReadModule(Path.GetFullPath(AssemblyDirectory + AssemblyPath));
            var xe = new XElement(
                "QueryValidator",
                 new XAttribute("ConnectionStringName", "MyConnectionString"),
                 new XAttribute("ConfigurationType", "Web"));

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

        [Test]
        public void GivedWeavedAssembly_ShouldContainNoQuery_WithValidationSign()
        {
            var notCleanedQueries =
                _weaver.ModuleDefinition.Types.Where(
                    _ =>
                        _.Methods.Any(
                            m =>
                                m.Body.Instructions.Any(
                                    i =>
                                        i.Operand != null && (i.Operand as string) != null &&
                                        (i.Operand as string).StartsWith("|>"))));

            Assert.That(notCleanedQueries, Is.Empty);
        }
    }
}