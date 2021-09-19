﻿using SchoolGrades.DbClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace SchoolGrades
{
    internal partial class BusinessLayer
    {
        internal List<StartLink> GetStartLinksOfClass(Class Class)
        {
            return dl.GetStartLinksOfClass(Class);
        }
        internal void UpdatePathStartLinkOfClass(Class currentClass, string text)
        {
            dl.UpdatePathStartLinkOfClass(currentClass, text);
        }
        internal int? SaveStartLink(int? IdStartLink, int? IdClass, string SchoolYear,
            string StartLink, string Desc)
        {
            return dl.SaveStartLink(IdStartLink, IdClass, SchoolYear, StartLink, Desc);
        }
        internal void DeleteStartLink(int? currentIdStartLink)
        {
            dl.DeleteStartLink(currentIdStartLink);
        }
    }
}
