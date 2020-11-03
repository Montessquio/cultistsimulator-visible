﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Assets.Core;
using Assets.Core.Enums;
using Assets.Core.Fucine;
using Assets.Core.Fucine.DataImport;
using Assets.Core.Interfaces;

namespace Assets.Core.Entities
{
    [FucineImportable("recipes")]
    public class Recipe : AbstractEntity<Recipe>
    {

        [FucineValue("x")]
        public string ActionId { get; set; }

        [FucineDict(ValidateAsElementId = true)]
        public Dictionary<string, string> Requirements { get; set; }

        [FucineDict(ValidateAsElementId = true)]
        public Dictionary<string, string> TableReqs { get; set; }

        [FucineDict(ValidateAsElementId = true)]
        public Dictionary<string, string> ExtantReqs { get; set; }

        [FucineDict(ValidateAsElementId = true)]
        public Dictionary<string, string> Effects { get; set; }

        [FucineAspects(ValidateAsElementId = true)]
        public AspectsDictionary Aspects {get; set; }

        [FucineList]
        public List<MutationEffect> Mutations { get; set; }

        /// <summary>
        /// Elements that should be purged from the board (including currently dragged card if any). Int value is max number elements to be purged. (Later might also purge from slots and even maybe situations.
        /// </summary>)
        [FucineDict(ValidateAsElementId = true)]
        public Dictionary<string, int> Purge { get; set; }

        [FucineDict]
        public Dictionary<string, int> HaltVerb { get; set; }

         [FucineDict]
        public Dictionary<string, int> DeleteVerb { get; set; }


        /// <summary>
        /// do something grander like a bong when we loop this recipe
        /// </summary>
        [FucineValue(false)]
        public bool SignalImportantLoop { get; set; }

        /// <summary>
        /// This is distinct from the EndingFlavour on Ending, because some recipes may be menacing but not end directly.
        /// </summary>
        [FucineValue((int)EndingFlavour.None)]
        public EndingFlavour SignalEndingFlavour
        {
            get;
            set;
        } 

        [FucineValue(false)]
        public bool Craftable { get; set; }

        /// <summary>
        /// If HintOnly is true and Craftable is false, the recipe will display as a hint, but *only if no craftable recipes are available*
        /// </summary>
        [FucineValue(false)]
        public bool HintOnly { get; set; }

        //real recipes override hint prediction, both override null recipes
        public virtual int Priority
        {
            get
            {
                if (HintOnly)
                    return 0;
                return 1;
            }
        }

        [FucineValue(0)]
        public int Warmup { get; set; }

        [FucineValue(DefaultValue = ".", Localise = true)]
        public string Label { get; set; }


        /// <summary>
        /// displayed when we identify and when we are running a recipe
        /// </summary>
        [FucineValue(DefaultValue = ".", Localise = true)]
        public string StartDescription { get; set; }

        /// <summary>
        /// displayed in the results when the recipe is complete. If we loop straight to another recipe, it won't usually be visible.
        /// </summary>
        [FucineValue(DefaultValue = ".", Localise = true)]
        public string Description { get; set; }

        [FucineValue("")]
        public string Comments { get; set; }

        /// <summary>
        /// On completion, the recipe will draw
        ///from this deck and add the result to the outcome.
        /// </summary>
        [FucineDict]
        public Dictionary<string, int> DeckEffects { get; set; }

        [FucineList(Localise = true)]
        public List<LinkedRecipeDetails> Alt { get; set; }

       [FucineList(Localise = true)]
        public List<LinkedRecipeDetails> Linked { get; set; }

        [FucineValue("")]
        public string Ending { get; set; }


        /// <summary>
        /// 0 means any number of executions; otherwise, this recipe may only be executed this many times by a given character.
        /// </summary>
        [FucineValue(0)]
        public int MaxExecutions { get; set; }

        [FucineValue(null)]
        public string BurnImage { get; set; }

        [FucineValue((int)PortalEffect.None)]
        public PortalEffect PortalEffect { get; set; }

        [FucineList(Localise=true)]
        public List<SlotSpecification> Slots { get; set; }

        [FucineSubEntity(typeof(DeckSpec))]
        public DeckSpec InternalDeck { get; set; }

        protected Recipe()
        {

        }

        public Recipe(EntityData importDataForEntity, ContentImportLog log):base(importDataForEntity, log)
        {

        }

 
        protected override void OnPostImportForSpecificEntity(ContentImportLog log, ICompendium populatedCompendium)
        {
            if (InternalDeck.Spec.Any())
            {
                InternalDeck.SetId("deck." + Id);
                if (populatedCompendium.TryAddEntity(InternalDeck))
                    DeckEffects.Add(InternalDeck.Id, InternalDeck.Draws);
                else
                    log.LogProblem("Duplicate internal deck id: " + InternalDeck.Id);
            }



            foreach (var l in Linked)
                l.OnPostImport(log, populatedCompendium);

            foreach (var a in Alt)
                a.OnPostImport(log, populatedCompendium);

        }


        public bool UnlimitedExecutionsPermitted()
        {
            return MaxExecutions == 0;
        }


        public bool CanExecuteInContext(Recipe currentRecipe,SituationState situationState)
        {
            //return true if:
            //Situation is Unstarted; verb matches; and the recipe is either craftable or hintable
            if (situationState == SituationState.ReadyToStart && (Craftable || HintOnly) && ActionId==currentRecipe.ActionId) //this is slightly naughty: we're assuming that if a NullRecipe is passed, it'll be set up with the correct action id for the verb. 
                return true;

            //Situation is Ongoing. Recipe is in Alt list of current recipe - as Always Succeed and not as Additional. ActionId doesn't need to match.
            if (situationState == SituationState.Ongoing && currentRecipe.Alt.Exists(r => r.Id == Id && r.ShouldAlwaysSucceed() && !r.Additional))
                return true;

            //Situation is RequiringExecution, and recipe is in Linked list of current recipe.  ActionId doesn't need to match.
            if (situationState == SituationState.RequiringExecution && currentRecipe.Linked.Exists(r => r.Id == Id))
                return true;

            return false;
        }


        public bool RequirementsSatisfiedBy(AspectsInContext aspectsinContext)
        {
            foreach (var req in Requirements)
                if (!CheckRequirementsSatisfiedForContext(aspectsinContext.AspectsInSituation, req))
                    return false;

            foreach (var treq in TableReqs)
                if (!CheckRequirementsSatisfiedForContext(aspectsinContext.AspectsOnTable, treq))
                    return false;

            foreach (var ereq in ExtantReqs)
                if (!CheckRequirementsSatisfiedForContext(aspectsinContext.AspectsExtant, ereq))
                    return false;

            return true;
        }

        private static bool CheckRequirementsSatisfiedForContext(IAspectsDictionary aspectsToCheck,
            KeyValuePair<string, string> req)
        {
            if (!int.TryParse(req.Value, out var reqValue))
                //the value is not an int: it must be a reference to another aspect
                reqValue = aspectsToCheck.AspectValue(req.Value);

            {
                if (reqValue <= -1) //this is a No More Than requirement
                {
                    if (aspectsToCheck.AspectValue(req.Key) >= -reqValue)
                        return false;
                }
                else if (!(aspectsToCheck.AspectValue(req.Key) >= reqValue))
                {
                    //req >0 means there must be >=req of the element
                    return false;
                }
            }
            return true;
        }




    }
}