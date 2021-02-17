﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using SecretHistories.Fucine.DataImport;
using SecretHistories.Fucine;

namespace SecretHistories.Fucine
{
    public class ValueImporter : AbstractImporter
    {
        public override bool TryImportProperty<T>(T entity, CachedFucineProperty<T> _cachedFucinePropertyToPopulate, EntityData entityData, ContentImportLog log)
        {
            object valueInData;

            if (_cachedFucinePropertyToPopulate.FucineAttribute.Localise)
                valueInData = entityData.ValuesTable[_cachedFucinePropertyToPopulate.LowerCaseName];
            else
                 valueInData = entityData.ValuesTable[_cachedFucinePropertyToPopulate.LowerCaseName];

            if (valueInData==null)
            {
                _cachedFucinePropertyToPopulate.SetViaFastInvoke(entity,_cachedFucinePropertyToPopulate.FucineAttribute.DefaultValue);
                return false;
            }
            else
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(_cachedFucinePropertyToPopulate.ThisPropInfo.PropertyType);
                _cachedFucinePropertyToPopulate.SetViaFastInvoke(entity, typeConverter.ConvertFromString(valueInData.ToString()));
                return true;
            }
        }
    }
}