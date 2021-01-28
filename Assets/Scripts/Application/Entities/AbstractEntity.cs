﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SecretHistories.Fucine.DataImport;
using SecretHistories.Interfaces;
using UnityEngine;

namespace SecretHistories.Fucine
{

    public abstract class AbstractEntity<T>: IEntityWithId where T : AbstractEntity<T>
    {
        protected string _id;

        [FucineId]
        public virtual string Id
        {
            get => _id;
        }

        public void SetId(string id)
        {
            _id = id;
        }


        /// <summary>
        /// If a 'lever' value can be set to the id of another entity (currently, just elements)
        /// then that entity should be retrieved from a compendium as, instead, the contents (in the saved file) of the specified lever - i.e., the entity from a previous game or context.
        /// This allows us to have e.g. a template lever_lastbook element which is always replaced by the current value of the lastbook lever - i.e. the value from the previous game.
        /// </summary>
        [FucineValue("")]
        public string Lever { get; set; }


        public string UniqueId { get; set; }
        protected bool Refined = false;
        protected readonly Hashtable UnknownProperties = new Hashtable();

        /// <summary>
        /// This is run for every top-level entity when the compendium has been completely (re)populated. Use for entities that
        /// need additional population based on data from other entities.
        /// It's not explicitly run for subentities - that's up to their parent entities.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="populatedCompendium"></param>
        public void OnPostImport(ContentImportLog log, Compendium populatedCompendium)
        {
            if (Refined)
                return;

            SupplyValidationRequirementsToCompendium(populatedCompendium);
            OnPostImportForSpecificEntity(log,populatedCompendium);
            PopUnknownKeysToLog(log);

            Refined = true;
        }
        /// <summary>
        /// This is overridden wherever we need more logic in each subclass
        /// </summary>
        /// <param name="log"></param>
        /// <param name="populatedCompendium"></param>
        protected abstract void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium);

        private void PopUnknownKeysToLog(ContentImportLog log)
        {
            Hashtable unknownProperties = PopAllUnknownProperties();

            if (unknownProperties.Keys.Count > 0)
            {
                foreach (var k in unknownProperties.Keys)
                    log.LogInfo($"Unknown property in import: {k} for {GetType().Name}");
            }
        }

        private void SupplyValidationRequirementsToCompendium(Compendium populatedCompendium)
        {
            var fucineProperties = TypeInfoCache<T>.GetCachedFucinePropertiesForType();

            foreach (var cachedProperty in fucineProperties)
            {
                if (cachedProperty.FucineAttribute.ValidateAsElementId)
                {
                    object toValidate = cachedProperty.GetViaFastInvoke(this as T);
                    populatedCompendium.SupplyElementIdsForValidation(toValidate);
                }
            }
        }



        public void PushUnknownProperty(object key, object value)
        {
            UnknownProperties.Add(key, value);
        }

        public virtual Hashtable PopAllUnknownProperties()
        {
            Hashtable propertiesPopped = new Hashtable(UnknownProperties);
            UnknownProperties.Clear();
            return propertiesPopped;
        }


        protected AbstractEntity(EntityData importDataForEntity, ContentImportLog log)
        {
            //this still needs an empty body (or not empty if I want specific logic) in each of the subclasses, or the FastInvoke constructor won't work
            try
            {
                var fucineProperties = TypeInfoCache<T>.GetCachedFucinePropertiesForType();

                foreach (var cachedProperty in fucineProperties)
                {
                    //don't forget defaultdrawmessages is both localisable and a Dictionary<string,string>

                    var importer = cachedProperty.GetImporterForProperty();
                    bool imported = importer.TryImportProperty<T>(this as T, cachedProperty, importDataForEntity, log);
                    if (imported)
                        importDataForEntity.ValuesTable.Remove(cachedProperty.LowerCaseName);
                }


                foreach (var k in importDataForEntity.ValuesTable.Keys)
                {
                    PushUnknownProperty(k, importDataForEntity.ValuesTable[k]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        protected AbstractEntity()
        {

        }
    }

}
