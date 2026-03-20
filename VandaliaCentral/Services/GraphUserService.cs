using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

using System.Net.Http.Headers;

namespace VandaliaCentral.Services
{
    public sealed class EntraGroupOption
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }

    public class GraphUserService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public GraphUserService(
            ITokenAcquisition tokenAcquisition,
            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _tokenAcquisition = tokenAcquisition;
            _consentHandler = consentHandler;
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

        public async Task<List<EntraGroupOption>> GetGroupsAsync()
        {
            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "Group.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var groups = new List<EntraGroupOption>();

                var page = await graphClient.Groups
                    .Request()
                    .Select("id,displayName")
                    .Top(200)
                    .GetAsync();

                while (page != null)
                {
                    groups.AddRange(page.CurrentPage
                        .Where(group => !string.IsNullOrWhiteSpace(group.Id) && !string.IsNullOrWhiteSpace(group.DisplayName))
                        .Select(group => new EntraGroupOption
                        {
                            Id = group.Id!,
                            DisplayName = group.DisplayName!
                        }));

                    if (page.NextPageRequest != null)
                    {
                        page = await page.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                return groups
                    .OrderBy(group => group.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return new List<EntraGroupOption>();
            }
        }

        public async Task<HashSet<string>> GetUserMemberGroupIdsAsync(string userId, IEnumerable<string> candidateGroupIds)
        {
            var normalizedIds = candidateGroupIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(userId) || normalizedIds.Count == 0)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(async request =>
                {
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "GroupMember.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var result = await graphClient.Users[userId]
                    .CheckMemberGroups(normalizedIds)
                    .Request()
                    .PostAsync();

                return result.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
