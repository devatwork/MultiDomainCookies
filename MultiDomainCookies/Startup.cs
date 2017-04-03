using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MultiDomainCookies
{
    public class Startup
    {
	    private const string CookieName = "TestCookie";
		
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Trace);

            app.Run(async context =>
            {
	            var host = context.Request.Host.ToString();

				// Read existing cookie, if any
	            var currentValue = context.Request.Cookies[CookieName];

				// Write the current hostname to the cookie
				context.Response.Cookies.Append(CookieName, host);

				await context.Response.WriteAsync($"Received cookie value '{currentValue}'\r\n");
				await context.Response.WriteAsync($"Cookie set to value '{host}' for host '{host}'");
            });
        }
    }
}
