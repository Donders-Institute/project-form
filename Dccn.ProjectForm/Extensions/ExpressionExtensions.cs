using System;
using System.Linq;
using System.Linq.Expressions;

namespace Dccn.ProjectForm.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, TBaseResult>> UpcastLambda<T, TResult, TBaseResult>(
            this Expression<Func<T, TResult>> expression) where TResult : TBaseResult
        {
            return Expression.Lambda<Func<T, TBaseResult>>(expression.Body, expression.Parameters);
        }

        public static Expression<Func<T, TResult>> Substitute<TParam, T, TResult>(
            this Expression<Func<TParam, T, TResult>> expression, TParam value)
        {
            var visitor = new Visitor(expression.Parameters.First(), Expression.Constant(value, typeof(TParam)));
            return Expression.Lambda<Func<T, TResult>>(visitor.Visit(expression.Body) ?? throw new InvalidOperationException(), expression.Parameters.Skip(1));
        }

        private class Visitor : ExpressionVisitor
        {
            private readonly ParameterExpression _param;
            private readonly Expression _expansion;

            public Visitor(ParameterExpression param, Expression expansion)
            {
                _param = param;
                _expansion = expansion;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _param ? _expansion : node;
            }
        }
    }
}