using Microsoft.Graph;
using Microsoft.Identity.Web;

using System.Net.Http.Headers;

namespace VandaliaCentral.Services
{
    public class GraphUserService
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public GraphUserService(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
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
    }
}
