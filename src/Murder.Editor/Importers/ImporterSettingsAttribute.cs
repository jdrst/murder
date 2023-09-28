﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Murder.Editor.Importers
{
    internal enum FilterType
    {
        All,
        OnlyTheseFolders,
        ExceptTheseFolders,
        None
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class ImporterSettingsAttribute : Attribute
    {
        internal FilterType FilterType;
        internal readonly string[] FilterFolders;
        internal readonly string[] FileExtensions;

        public ImporterSettingsAttribute(params string[] fileExtensions)
        {
            FilterType = FilterType.All;
            FilterFolders = new string[0];
            FileExtensions = fileExtensions;
        }
        
        public ImporterSettingsAttribute(FilterType filterType,  string[] filterFolders, string[] fileExtensions)
        {
            FilterType = filterType;
            FilterFolders = filterFolders;
            FileExtensions = fileExtensions;
        }
    }
}