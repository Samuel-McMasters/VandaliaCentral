using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using VandaliaCentral.Models;

using System.Net.Http.Headers;
using System.Text.Json;

namespace VandaliaCentral.Services
{
    public class GraphUserService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly IHttpClientFactory _httpClientFactory;

        public GraphUserService(
            ITokenAcquisition tokenAcquisition,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
            IHttpClientFactory httpClientFactory)
        {
            _tokenAcquisition = tokenAcquisition;
            _consentHandler = consentHandler;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All", "GroupMember.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var allGroupUsers = new List<User>();

                var page = await graphClient.Groups["3b1a951d-f7f7-4da9-b6b5-f939475d21d0"].Members
                    .Request()
                    .Select("id,displayName,mail,userPrincipalName,jobTitle,mobilePhone,businessPhones,officeLocation,streetAddress,city,state,postalCode,country,onPremisesExtensionAttributes")
                    .Top(100)
                    .GetAsync();

                while (page != null)
                {
                    var users = page.CurrentPage.OfType<User>();
                    allGroupUsers.AddRange(users);

                    if (page.NextPageRequest != null)
                    {
                        page = await page.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                // Sort the list in descending alphabetical order by DisplayName
                var sortedUsers = allGroupUsers
                    .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return sortedUsers;
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return Enumerable.Empty<User>();
            }
        }



        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var users = new List<User>();

                var page = await graphClient.Users
                    .Request()
                    .Select("id,displayName,mail,userPrincipalName,jobTitle")
                    .Top(100)
                    .GetAsync();

                while (page != null)
                {
                    users.AddRange(page.CurrentPage.Where(u => !string.IsNullOrWhiteSpace(u.DisplayName)));

                    if (page.NextPageRequest != null)
                    {
                        page = await page.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                return users
                    .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return Enumerable.Empty<User>();
            }
        }



        public async Task<IEnumerable<User>> GetGroupUsersAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return Enumerable.Empty<User>();
                }

                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All", "GroupMember.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var users = new List<User>();

                var page = await graphClient.Groups[groupId].Members
                    .Request()
                    .Select("id,displayName,mail,userPrincipalName")
                    .Top(100)
                    .GetAsync();

                while (page != null)
                {
                    users.AddRange(page.CurrentPage.OfType<User>());

                    if (page.NextPageRequest != null)
                    {
                        page = await page.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return Enumerable.Empty<User>();
            }
        }

        public async Task<IEnumerable<AdminTeamMemberLocation>> GetGroupUsersWithWorkLocationAsync(string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return Enumerable.Empty<AdminTeamMemberLocation>();
                }

                var users = (await GetGroupUsersAsync(groupId)).ToList();
                if (users.Count == 0)
                {
                    return Enumerable.Empty<AdminTeamMemberLocation>();
                }

                var workLocationByUserId = await GetWorkLocationTypesByUserIdsAsync(users
                    .Select(u => u.Id)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Cast<string>()
                    .ToList());

                var userTasks = users.Select(user =>
                {
                    workLocationByUserId.TryGetValue(user.Id ?? string.Empty, out var workLocationType);
                    var workLocation = MapWorkLocation(workLocationType);

                    return new AdminTeamMemberLocation
                    {
                        UserId = user.Id ?? string.Empty,
                        DisplayName = user.DisplayName ?? user.UserPrincipalName ?? "Unknown User",
                        Mail = user.Mail,
                        UserPrincipalName = user.UserPrincipalName,
                        WorkLocation = workLocation
                    };
                });

                var members = userTasks.ToList();
                return members
                    .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return Enumerable.Empty<AdminTeamMemberLocation>();
            }
        }


        public async Task<User?> GetUserProfileAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return null;
                }

                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                return await graphClient.Users[userId]
                    .Request()
                    .Select("id,displayName,mail,userPrincipalName,officeLocation")
                    .GetAsync();
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return null;
            }
        }

        public async Task<string?> GetCurrentUserOfficeLocationAsync()
        {
            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var user = await graphClient.Me
                    .Request()
                    .Select("officeLocation")
                    .GetAsync();

                return user?.OfficeLocation;
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return null;
            }
        }

        public async Task<bool> IsUserInGroupAsync(string userId, string groupId)
        {
            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "GroupMember.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var result = await graphClient.Users[userId]
                    .CheckMemberGroups(new List<string> { groupId })
                    .Request()
                    .PostAsync();

                return result.Contains(groupId);
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Graph error: {ex.Message}");
                return false;
            }
        }

        private static string MapWorkLocation(string? workLocationType)
        {
            return workLocationType?.ToLowerInvariant() switch
            {
                "office" => "In office",
                "remote" => "Remote",
                "timeoff" => "Time off",
                _ => "Unknown"
            };
        }

        private async Task<Dictionary<string, string?>> GetWorkLocationTypesByUserIdsAsync(List<string> userIds)
        {
            var results = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (userIds.Count == 0)
            {
                return results;
            }

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Presence.Read.All" });
            var client = _httpClientFactory.CreateClient();
            const int batchSize = 100;

            for (var i = 0; i < userIds.Count; i += batchSize)
            {
                var idBatch = userIds.Skip(i).Take(batchSize).ToList();
                var requestBody = JsonSerializer.Serialize(new { ids = idBatch });

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/communications/getPresencesByUserId");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var payload = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(payload))
                {
                    continue;
                }

                using var json = JsonDocument.Parse(payload);
                if (!json.RootElement.TryGetProperty("value", out var valueElement) ||
                    valueElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var presenceItem in valueElement.EnumerateArray())
                {
                    if (!presenceItem.TryGetProperty("id", out var idElement))
                    {
                        continue;
                    }

                    var id = idElement.GetString();
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    string? workLocationType = null;

                    if (presenceItem.TryGetProperty("workLocation", out var workLocationElement) &&
                        workLocationElement.ValueKind == JsonValueKind.Object &&
                        workLocationElement.TryGetProperty("workLocationType", out var workLocationTypeElement))
                    {
                        workLocationType = workLocationTypeElement.GetString();
                    }
                    else if (presenceItem.TryGetProperty("workLocation", out workLocationElement) &&
                             workLocationElement.ValueKind == JsonValueKind.Object &&
                             workLocationElement.TryGetProperty("type", out var typeElement))
                    {
                        workLocationType = typeElement.GetString();
                    }
                    else if (presenceItem.TryGetProperty("workLocationType", out var topLevelWorkLocationType))
                    {
                        workLocationType = topLevelWorkLocationType.GetString();
                    }

                    results[id] = workLocationType;
                }
            }

            return results;
        }
    }
}
