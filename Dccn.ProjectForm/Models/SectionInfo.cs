using System;
using System.Linq.Expressions;
using Dccn.ProjectForm.Pages;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Dccn.ProjectForm.Models
{
    public class SectionInfo
    {
        private readonly Func<FormModel, ISectionModel> _compiledExpr;

        public SectionInfo(Expression<Func<FormModel, ISectionModel>> expr)
        {
            _compiledExpr = expr.Compile();
            Expr = expr;
            Id = ExpressionHelper.GetExpressionText(expr);
        }

        public Expression<Func<FormModel, ISectionModel>> Expr { get; }
        public string Id { get; }

        public ISectionModel GetModel(FormModel model)
        {
            return _compiledExpr(model);
        }
    }
}