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

        private bool AreLogsMuted;

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

            AreLogsMuted = Config.Attribute("MuteLogs") != null ? Config.Attribute("MuteLogs").Value == "1" : false;

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
                    LogMessageConditionally(string.Format("Validating query {0}", query));
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

        private void LogMessageConditionally(string message)
        {
            if (AreLogsMuted == false) LogInfo(message);
        }

        private IEnumerable<string> GetQueriesToValidate()
        {
            var instructions =
                ModuleDefinition.Types.SelectMany(_ => _.Methods.Where(m => m.HasBody).Select(m => m.Body))
                    .Where(_ => _.Instructions != null && _.Instructions.Any(i => i.OpCode == OpCodes.Ldstr))
                    .SelectMany(_ => _.Instructions);

            var instructionsFromNested =
                ModuleDefinition.Types.SelectMany(_ => _.NestedTypes)
                    .SelectMany(_ => _.Methods)
                    .Where(m => m.HasBody)
                    .Select(m => m.Body)
                    .Where(_ => _.Instructions != null && _.Instructions.Any(i => i.OpCode == OpCodes.Ldstr))
                    .SelectMany(_ => _.Instructions);

            instructions = instructions.Concat(instructionsFromNested);
            foreach (var instruction in instructions)
            {
                var query = instruction.Operand as string;
                if (query == null)
                    continue;

                if (query.StartsWith("|>"))
                {
                    var queryWithoutValidator = query.Replace("|>", string.Empty);
                    var cleanedQuery = Regex.Replace(queryWithoutValidator, "@[a-zA-Z]{0,}", "''", RegexOptions.Compiled);

                    LogMessageConditionally(string.Format("Cleaned query is {0}", cleanedQuery));
                    instruction.Operand = queryWithoutValidator;
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
                LogInfo(string.Format("Found configuration file {0}", ModuleDefinition.FullyQualifiedName));
                return exeConnectionString.ConnectionString;
            }

            var fullyQualifiedNameFromObj = Directory.GetFiles(Path.GetDirectoryName(ModuleDefinition.FullyQualifiedName), "*.config").FirstOrDefault();
            LogInfo(string.Format("Trying to get configuration for {0}", fullyQualifiedNameFromObj));

            var objConfiguration =
                ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = fullyQualifiedNameFromObj },
                    ConfigurationUserLevel.None).ConnectionStrings.ConnectionStrings[connectionStringName];

            if (objConfiguration != null)
            {
                LogInfo(string.Format("Found configuration file {0}", fullyQualifiedNameFromObj));
                return objConfiguration.ConnectionString;
            }

            throw new ConfigurationErrorsException("Cannot find configuration.");
        }
    }
}