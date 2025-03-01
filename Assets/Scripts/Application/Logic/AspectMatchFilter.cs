﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecretHistories.Fucine;
using SecretHistories.UI;

namespace Assets.Logic
{
    public class AspectMatchFilter
    {
        private readonly Dictionary<string, int> _filterCriteria;

        public AspectMatchFilter(Dictionary<string, int> filterCriteria)
        {
            _filterCriteria = filterCriteria;
        }

        public IEnumerable<Token> FilterElementStacks(IEnumerable<Token> stacks)
        {
            IList<Token> filteredElementStacks=new List<Token>();
            foreach (var stack in stacks)
            {

                if (stack.Payload.GetAspects(true).Any(a => _filterCriteria.ContainsKey(a.Key) && _filterCriteria[a.Key] <= a.Value))
                    filteredElementStacks.Add(stack);
            }
            return filteredElementStacks;
        }
    }
}
