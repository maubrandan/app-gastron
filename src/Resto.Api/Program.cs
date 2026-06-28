using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Resto.Application;

using Resto.Domain.Exceptions;

using Resto.Infrastructure;

using Resto.Infrastructure.Identity;

using Resto.Infrastructure.Persistence;

using Resto.Infrastructure.SignalR;



var builder = WebApplication.CreateBuilder(args);



builder.Services.AddApplication(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);



builder.Services.AddControllers();

builder.Services.AddOpenApi();



builder.Services.AddCors(options =>

{

    options.AddPolicy("Frontend", policy =>

        policy.WithOrigins(

                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()

                ?? ["http://localhost:4200"])

            .AllowAnyHeader()

            .AllowAnyMethod()

            .AllowCredentials());

});



var app = builder.Build();



using (var scope = app.Services.CreateScope())

{

    var db = scope.ServiceProvider.GetRequiredService<RestoDbContext>();

    await db.Database.MigrateAsync();

    await RestoDbSeeder.SeedAsync(db);



    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await IdentitySeeder.SeedAsync(userManager, roleManager, logger);

}



if (app.Environment.IsDevelopment())

{

    app.MapOpenApi();

}



app.UseExceptionHandler(errorApp =>

{

    errorApp.Run(async context =>

    {

        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;



        if (exception is ConcurrencyConflictException concurrencyEx)

        {

            context.Response.StatusCode = StatusCodes.Status409Conflict;

            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new

            {

                type = "https://resto.local/errors/concurrency-conflict",

                title = "Conflicto de concurrencia",

                status = 409,

                detail = concurrencyEx.Message

            });

            return;

        }



        if (exception is DomainException domainEx)

        {

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new

            {

                type = "https://resto.local/errors/domain",

                title = "Error de negocio",

                status = 400,

                detail = domainEx.Message

            });

            return;

        }



        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new

        {

            type = "https://resto.local/errors/internal",

            title = "Error interno del servidor",

            status = 500,

            detail = "Error interno del servidor."

        });

    });

});



if (!app.Environment.IsDevelopment())

{

    app.UseHttpsRedirection();

}



app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<RestoHub>("/hubs/resto");



app.Run();

