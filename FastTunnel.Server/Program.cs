// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Net.Http;
using System.Net.Sockets;
using FastTunnel.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FastTunnel.Server;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console().WriteTo.File("Logs/log-.log", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args
            });

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

            (builder.Configuration as IConfigurationBuilder).AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
            (builder.Configuration as IConfigurationBuilder).AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true); ;

            // -------------------FastTunnel STEP1 OF 3------------------
            builder.Services.AddFastTunnelServer(builder.Configuration.GetSection("FastTunnel"));
            // -------------------FastTunnel STEP1 END-------------------

            builder.Host.UseWindowsService();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            // -------------------FastTunnel STEP2 OF 3------------------
            app.UseFastTunnelServer();
            // -------------------FastTunnel STEP2 END-------------------

            app.MapFastTunnelServer();

            app.Run();
        }
        catch (System.Exception ex)
        {
            Log.Fatal(ex, "致命异常");
            throw;
        }
    }
}
