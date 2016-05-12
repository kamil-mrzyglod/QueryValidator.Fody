# QueryValidator.Fody
Writing SQL queries in your code can be really tiresome - there is no syntax highlighting, code completion and other nice-to-have features which e.g. SQL Management Studio has. It is easy to make mistake, it is easy forget about something - the only thing to realize something's wrong, is to run a query. What if MSBuild could do it for you?

## How it works?
QueryValidator scans your assembly for SQL queries, which should be validated. You can tell the weaver to validate given query by appending `|>` to it:

`_connection.Query("|> SELECT * FROM dbo.Foo")`

`|>` will be removed after a build so your query will run just fine if everything's OK. If query validation returns an error, your build will be interrupted and detailed SQL error will be displayed.

Internally QueryValidator appends to your query `SET FMTONLY ON` flag, so it is only compiled, not executed. It performs a query against SQL server specified in a connection string, which can be found in your application's configuration file. By default it searches for a connection string named `MyConnectionString`, you can customize it in `FodyWeavers.xml` file:

```
<Weavers>
  <QueryValidator ConnectionStringName="MyConnectionString"/>
</Weavers>
```

## Parameters

Let's say you have following query(this example uses Dapper but you can easily imaging passing parameters with ADO.NET):

`_connection.Query("|> SELECT * FROM dbo.Foo WHERE Id = @Id", new { Id = 1 })`

Validating this query against SQL server will result in an error - `@Id` is not declared. For now QueryValidator checks whether query contains any at-something strings and replaces them with an empty string:

`_connection.Query("|> SELECT * FROM dbo.Foo WHERE Id = ''", new { Id = 1 })`

It is planned to hoist parameters in the future, so they will fake real values.
