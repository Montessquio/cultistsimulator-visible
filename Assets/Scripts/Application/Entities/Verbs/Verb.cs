﻿using System;
using System.Collections.Generic;
using Assets.Scripts.Application.Infrastructure.Events;
using SecretHistories.Abstract;
using SecretHistories.Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Commands;
using SecretHistories.Core;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.UI;

namespace SecretHistories.Entities
{
    [FucineImportable("verbs")]
    public class Verb: AbstractEntity<Verb>
    {
        public override string Id => _id;

        [FucineValue(DefaultValue = ".", Localise = true)]
        public virtual string Label { get; set; }

        [FucineValue(DefaultValue = ".", Localise = true)]
        public virtual string Description { get; set; }

        [FucineValue]
        public virtual string Icon { get; set; }

        /// <summary>
        /// This doesn't do anything at the moment; but we should support it for teh legacy
        /// </summary>
        [FucineValue(DefaultValue = true)]
        public virtual bool Controllable { get; set; }

        [FucineValue(DefaultValue=VerbCategory.Shabda)]
        public virtual VerbCategory Category { get; set; }

        public virtual bool Spontaneous { get; set; }

#pragma warning disable 67
        public event Action<TokenPayloadChangedArgs> OnChanged;
        public event Action<float> OnLifetimeSpent;
#pragma warning restore 67
        [Encaust]
        public virtual int Quantity => 1;
        [Encaust]
        public virtual Dictionary<string, int> Mutations { get; }
        public virtual List<SphereSpec> Thresholds { get; set; } = new List<SphereSpec>();
        [FucineSubEntity(typeof(SphereSpec),Localise = true)]
        public virtual SphereSpec Slot { get; set; }

        [FucineList(Localise = true)]
        public virtual List<SphereSpec> Slots { get; set; }

        public virtual bool IsValid()
        {
            return true;
        }


        protected Verb(string id, string label, string description)
        {
            _id = id;
            Label = label;
            Description = description;
        }

        public static Verb CreateSpontaneousVerb(string id, string label, string description)
        {
            var v=new  Verb(id, label, description);
            v.Spontaneous = true;
            return v;
        }



        public virtual string DefaultUniqueTokenId()
        {
            int identity = FucineRoot.Get().IncrementedIdentity();
            return $"!{Id}_{identity}";
        }


        public Verb()
        {

        }

        public Verb(EntityData importDataForEntity, ContentImportLog log) : base(importDataForEntity, log)
        {

        }

        

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
            if (Slot != null)
                    Thresholds.Add(Slot); //what if this is empty? likely source of trouble later
            Thresholds.AddRange(Slots);
        }

    }
}
