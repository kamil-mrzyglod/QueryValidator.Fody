<img src="https://raw.github.com/kamil-mrzyglod/QueryValidator.Fody/master/Icons/noun_364878_cc.png" width="100" /> 

# QueryValidator.Fody
Writing SQL queries in your code can be really tiresome - there is no syntax highlighting, code completion and other nice-to-have features which e.g. SQL Management Studio has. It is easy to make mistake, it is easy forget about something - the only thing to realize something's wrong, is to run a query. What if MSBuild could do it for you?

## Installation [![NuGet Status](https://img.shields.io/nuget/v/QueryValidator.Fody.svg?style=flat)](https://www.nuget.org/packages/QueryValidator.Fody/)
QueryValidator.Fody can be installed using NuGet Packages Manager:

```Install-Package QueryValidator.Fody ```

## How it works?
QueryValidator scans your assembly for SQL queries, which should be validated. You can tell the weaver to validate given query by appending `|>` to it:

```
_connection.Query("|> SELECT * FROM dbo.Foo")
```

`|>` will be removed after a build so your query will run just fine if everything's OK. If query validation returns an error, your build will be interrupted and detailed SQL error will be displayed.

Internally QueryValidator appends to your query `SET FMTONLY ON` flag, so it is only compiled, not executed. It performs a query against SQL server specified in a connection string, which can be found in your application's configuration file. By default it searches for a connection string named `MyConnectionString`, you can customize it in `FodyWeavers.xml` file:

```
<Weavers>
  <QueryValidator ConnectionStringName="MyConnectionString"/>
</Weavers>
```

## Parameters

Let's say you have following query(this example uses [Dapper](https://github.com/StackExchange/dapper-dot-net) but you can easily imagine passing parameters with ADO.NET):

`_connection.Query("|> SELECT * FROM dbo.Foo WHERE Id = @Id", new { Id = 1 })`

Validating this query against SQL server will result in an error - `@Id` is not declared. For now QueryValidator checks whether query contains any at-something strings and replaces them with an empty string:

`_connection.Query("|> SELECT * FROM dbo.Foo WHERE Id = ''", new { Id = 1 })`

It is planned to hoist parameters in the future, so they will fake real values.

## Configuration transformations

If you use QueryValidator with configuration transformation tools(like SlowCheetah), you can face a problem, when it cannot find a configuration file. This is caused by a fact, that those transformation often happen *after the build* - because Fody starts immediately after compilation(and runs QueryValidator), there is no correct configuration file, which can be used to get a connection string. 

A solution for this is to create a custom `.targets` file, which will execute transformation task *before* Fody(below example runs `TransformAllFiles` from SlowCheetah):

```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Target Name="RunTransformationsOnDemand" BeforeTargets="AfterCompile"
	<CallTarget Targets="TransformAllFiles"/>
  </Target>
</Project>
```

You only need to import above file inside your `.csproj` file.

## Icon
[database](https://thenounproject.com/term/database/364878/) by Nimal Raj from the Noun Project
