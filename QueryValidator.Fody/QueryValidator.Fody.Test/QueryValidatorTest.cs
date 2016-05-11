using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace QueryValidator.Fody.Test
{
    public class QueryValidatorTest
    {
        private const string AssemblyPath = @"../../../../QueryValidator.Fody.TestAssembly/bin/Debug/QueryValidator.Fody.TestAssembly.exe";

        private string _weavedAssemblyName;

        private ModuleWeaver _weaver;

        [SetUp]
        public void SetUp()
        {
            _weavedAssemblyName = AssemblyDirectory + $"QueryValidator.Fody.TestAssembly{DateTime.Now.Ticks}.exe";

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