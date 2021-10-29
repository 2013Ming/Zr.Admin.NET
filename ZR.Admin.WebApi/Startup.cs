using Hei.Captcha;
using Infrastructure;
using Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SqlSugar.IOC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZR.Admin.WebApi.Extensions;
using ZR.Admin.WebApi.Filters;
using ZR.Admin.WebApi.Middleware;

namespace ZR.Admin.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            CurrentEnvironment = hostEnvironment;
        }

        private IWebHostEnvironment CurrentEnvironment { get; }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            string corsUrls = Configuration["sysConfig:cors"];

            //���ÿ���
            services.AddCors(c =>
            {
                c.AddPolicy("Policy", policy =>
                {
                    policy.WithOrigins(corsUrls.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                    .AllowAnyHeader()//��������ͷ
                    .AllowCredentials()//����cookie
                    .AllowAnyMethod();//�������ⷽ��
                });
            });
            //����Error unprotecting the session cookie����
            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "DataProtection"));
            //��ͨ��֤��
            services.AddHeiCaptcha();
            services.AddSession();
            services.AddHttpContextAccessor();

            //Cookie ��֤
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            //����������Model��
            services.Configure<OptionsSetting>(Configuration);

            InjectRepositories(services);

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(GlobalActionMonitor));//ȫ��ע���쳣
            })
            .AddMvcLocalization()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ZrAdmin", Version = "v1" });
                if (CurrentEnvironment.IsDevelopment())
                {
                    //����ĵ�ע��
                    c.IncludeXmlComments("ZRAdmin.xml", true);
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZrAdmin v1"));

            //ʹ���Զ�ζ�ȥbody����
            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                return next();
            });
            //�������ʾ�̬�ļ�/wwwrootĿ¼�ļ���Ҫ����UseRoutingǰ��
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("Policy");//Ҫ����app.UseEndpointsǰ��

            //app.UseAuthentication������Authentication�м�������м������ݵ�ǰHttp�����е�Cookie��Ϣ������HttpContext.User���ԣ�������õ�����
            //����ֻ����app.UseAuthentication����֮��ע����м�����ܹ���HttpContext.User�ж�ȡ��ֵ��
            //��Ҳ��Ϊʲô����ǿ��app.UseAuthentication����һ��Ҫ���������app.UseMvc����ǰ�棬��Ϊֻ������ASP.NET Core��MVC�м���в��ܶ�ȡ��HttpContext.User��ֵ��
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseResponseCaching();

            // �ָ�/��������
            app.UseAddTaskSchedulers();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        /// ע��Services����
        /// </summary>
        /// <param name="services"></param>
        private void InjectRepositories(IServiceCollection services)
        {
            services.AddAppService();

            //�����ƻ�����
            services.AddTaskSchedulers();

            string connStr = Configuration.GetConnectionString(OptionsSetting.ConnAdmin);
            string connStrBus = Configuration.GetConnectionString(OptionsSetting.ConnBus);
            string dbKey = Configuration[OptionsSetting.DbKey];
            int dbType = Convert.ToInt32(Configuration[OptionsSetting.ConnDbType]);
            int dbType_bus = Convert.ToInt32(Configuration[OptionsSetting.ConnBusDbType]);

            SugarIocServices.AddSqlSugar(new List<IocConfig>() {
               new IocConfig() {
                ConfigId = "0",  //�����ݿ�
                ConnectionString = connStr,
                DbType = (IocDbType)dbType,
                IsAutoCloseConnection = true//�Զ��ͷ�
            }, new IocConfig() {
                ConfigId = "1", // ���⻧�õ�
                ConnectionString = connStrBus,
                DbType = (IocDbType)dbType_bus,
                IsAutoCloseConnection = true//�Զ��ͷ�
            }
            });

            //��ʽ���� ������ӡSQL 
            DbScoped.SugarScope.GetConnection(0).Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("��SQL��䡿" + sql.ToLower() + "\r\n"
                    + DbScoped.SugarScope.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
            };
            //�����ӡ��־
            DbScoped.SugarScope.GetConnection(0).Aop.OnError = (e) =>
            {
                Console.WriteLine($"[ִ��Sql����]{e.Message}��SQL={e.Sql}");
                Console.WriteLine();
            };

            //��ʽ���� ������ӡSQL 
            DbScoped.SugarScope.GetConnection(1).Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("��SQL���Bus��" + sql.ToLower() + "\r\n"
                    + DbScoped.SugarScope.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
            };
            //Bus Db������־
            DbScoped.SugarScope.GetConnection(1).Aop.OnError = (e) =>
            {
                Console.WriteLine($"[ִ��Sql����Bus]{e.Message}��SQL={e.Sql}");
                Console.WriteLine();
            };
        }
    }
}
