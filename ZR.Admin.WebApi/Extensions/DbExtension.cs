﻿using Infrastructure;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using SqlSugar.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using ZR.Admin.WebApi.Framework;
using ZR.Model.System;

namespace ZR.Admin.WebApi.Extensions
{
    public static class DbExtension
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //全部数据权限
        public static string DATA_SCOPE_ALL = "1";
        //自定数据权限
        public static string DATA_SCOPE_CUSTOM = "2";
        //部门数据权限
        public static string DATA_SCOPE_DEPT = "3";
        //部门及以下数据权限
        public static string DATA_SCOPE_DEPT_AND_CHILD = "4";
        //仅本人数据权限
        public static string DATA_SCOPE_SELF = "5";

        public static void AddDb(IConfiguration Configuration)
        {
            string connStr = Configuration.GetConnectionString(OptionsSetting.ConnAdmin);
            string connStrBus = Configuration.GetConnectionString(OptionsSetting.ConnBus);

            int dbType = Convert.ToInt32(Configuration[OptionsSetting.ConnDbType]);
            int dbType_bus = Convert.ToInt32(Configuration[OptionsSetting.ConnBusDbType]);

            SugarIocServices.AddSqlSugar(new List<IocConfig>() {
               new IocConfig() {
                ConfigId = "0",
                ConnectionString = connStr,
                DbType = (IocDbType)dbType,
                IsAutoCloseConnection = true
            }, new IocConfig() {
                ConfigId = "1",
                ConnectionString = connStrBus,
                DbType = (IocDbType)dbType_bus,
                IsAutoCloseConnection = true
            }
            });
            SugarIocServices.ConfigurationSugar(db =>
            {
                #region db0
                db.GetConnection(0).Aop.OnLogExecuting = (sql, pars) =>
                {
                    var param = DbScoped.SugarScope.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value));

                    FilterData();

                    logger.Info($"{sql}，{param}");
                };

                db.GetConnection(0).Aop.OnError = (e) =>
                {
                    logger.Error(e, $"执行SQL出错：{e.Message}");
                };
                //SQL执行完
                db.GetConnection(0).Aop.OnLogExecuted = (sql, pars) =>
                {
                    //执行完了可以输出SQL执行时间 (OnLogExecutedDelegate) 
                };
                #endregion

                #region db1
                //Db1
                db.GetConnection(1).Aop.OnLogExecuting = (sql, pars) =>
                {
                    var param = DbScoped.SugarScope.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value));

                    logger.Info($"Sql语句：{sql}, {param}");
                };
                //Db1错误日志
                db.GetConnection(1).Aop.OnError = (e) =>
                {
                    logger.Error($"执行Sql语句失败：{e.Sql}，原因：{e.Message}");
                };
                #endregion
            });
        }

        /// <summary>
        /// 分页获取count 不会追加sql
        /// </summary>
        private static void FilterData()
        {
            var u = App.User;
            if (u == null) return;
            //获取当前用户的信息
            var user = JwtUtil.GetLoginUser(App.HttpContext);
            if (user == null) return;
            //管理员不过滤
            if (user.RoleIds.Any(f => f.Equals("admin"))) return;

            foreach (var role in user.Roles)
            {
                string dataScope = role.DataScope;
                if (DATA_SCOPE_ALL.Equals(dataScope))//所有权限
                {
                    break;
                }
                else if (DATA_SCOPE_CUSTOM.Equals(dataScope))//自定数据权限
                {
                    //有问题
                    //var roleDepts = db0.Queryable<SysRoleDept>()
                    //.Where(f => f.RoleId == role.RoleId).Select(f => f.DeptId).ToList();
                    //var filter1 = new TableFilterItem<SysDept>(it => roleDepts.Contains(it.DeptId));
                    //DbScoped.SugarScope.GetConnection(0).QueryFilter.Add(filter1);
                }
                else if (DATA_SCOPE_DEPT.Equals(dataScope))//本部门数据
                {
                    //有问题添加后的SQL 语句 是 AND deptId = @deptId
                    var exp = Expressionable.Create<SysDept>();
                    exp.Or(it => it.DeptId == user.DeptId);
                    var filter1 = new TableFilterItem<SysDept>(exp.ToExpression());
                    DbScoped.SugarScope.GetConnection(0).QueryFilter.Add(filter1);
                    Console.WriteLine("本部门数据过滤");
                }
                else if (DATA_SCOPE_DEPT_AND_CHILD.Equals(dataScope))//本部门及以下数据
                {
                    //SQl  OR {}.dept_id IN ( SELECT dept_id FROM sys_dept WHERE dept_id = {} or find_in_set( {} , ancestors ) )
                }
                else if (DATA_SCOPE_SELF.Equals(dataScope))//仅本人数据
                {
                    var filter1 = new TableFilterItem<SysUser>(it => it.UserId == user.UserId);
                    DbScoped.SugarScope.GetConnection(0).QueryFilter.Add(filter1);
                }
            }
        }
    }
}
