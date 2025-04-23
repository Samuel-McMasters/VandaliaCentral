using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

using System.Net.Http.Headers;

namespace VandaliaCentral.Services
{
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
                    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { "User.Read.All" });
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }));

                var allUsers = new List<User>();
                var page = await graphClient.Users
                    .Request()
                    .Select("id,displayName,mail,jobTitle,mobilePhone,businessPhones")
                    .Top(100)
                    .GetAsync();

                while (page != null)
                {
                    allUsers.AddRange(page.CurrentPage);

                    if (page.NextPageRequest != null)
                    {
                        page = await page.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                return allUsers;
            }
            catch (Exception ex)
            {
                _consentHandler.HandleException(ex);
                return Enumerable.Empty<User>();
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
    }
}