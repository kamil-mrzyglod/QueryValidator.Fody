using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace QueryValidator.Fody
{
    public class ModuleWeaver
    {
        private const string ConnectionStringName = "DefaultConnection";

        public ModuleDefinition ModuleDefinition { get; set; }

        public XElement Config { get; set; }

        public void Execute()
        {
            var connectionStringName = ConnectionStringName;
            var attr = Config.Attribute("ConnectionStringName");
            if (attr != null)
                connectionStringName = attr.Value;

            var queries = GetQueriesToValidate();

            var connectionString = GetConnectionStringFromConfig(connectionStringName);
        }

        private IEnumerable<string> GetQueriesToValidate()
        {
            var validTypes =
                ModuleDefinition.Types.Where(_ => _.Methods.Any(m => m.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr)));
            foreach (var methods in validTypes.Select(_ => _.Methods))
            {
                foreach (var method in methods)
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        var query = instruction.Operand as string;
                        if(query == null)
                            continue;

                        if (query.StartsWith("|>"))
                            yield return query;
                    }
                }
            }
        }

        private string GetConnectionStringFromConfig(string connectionStringName)
        {
            var connectionStrings = ConfigurationManager.OpenExeConfiguration(ModuleDefinition.FullyQualifiedName).ConnectionStrings;
            return connectionStrings.ConnectionStrings[connectionStringName].ConnectionString;
        }
    }
}