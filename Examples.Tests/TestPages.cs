using Microsoft.Playwright;

namespace Examples.Tests;

public class TestPageTests : BasePlaywrightTests
{
	public TestPageTests(AspireManager aspireManager) : base(aspireManager) { }

	private static async Task AssertBodyContainsAsync(IPage page, string expected)
	{
		var content = await page.InnerTextAsync("body");
		Assert.Contains(expected, content);
	}

	[Fact]
	public async Task IsReady()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("server", async page =>
			{
				await page.GotoAsync("/");
				var content = await page.InnerTextAsync("body");
				Assert.Equal("Interface bridge is ready", content);
			});
	}

	[Fact]
	public async Task ClientRoot()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/");
				const string expected = "{\"name\":\"World\",\"location\":\"Earth\",\"greeting\":\"Hello, World from Earth! Time:";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test1()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/1");
				const string expected = @"{""testId"":""a0000000-b000-c000-d000-e00000000000"",""fullName"":""Bob"",""age"":42,""pocoData"":{""childName"":""Bobbie"",""childAge"":20}}";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test2()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/2");
				const string expected = @"[{""testId"":""0000000a-000b-000c-000d-00000000000e"",""fullName"":""Bob"",""age"":42,""pocoData"":{""childName"":""Bobbie"",""childAge"":20}}]";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test3()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/3");
				const string expected = @"{""testId"":""a0000000-b000-c000-d000-e00000000000"",""fullName"":""Bob"",""age"":42,""pocoData"":{""childName"":""Bobbie"",""childAge"":20}}";
				
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test4()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/4");
				const string expected = @"""a0000000-b000-c000-d000-e00000000000""";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test5()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/5");
				const string expected = "Response: World says hello!";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Test6()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/6");
				const string expected = @"""a0000000-b000-c000-d000-e00000000000""";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	// [Fact]
	// public async Task Test7_Download()
	// {
	// 	await ConfigureAsync<Projects.Examples_AppHost>();

	// 	await InteractWithPageAsync("client", async page =>
	// 		{
	// 			var download = await page.RunAndWaitForDownloadAsync(() => page.GotoAsync("/test/7"));
	// 			const string expectedFileName = "7";
	// 			Assert.Equal(expectedFileName, download.SuggestedFilename);
	// 		});
	// }

	[Fact]
	public async Task CanStream()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/stream");
				const string expected = @"0000000a-000b-000c-000d-00000000000e: Streamed Bob #5 (42)";
				await AssertBodyContainsAsync(page, expected);
			});
	}

	[Fact]
	public async Task Auth()
	{
		await ConfigureAsync<Projects.Examples_AppHost>();

		await InteractWithPageAsync("client", async page =>
			{
				await page.GotoAsync("/test/auth");
				const string expected = 
				"""
				Anonymous access:
				Testing   Anonymous...Success
				Testing   Authorize...Failure: Unauthorized
				Testing   Admin...Failure: Unauthorized

				User access:
				Testing   Sign-In...Success
				Testing   Anonymous...Success
				Testing   Authorize...Success
				Testing   Admin...Failure: Forbidden
				Testing   Sign-Out...Success

				Admin access:
				Testing   Sign-In...Success
				Testing   Anonymous...Success
				Testing   Authorize...Success
				Testing   Admin...Success
				Testing   Sign-Out...Success
				""";
				await AssertBodyContainsAsync(page, expected);
			});
	}
}
