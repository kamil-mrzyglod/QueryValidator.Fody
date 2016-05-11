using System.Configuration;
using System.Xml.Linq;
using Mono.Cecil;

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

            var connectionString = GetConnectionStringFromConfig(connectionStringName);
        }

        private string GetConnectionStringFromConfig(string connectionStringName)
        {
            var connectionStrings = ConfigurationManager.OpenExeConfiguration(ModuleDefinition.FullyQualifiedName).ConnectionStrings;
            return connectionStrings.ConnectionStrings[connectionStringName].ConnectionString;
        }
    }
}