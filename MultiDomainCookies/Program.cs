using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MultiDomainCookies
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseUrls("http://localhost:5000")
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
