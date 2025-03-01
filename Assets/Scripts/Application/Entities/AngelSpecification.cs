﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.UI;
using SecretHistories.Spheres.Angels;
using SecretHistories.Constants;

namespace SecretHistories.Entities
{
   public class AngelSpecification: AbstractEntity<AngelSpecification>
    {
        [FucineValue]
        public string Choir { get; set; }

        [FucineValue]
        public string LiveIn{ get; set; }

        [FucineValue]
        public string WatchOver { get; set; }

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
        }

        public AngelSpecification()
        {}

        public AngelSpecification(EntityData importDataForEntity, ContentImportLog log) : base(importDataForEntity, log)
        {

        }



    }
}
