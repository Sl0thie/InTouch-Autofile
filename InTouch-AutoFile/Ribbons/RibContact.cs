﻿using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace InTouch_AutoFile
{
    public partial class RibContact
    {
        private Outlook.Inspector inspector;

        private void RibContact_Load(object sender, RibbonUIEventArgs e)
        {
            inspector = Context as Outlook.Inspector;
            switch (Op.NextFormRegion)
            {
                case ContactFormRegion.None:
                    break;

                case ContactFormRegion.InTouchSettings:
                    inspector.SetCurrentFormPage("InTouch-AutoFile.ContactInTouchSettings");
                    Op.NextFormRegion = ContactFormRegion.None;
                    break;
            }
        }

        private void ButtonInTouchSettings_Click(object sender, RibbonControlEventArgs e)
        {
            inspector.SetCurrentFormPage("InTouch-AutoFile.ContactInTouchSettings");
        }
    }
}
