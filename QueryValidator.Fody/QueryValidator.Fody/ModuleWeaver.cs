using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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

        public Action<string> LogError { get; set; }

        public ModuleWeaver()
        {
            LogError = s => { };
        }

        public void Execute()
        {
            var connectionStringName = ConnectionStringName;
            var attr = Config.Attribute("ConnectionStringName");
            if (attr != null)
                connectionStringName = attr.Value;

            var queries = GetQueriesToValidate();
            var connectionString = GetConnectionStringFromConfig(connectionStringName);

            ValidateQueries(connectionString, queries);
        }

        private void ValidateQueries(string connectionString, IEnumerable<string> queries)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (var query in queries)
                {
                    using (var command = new SqlCommand())
                    {
                        try
                        {
                            command.Connection = connection;
                            command.CommandText = string.Format("SET NOEXEC ON;{0}", query);
                            command.ExecuteNonQuery();
                        }
                        catch (SqlException ex)
                        {
                            LogError(ex.Message);
                        }
                    }
                }
            }
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