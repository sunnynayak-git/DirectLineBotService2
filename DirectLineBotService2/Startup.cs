using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DirectLineBotService2.Startup))]
namespace DirectLineBotService2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
