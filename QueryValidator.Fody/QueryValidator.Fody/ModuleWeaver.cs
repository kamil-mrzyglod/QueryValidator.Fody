using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public Action<string> LogInfo { get; set; }

        public ModuleWeaver()
        {
            LogError = s => { };
            LogInfo = s => { };
        }

        public void Execute()
        {
            var connectionStringName = ConnectionStringName;
            var attr = Config.Attribute("ConnectionStringName");
            if (attr != null)
                connectionStringName = attr.Value;

            var queries = GetQueriesToValidate();
            if (queries == null)
            {
                LogInfo("No query to validate.");
                return;
            }

            var enumerable = queries as IList<string> ?? queries.ToList();
            LogInfo(string.Format("Found {0} queries to validate.", enumerable.Count()));

            var connectionString = GetConnectionStringFromConfig(connectionStringName);
            LogInfo(string.Format("Connection string is {0}", connectionString));

            ValidateQueries(connectionString, enumerable);
        }

        private void ValidateQueries(string connectionString, IEnumerable<string> queries)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                foreach (var query in queries)
                {
                    LogInfo(string.Format("Validating query {0}", query));
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
            var instructions =
                ModuleDefinition.Types.SelectMany(_ => _.Methods.Where(m => m.HasBody).Select(m => m.Body))
                    .Where(_ => _.Instructions != null && _.Instructions.Any(i => i.OpCode == OpCodes.Ldstr))
                    .SelectMany(_ => _.Instructions);

            foreach (var instruction in instructions)
            {
                var query = instruction.Operand as string;
                if (query == null)
                    continue;

                if (query.StartsWith("|>"))
                {
                    var cleanedQuery = Regex.Replace(query.Replace("|>", string.Empty), "@[a-zA-Z]{0,}", "''");

                    LogInfo(string.Format("Cleaned query is {0}", cleanedQuery));
                    instruction.Operand = cleanedQuery;
                    yield return cleanedQuery;
                }
            }
        }

        private string GetConnectionStringFromConfig(string connectionStringName)
        {
            LogInfo(string.Format("Trying to get configuration for {0}", ModuleDefinition.FullyQualifiedName));

            var connectionStrings = ConfigurationManager.OpenExeConfiguration(ModuleDefinition.FullyQualifiedName).ConnectionStrings;
            var exeConnectionString = connectionStrings.ConnectionStrings[connectionStringName];
            if (exeConnectionString != null)
            {
                return exeConnectionString.ConnectionString;
            }

            string filePath;
            if(TryToFindConfigurationFile(out filePath) == false)
                throw new ArgumentException("Cannot find configuration file.");

            LogInfo(string.Format("Found configuration file {0}", filePath));

            var map = new ExeConfigurationFileMap { ExeConfigFilename = filePath };
            var connectionStringSettings = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None)
                .ConnectionStrings.ConnectionStrings[connectionStringName];

            if(connectionStringSettings == null)
                throw new ArgumentNullException("connectionStringName", "Cannot find specified connection string");

            return
                connectionStringSettings.ConnectionString;
        }

        private bool TryToFindConfigurationFile(out string filePath)
        {
            var dllDirParent = Directory.GetParent(ModuleDefinition.FullyQualifiedName).Parent;
            if (dllDirParent == null)
            {
                LogError("Cannot find configuration file");
                filePath = null;
                return false;
            }

            filePath = string.Empty;
            if (dllDirParent.GetFiles("web.config").Any())
            {
                filePath = dllDirParent.FullName + @"\web.config";
                return true;
            }

            if (dllDirParent.GetFiles("app.config").Any())
            {
                filePath = dllDirParent.FullName + @"\app.config";
                return true;
            }

            if (dllDirParent.Parent != null && dllDirParent.Parent.GetFiles("web.config").Any())
            {
                filePath = dllDirParent.Parent.FullName + @"\web.config";
                return true;
            }

            if (dllDirParent.Parent != null && dllDirParent.Parent.GetFiles("app.config").Any())
            {
                filePath = dllDirParent.Parent.FullName + @"\app.config";
                return true;
            }

            return false;
        }
    }
}