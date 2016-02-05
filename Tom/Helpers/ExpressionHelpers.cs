using System;
using System.Linq.Expressions;

namespace Tom.Helpers
{
    public static class ExpressionHelpers
    {
        public static string GetName<TModel>(this Expression<Func<TModel, object>> exp)
        {
            MemberExpression body = exp.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
