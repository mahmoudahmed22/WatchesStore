﻿using System.Threading.Tasks;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ShippingCountryRule : IRule
    {
        public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var match = expression.HasListMatch(context.Customer?.ShippingAddress?.CountryId ?? 0);

            return Task.FromResult(match);
        }
    }
}
