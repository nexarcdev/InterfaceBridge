using Aspire.Hosting;

namespace Examples.Tests;

public class EnvVarTests
{
	[Fact]
	public async Task WebResourceEnvVarsResolveToApiService()
	{
		// Arrange
		var appHost = await DistributedApplicationTestingBuilder
				.CreateAsync<Projects.Examples_AppHost>();

		var frontend = (IResourceWithEnvironment)appHost.Resources
				.Single(static r => r.Name == "client");

		// Act
		var envVars = await frontend.GetEnvironmentVariableValuesAsync(
				DistributedApplicationOperation.Publish);

		// Assert
		//new()
		//{
		//	["services__api__https__0"] = "{api.bindings.https.url}",
		//}
		Assert.Contains(
			new KeyValuePair<string, string>("services__server__https__0", "{server.bindings.https.url}"),
			envVars);

	}
}