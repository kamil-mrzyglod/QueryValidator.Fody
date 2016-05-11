namespace QueryValidator.Fody.TestAssembly
{
    public class Program
    {
        static void Main()
        {
            var queryToValidate = "|>SELECT * FROM dbo.Foo";
            var queryNotToValidate = "SELECT * FROM dbo.Foo";
        }
    }
}