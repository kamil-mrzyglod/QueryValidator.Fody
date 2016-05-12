namespace QueryValidator.Fody.TestWeb
{
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    using Dapper;

    public class TestClass
    {
        public async Task Foo()
        {
            using (var connection = new SqlConnection())
            {
                await connection.QueryAsync(@"|> SELECT * FROM dbo.Foo");
            }
        }

        public void Bar()
        {
            using (var connection = new SqlConnection())
            {
                connection.Query(@"|> SELECT * FROM dbo.Foo");
            }
        }
    }
}