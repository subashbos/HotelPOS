namespace HotelPOS.Api.Middleware
{
    /// <summary>
    /// Awaits ApiUserContext.EnsurePermissionsLoadedAsync() once per authenticated request, before
    /// any controller/service synchronously reads IUserContext.Permissions. Without this, that
    /// synchronous getter would have to block a thread-pool thread on the permissions DB query
    /// itself - IUserContext.Permissions can't be made async because the WPF client also
    /// implements it, synchronously, from an already-loaded local session.
    /// </summary>
    public class PermissionsPreloadMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionsPreloadMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApiUserContext userContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await userContext.EnsurePermissionsLoadedAsync();
            }

            await _next(context);
        }
    }
}
