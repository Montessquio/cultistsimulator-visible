﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Core.Interfaces;

namespace Assets.Core.Entities
{
   public class CreatedVerb: IVerb
    {
        public CreatedVerb(string id, string label, string description)
        {
            Id = id;
            Label = label;
            Description = description;
            Startable = false;
            Species = string.Empty;
        }

        public string Species { get; private set; }

        public  bool Transient
        {
            get { return true; }
        }

        public string Art=>String.Empty;


        public string Id { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public SlotSpecification Slot { get; set; }
        public List<SlotSpecification> Slots { get; set; }
        public bool Startable { get; set; }
    }
}
