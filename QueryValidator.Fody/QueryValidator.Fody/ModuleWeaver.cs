using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
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

        public string SolutionDirectoryPath { get; set; }

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
                            command.CommandText = string.Format("SET FMTONLY ON;{0}", query);
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
                        {
                            var cleanedQuery = query.Replace("|>", string.Empty);
                            instruction.Operand = cleanedQuery;
                            yield return cleanedQuery;
                        }                            
                    }
                }
            }
        }

        private string GetConnectionStringFromConfig(string connectionStringName)
        {
            var connectionStrings = ConfigurationManager.OpenExeConfiguration(ModuleDefinition.FullyQualifiedName).ConnectionStrings;
            var exeConnectionString = connectionStrings.ConnectionStrings[connectionStringName];
            if (exeConnectionString != null)
            {
                return exeConnectionString.ConnectionString;
            }

            var dllDirParent = Directory.GetParent(ModuleDefinition.FullyQualifiedName).Parent;
            if (dllDirParent == null)
            {
                LogError("Cannot find configuration file");
                return null;
            }

            var filePath = string.Empty;
            if (dllDirParent.GetFiles("web.config").Any())
                filePath = dllDirParent.FullName + @"\web.config";

            if(dllDirParent.Parent != null && dllDirParent.Parent.GetFiles("web.config").Any())
                filePath = dllDirParent.Parent.FullName + @"\web.config";

            var map = new ExeConfigurationFileMap { ExeConfigFilename = filePath };
            var connectionStringSettings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None)
                .ConnectionStrings.ConnectionStrings[connectionStringName];

            if(connectionStringSettings == null)
                throw new ArgumentNullException("connectionStringName", "Cannot find specified connection string");

            return
                connectionStringSettings.ConnectionString;
        }
    }
}