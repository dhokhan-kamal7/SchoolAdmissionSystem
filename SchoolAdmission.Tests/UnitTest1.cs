
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

public class UnitTest1
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("http://localhost:8080")
    };

    private static string StudentId(int number)
        => number.ToString("D17");

    private static async Task<int> CreateClass(string name, int seats)
    {
        var response = await Client.PostAsJsonAsync("/classes", new
        {
            name,
            shift = "Morning",
            totalSeats = seats
        });

        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<HttpResponseMessage> CreateHold(
        int classId, string studentId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/classes/{classId}/holds");

        request.Headers.Add("X-Student-Id", studentId);

        return await Client.SendAsync(request);
    }

    [Fact]
    public async Task GetClasses_ReturnsSuccess()
    {
        var response = await Client.GetAsync("/classes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidClass_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/classes", new
        {
            name = "",
            shift = "Morning",
            totalSeats = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateHold_RequiresValidStudentId()
    {
        var classId = await CreateClass("Invalid ID Test", 1);

        var response = await Client.PostAsync(
            $"/classes/{classId}/holds", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateHold_ReturnsHeld()
    {
        var classId = await CreateClass("Hold Test", 1);
        var studentId = StudentId(1001);

        var response = await CreateHold(classId, studentId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "HELD",
            json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConcurrentHolds_DoNotOverbookSeats()
    {
        var classId = await CreateClass("Concurrency Test", 1);

        var requests = Enumerable.Range(2001, 10)
            .Select(i => CreateHold(classId, StudentId(i)));

        var responses = await Task.WhenAll(requests);

        var successfulRequests = responses.Count(
            r => r.StatusCode == HttpStatusCode.Created);

        Assert.Equal(1, successfulRequests);

        foreach (var response in responses)
            response.Dispose();
    }

    [Fact]
    public async Task ConfirmHold_IsIdempotent()
    {
        var classId = await CreateClass("Confirmation Test", 1);
        var studentId = StudentId(3001);

        var holdResponse = await CreateHold(classId, studentId);

        holdResponse.EnsureSuccessStatusCode();

        using var holdJson = JsonDocument.Parse(
            await holdResponse.Content.ReadAsStringAsync());

        var holdId = holdJson.RootElement
            .GetProperty("holdId").GetGuid();

        async Task<HttpResponseMessage> Confirm()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post, $"/holds/{holdId}/confirm");

            request.Headers.Add("X-Student-Id", studentId);

            return await Client.SendAsync(request);
        }

        var first = await Confirm();
        var second = await Confirm();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        first.Dispose();
        second.Dispose();
    }

    [Fact]
    public async Task GetMyHolds_ReturnsStudentApplications()
    {
        var classId = await CreateClass("My Applications Test", 1);
        var studentId = StudentId(4001);

        var holdResponse = await CreateHold(classId, studentId);
        holdResponse.EnsureSuccessStatusCode();

        var request = new HttpRequestMessage(
            HttpMethod.Get, "/me/holds");

        request.Headers.Add("X-Student-Id", studentId);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }
}
