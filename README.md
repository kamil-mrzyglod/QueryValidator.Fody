# QueryValidator.Fody
Writing SQL queries in your code can be really tiresome - there is no syntax highlighting, code completion and other nice-to-have features which e.g. SQL Management Studio has. It is easy to make mistake, it is easy forget about something - the only thing to realize something's wrong, is to run a query. What if MSBuild can do it for you?

## How it works?
QueryValidator scans your assembly for SQL queries, which should be validated. You can tell weaver to validate given query by appending `|>` to it:

`_connection.Query("|> SELECT * FROM dbo.Foo")`

`|>` will be removed after a build so your query will run just fine if everything's OK. If query validation returns an error, your build will be interrupted and specific info will be displayed.

Internally QueryValidator appends to your query `SET FMTONLY ON` flag, so it is only compiled, not executed. It uses connection string from weaved assembly, so in case you have any concerns about giving it too much power, you can explicitely specify, which one should be used in `FodyWeavers.xml`:

```
<Weavers>
  <QueryValidator ConnectionStringName="MyConnectionString"/>
</Weavers>
```
