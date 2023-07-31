using Microsoft.AspNetCore.Mvc;
using ZR.Admin.WebApi.Extensions;
using ZR.Admin.WebApi.Filters;
using ZR.Model.Dto;
using ZR.Model.System;
using ZR.Service.System.IService;

namespace ZR.Admin.WebApi.Controllers
{
    /// <summary>
    /// 文章目录Controller
    /// </summary>
    [Route("article/ArticleCategory")]
    public class ArticleCategoryController : BaseController
    {
        /// <summary>
        /// 文章目录接口
        /// </summary>
        private readonly IArticleCategoryService _ArticleCategoryService;

        public ArticleCategoryController(IArticleCategoryService ArticleCategoryService)
        {
            _ArticleCategoryService = ArticleCategoryService;
        }

        /// <summary>
        /// 查询文章目录列表
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpGet("list")]
        //[ActionPermissionFilter(Permission = "articlecategory:list")]
        public IActionResult QueryArticleCategory([FromQuery] ArticleCategoryQueryDto parm)
        {
            var response = _ArticleCategoryService.GetList(parm);
            return SUCCESS(response);
        }

        /// <summary>
        /// 查询文章目录列表树
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpGet("treeList")]
        //[ActionPermissionFilter(Permission = "articlecategory:list")]
        public IActionResult QueryTreeArticleCategory([FromQuery] ArticleCategoryQueryDto parm)
        {
            var response = _ArticleCategoryService.GetTreeList(parm);
            return SUCCESS(response);
        }

        /// <summary>
        /// 查询文章目录详情
        /// </summary>
        /// <param name="CategoryId"></param>
        /// <returns></returns>
        [HttpGet("{CategoryId}")]
        //[ActionPermissionFilter(Permission = "articlecategory:query")]
        public IActionResult GetArticleCategory(int CategoryId)
        {
            var response = _ArticleCategoryService.GetFirst(x => x.CategoryId == CategoryId);
            
            return SUCCESS(response);
        }

        /// <summary>
        /// 添加文章目录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Verify]
        [ActionPermissionFilter(Permission = "articlecategory:add")]
        [Log(Title = "文章目录", BusinessType = BusinessType.INSERT)]
        public IActionResult AddArticleCategory([FromBody] ArticleCategoryDto parm)
        {
            var modal = parm.Adapt<ArticleCategory>().ToCreate(HttpContext);
            var response = _ArticleCategoryService.AddArticleCategory(modal);

            return ToResponse(response);
        }

        /// <summary>
        /// 更新文章目录
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Verify]
        [ActionPermissionFilter(Permission = "articlecategory:edit")]
        [Log(Title = "文章目录", BusinessType = BusinessType.UPDATE)]
        public IActionResult UpdateArticleCategory([FromBody] ArticleCategoryDto parm)
        {
            var modal = parm.Adapt<ArticleCategory>().ToUpdate(HttpContext);
            var response = _ArticleCategoryService.Update(w => w.CategoryId == modal.CategoryId, it => new ArticleCategory()
            {
                Name = modal.Name,
                ParentId = modal.ParentId,
            });

            return ToResponse(response);
        }

        /// <summary>
        /// 删除文章目录
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{ids}")]
        [ActionPermissionFilter(Permission = "articlecategory:delete")]
        [Log(Title = "文章目录", BusinessType = BusinessType.DELETE)]
        public IActionResult DeleteArticleCategory(string ids)
        {
            int[] idsArr = Tools.SpitIntArrary(ids);
            if (idsArr.Length <= 0) { return ToResponse(ApiResult.Error($"删除失败Id 不能为空")); }

            var response = _ArticleCategoryService.Delete(idsArr);

            return ToResponse(response);
        }

        /// <summary>
        /// 导出文章目录
        /// </summary>
        /// <returns></returns>
        [Log(Title = "文章目录", BusinessType = BusinessType.EXPORT, IsSaveResponseData = false)]
        [HttpGet("export")]
        [ActionPermissionFilter(Permission = "articlecategory:export")]
        public IActionResult Export([FromQuery] ArticleCategoryQueryDto parm)
        {
            parm.PageSize = 10000;
            var list = _ArticleCategoryService.GetList(parm).Result;

            string sFileName = ExportExcel(list, "ArticleCategory", "文章目录");
            return SUCCESS(new { path = "/export/" + sFileName, fileName = sFileName });
        }

        /// <summary>
        /// 获取文章目录,前端没用到
        /// </summary>
        /// <returns></returns>
        [HttpGet("CategoryList")]
        public IActionResult CategoryList()
        {
            var response = _ArticleCategoryService.GetAll();
            return SUCCESS(response);
        }

    }
}