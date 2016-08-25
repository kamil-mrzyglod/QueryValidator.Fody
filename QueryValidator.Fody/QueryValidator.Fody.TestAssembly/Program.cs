namespace QueryValidator.Fody.TestAssembly
{
    public class Program
    {
        static void Main()
        {
            var queryToValidate = "|>SELECT * FROM dbo.Foo";
            var queryNotToValidate = "SELECT * FROM dbo.Foo";

            var queryWithParameters = "|>SELECT * FROM dbo.Foo WHERE Id = @Id";
            var queryWithParametersIn = "|>SELECT * FROM dbo.Foo WHERE Id IN @Ids";
        }
    }
}