﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Core.Interfaces;
using Assets.CS.TabletopUI;

namespace Assets.Logic
{
    public class AspectMatchFilter
    {
        private readonly Dictionary<string, int> _filterCriteria;

        public AspectMatchFilter(Dictionary<string, int> filterCriteria)
        {
            _filterCriteria = filterCriteria;
        }

        public IEnumerable<ElementStack> FilterElementStacks(IEnumerable<ElementStack> stacks)
        {
            IList<ElementStack> filteredElementStacks=new List<ElementStack>();
            foreach (var stack in stacks)
            {

                if (stack.GetAspects().Any(a => _filterCriteria.ContainsKey(a.Key) && _filterCriteria[a.Key] <= a.Value))
                    filteredElementStacks.Add(stack);
            }
            return filteredElementStacks;
        }
    }
}
