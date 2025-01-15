This project contains integration tests.

## Updating the Schema for Strawberry Shake

If GraphQL is already installed, skip to the Update the GraphQL Schema step, after the Build Project step.
If GraphQL is not installed, you need to install it. Follow these steps:

### Install StrawberryShake package

`dotnet add package StrawberryShake.Server`

### Create a dotnet-tools manifest

```
dotnet new tool-manifest\
dotnet tool install StrawberryShake.Tools --local
```

### Download the GraphQL schema

`dotnet graphql init {{ServerUrl}} -n {{ClientName}} -x x-key="{{key}}"`
The `ServerUrl` value should point to the graphql endpoint and not to the schema endpoint.
example:
`dotnet graphql init https://localhost:7155/graphql -n AppClient -x x-key="123456"`

### Customize namespace (optional)

** In `.graphqlrc.json` insert a namespace property to the "StrawberryShake" section.
`"namespace": "{{ProjectNamespace}}.GraphQL"`

For example:

```
{
  "schema": "schema.graphql",
  "documents": "**/*.graphql",
  "extensions": {
    "strawberryShake": {
      "namespace": "Client.Playground.GraphQL",
      "name": "AppClient",
      "url": "https://localhost:7155/graphql",
      "records": {
        "inputs": false,
        "entities": false
      },
      "transportProfiles": [
        {
          "default": "Http",
          "subscription": "WebSocket"
        }
      ]
    }
  }
}
```

### Add new GraphQL queries

** In the root project create your query documents
`{queryName}}.graphql`
** Populate document with:
`query {{queryName}} {...}`

### Build Project

`dotnet build`
** A generated GraphQL client is created in: `obj\Debug\net8.0\berry\{{ClientName}}.Client.cs`

### Update the GraphQL Schema

If the GraphQL schema has changed, you need to update the Strawberry Shake client. Follow these steps:

#### 1. Run the server:

Ensure the server is running.

#### 2. Navigate to the Tests Directory:

Open a terminal and navigate to the directory where the test project.

#### 3. Update the schema:

Make sure the graphql strawberry shake tool is installed by running the following command:
`dotnet graphql where`

To install the tool, run the following command:
`dotnet tool install --global StrawberryShake.Tools --version 13.9.12`

You can check the latest version of the tool [here](https://www.nuget.org/packages/StrawberryShake.Tools/).
And you can see the release notes [here](https://github.com/ChilliCream/graphql-platform/releases/).

Note, use version 13.9.12 as the latest version is not compatible with the current version of the project ("
EntityGraphQL.AspNet" Version="5.5.3").

Run the following command to update the schema from the running server:  
`dotnet graphql update -u https://localhost:7155/graphql`
Note that a key is required to access the server. To add it, run the following command:  
`dotnet graphql update -u https://localhost:7155/graphql -x x-key=YOUR_KEY`
For example:  
`dotnet graphql update -u https://localhost:7155/graphql -x x-key=123456`

