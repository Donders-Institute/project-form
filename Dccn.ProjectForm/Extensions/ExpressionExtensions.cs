using System;
using System.Linq.Expressions;

namespace Dccn.ProjectForm.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, TBaseResult>> UpcastFuncResult<T, TResult, TBaseResult>(
            this Expression<Func<T, TResult>> expression) where TResult : TBaseResult
        {
            return Expression.Lambda<Func<T, TBaseResult>>(expression.Body, expression.Parameters);
        }
    }
}