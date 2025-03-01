﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrbCreationExtensions;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Constants;
using SecretHistories.Spheres;
using SecretHistories.UI;

namespace Assets.Scripts.Application.Infrastructure.SimpleJsonGameDataImport
{
    public class SimpleJsonSlotImporter
    {

        public static List<SphereSpec> ImportSituationOngoingSlotSpecs(Hashtable htSituation, List<SphereSpec> ongoingSlotsForRecipe)
        {
            List<SphereSpec> ongoingSlotSpecs = new List<SphereSpec>();

            

            if (htSituation.ContainsKey(SaveConstants.SAVE_ONGOINGSLOTELEMENTS))
            {
                var htOngoingSlotStacks = htSituation.GetHashtable(SaveConstants.SAVE_ONGOINGSLOTELEMENTS);

                foreach (string slotPath in htOngoingSlotStacks.Keys)
                {
                    var slotId = slotPath.Split(FucinePath.SPHERE)[0];
                    var slotSpec = new SphereSpec(typeof(ThresholdSphere), slotId);
                    ongoingSlotSpecs.Add(slotSpec);
                }
            }

            else
            {
                //we don't have any elements in ongoing slots - but we might still have an empty slot from the recipe, which isn't tracked in the save
                //so add the slot to the spec anyway
               foreach (var slot in ongoingSlotsForRecipe)
                    ongoingSlotSpecs.Add(slot);
            }

            return ongoingSlotSpecs;
        }

    }
}
