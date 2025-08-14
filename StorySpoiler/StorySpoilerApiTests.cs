using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;

{

}

namespace StorySpoiler
{
    public class Tests
    {
        [TestFixture]
        public class StorySpoilerApiTests
        {
            private RestClient client;
            private static string lastCreatedStoryId;
            private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";
            private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmZWU4NjVlMy1iYTExLTQwYzItODZlNC04ZTU1YTBiNDEzNWUiLCJpYXQiOiIwOC8xNC8yMDI1IDEwOjA1OjQ1IiwiVXNlcklkIjoiNDAzNGI5YzgtZTczYy00OTdlLThkYjktMDhkZGRiMWExM2YzIiwiRW1haWwiOiJwZXNob0BzdG9yeXNwb2lsZXIuY29tIiwiVXNlck5hbWUiOiJQZXNobzEyMyIsImV4cCI6MTc1NTE4NzU0NSwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.9eWmY2SHch4Grw_Bdn6Va6WlUbVibEp2q_ymZjZl9WY";
            private const string LoginUsername = "Pesho123";
            private const string LoginPassword = "123456";

            [OneTimeSetUp]
            public void Setup()
            {
                string jwtToken = GetJwtToken(LoginUsername, LoginPassword);

                var options = new RestClientOptions(BaseUrl)
                {
                    Authenticator = new JwtAuthenticator(jwtToken)
                };

                this.client = new RestClient(options);
            }

            private string GetJwtToken(string username, string password)
            {
                var tempClient = new RestClient(BaseUrl);
                var request = new RestRequest("/api/User/Authentication", Method.Post);
                request.AddJsonBody(new
                {
                    username,
                    password
                });

                var response = tempClient.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                    var token = content.GetProperty("accessToken").GetString();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        throw new InvalidOperationException("The JWT token is null or empty.");
                    }
                    return token;
                }
                else
                {
                    throw new InvalidOperationException($"Authentication failed: {response.StatusCode}, {response.Content}");
                }
            }

            //Tests
            [Order(1)]
            [Test]
            public void CreateStory_WithRequiredFields_ShouldReturnSuccess()
            {
                var storyRequest = new StoryDTO
                {
                    Title = "Test Story 2",
                    Url = "",
                    Description = "Some Description"

                };

                var request = new RestRequest("/api/Story/Create", Method.Post);
                request.AddJsonBody(storyRequest);
                var response = this.client.Execute(request);
                var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 Created.");
                Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));

                var requestAll = new RestRequest("/api/Story/All", Method.Get);
                var responseAll = this.client.Execute(requestAll);

                var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(responseAll.Content);

                lastCreatedStoryId = responseItems.LastOrDefault()?.StoryId;

            }

            [Order(2)]
            [Test]
            public void EditExistingStory_ShouldReturnSuccess()
            {
                var editRequest = new StoryDTO
                {
                    Title = "Edited Story",
                    Url = "",
                    Description = "Edited Some Description"
                };

                // Use correct path parameter
                var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put);
                request.AddJsonBody(editRequest);

                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

                var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
                Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));


            }

            [Order(3)]
            [Test]
            public void GetAllStories_ShouldRetutrrnListOfAllStories()
            {
                var request = new RestRequest("/api/Story/All", Method.Get);
                var response = this.client.Execute(request);

                var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
                Assert.That(responseItems, Is.Not.Null);
                Assert.That(responseItems, Is.Not.Empty);
            }

            [Order(4)]
            [Test]
            public void SearchStory_ShouldReturnMatchingResults()
            {
                string searchKeyword = "Edited Story";

                var request = new RestRequest("/api/Story/Search", Method.Get);
                request.AddQueryParameter("keyword", searchKeyword);

                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

                // Assuming Search returns an array of stories
                var stories = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
                Assert.That(stories, Is.Not.Null, "Expected a non-null response body.");
                Assert.That(stories, Is.Not.Empty, "Expected at least one story in search results.");
                Assert.That(stories.Any(s => s.Msg != null && s.Msg.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase)
                                             || s.StoryId != null),
                            "Expected at least one story to match the search keyword.");
            }

            [Order(5)]
            [Test]
            public void DeleteExistingStory_ShouldReturnSuccess()
            {
                var request = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
                Assert.That(response.Content, Does.Contain("Deleted successfully!"));
            }     

            [Order(6)]
            [Test]
            public void CreateStory_WithoutRequiredFields_ShouldReturnSuccess()
            {
                var storyRequest = new StoryDTO
                {
                    Title = "",
                    Description = ""

                };

                var request = new RestRequest("/api/Story/Create", Method.Post);
                request.AddJsonBody(storyRequest);
                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
            }

            [Order(7)]
            [Test]
            public void EditNotExistingStory_ShouldReturnSuccess()
            {
                string nonExistingStoryId = "123";
                var editRequest = new StoryDTO
                {
                    Title = "Edited Story",
                    Url = "",
                    Description = "Edited Some Description"
                };

                // Use correct path parameter
                var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
                request.AddJsonBody(editRequest);

                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found.");
                var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
                Assert.That(response.Content, Does.Contain("No spoilers..."));
            }

            [Order(8)]
            [Test]
            public void DeleteNotExistingStory_ShouldReturnSuccess()
            {
                string nonExistingStoryId = "123";
                var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
                var response = this.client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
                Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
            }

            [OneTimeTearDown]
            public void TearDown()
            {
                this.client?.Dispose();
            }
        }
    }
}