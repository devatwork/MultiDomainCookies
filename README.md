# MultiDomainCookies repro

It looks like ASP.NET Core does not currently work with multiple cookies with the same name. Having multiple cookies with the same name is valid according to [RFC 6265](https://tools.ietf.org/html/rfc6265#section-4.2.2).

I first encountered this on one of our acceptance environment but I managed to boil it down to the following repro:

1. Clone [https://github.com/devatwork/MultiDomainCookies](https://github.com/devatwork/MultiDomainCookies)
1. Change host file to map `sub.localhost` and `sub.sub.localhost` to `127.0.0.1`
1. Run the cloned application
1. Open `http://sub.localhost:5000/` in Microsoft Edge, this will give you your first cookie
1. Navigate `http://sub.sub.localhost:5000/`, this will give you your second cookie
1. Navigate to `http://sub.sub.localhost:5000/test` and observe both cookies are send along in the HTTP request
1. Observe only one of the values is read

Raw HTTP request (Recorded by Fiddler):

```
GET http://sub.sub.localhost:5000/test HTTP/1.1
Accept: text/html, application/xhtml+xml, image/jxr, */*
Accept-Language: nl-NL,nl;q=0.8,en-US;q=0.5,en;q=0.3
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393
Accept-Encoding: gzip, deflate
Host: sub.sub.localhost:5000
Connection: Keep-Alive
Pragma: no-cache
Cookie: TestCookie=sub.localhost%3A5000; TestCookie=sub.sub.localhost%3A5000
```

It looks like [IRequestCookieCollection](https://github.com/aspnet/HttpAbstractions/blob/rel/1.1.0/src/Microsoft.AspNetCore.Http.Features/IRequestCookieCollection.cs) does not take into account that multiple values can exist for the same cookie.

This causes problems further down the line in for example the [CookieAuthenticationHandler](https://github.com/aspnet/Security/blob/rel/1.1.1/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationHandler.cs#L75), which may not be able to Unprotect the first value of the cookie because it may be set by an different application. This may cause the session to be lost.

I've also observered this behavior with the AntiForgery token cookie, see [DefaultAntiforgeryTokenStore](https://github.com/aspnet/Antiforgery/blob/2fcb187d7d34e9a0b046c2502c3a8b9b0155db91/src/Microsoft.AspNetCore.Antiforgery/Internal/DefaultAntiforgeryTokenStore.cs#L45), leading to the following stacktrace:

```
System.InvalidOperationException:
   at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgeryTokenSerializer.Deserialize (Microsoft.AspNetCore.Antiforgery, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgery.DeserializeTokens (Microsoft.AspNetCore.Antiforgery, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgery.ValidateTokens (Microsoft.AspNetCore.Antiforgery, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgery+<ValidateRequestAsync>d__9.MoveNext (Microsoft.AspNetCore.Antiforgery, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Mvc.ViewFeatures.Internal.ValidateAntiforgeryTokenAuthorizationFilter+<OnAuthorizationAsync>d__3.MoveNext (Microsoft.AspNetCore.Mvc.ViewFeatures, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker+<InvokeAuthorizationFilterAsync>d__20.MoveNext (Microsoft.AspNetCore.Mvc.Core, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker+<InvokeAuthorizationFilterAsync>d__20.MoveNext (Microsoft.AspNetCore.Mvc.Core, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker+<InvokeAsync>d__18.MoveNext (Microsoft.AspNetCore.Mvc.Core, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Builder.RouterMiddleware+<Invoke>d__4.MoveNext (Microsoft.AspNetCore.Routing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Builder.Extensions.MapMiddleware+<Invoke>d__3.MoveNext (Microsoft.AspNetCore.Http.Abstractions, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Builder.Extensions.MapMiddleware+<Invoke>d__3.MoveNext (Microsoft.AspNetCore.Http.Abstractions, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware`1+<Invoke>d__18.MoveNext (Microsoft.AspNetCore.Authentication, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Server.IISIntegration.IISMiddleware+<Invoke>d__8.MoveNext (Microsoft.AspNetCore.Server.IISIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Hosting.Internal.RequestServicesContainerMiddleware+<Invoke>d__3.MoveNext (Microsoft.AspNetCore.Hosting, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification (mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089)
   at Microsoft.AspNetCore.Server.Kestrel.Internal.Http.Frame`1+<RequestProcessingAsync>d__2.MoveNext (Microsoft.AspNetCore.Server.Kestrel, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
Inner exception System.Security.Cryptography.CryptographicException handled at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgeryTokenSerializer.Deserialize:
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingBasedDataProtector.UnprotectCore (Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingBasedDataProtector.DangerousUnprotect (Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingBasedDataProtector.Unprotect (Microsoft.AspNetCore.DataProtection, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
   at Microsoft.AspNetCore.Antiforgery.Internal.DefaultAntiforgeryTokenSerializer.Deserialize (Microsoft.AspNetCore.Antiforgery, Version=1.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60)
```

Is my suspicion correct that this is a bug? Or did I completely overlook something?
